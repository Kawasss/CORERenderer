using COREMath;
using CORERenderer.GLFW;
using CORERenderer.Main;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static bool LoadSTL(string path, out string name, out List<float> vertices)
        {
            if (!File.Exists(path))
            {
                name = "ERROR";
                vertices = new();
                return false;
            }


            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                string binaryOrASCII = sr.ReadLine();
                if (binaryOrASCII.Length < 5) //the first 5 letters are needed to determine if the file is written in binary or not
                {
                    name = "ERROR";
                    vertices = new();
                    return false;
                }

                bool succes = false;
                if (binaryOrASCII[..5] == "solid")
                    succes = LoadSTLInASCII(path, out name, out vertices);
                else
                    succes = LoadSTLInBinary(path, out name, out vertices);
            }
            return false;
        }

        private static bool LoadSTLInASCII(string path, out string name, out List<float> vertices)
        {
            vertices = new();

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                name = sr.ReadLine()[6..]; //first 6 chars are "solid " so those can be skipped
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    /*
                    ASCII STI files are made of this sequence:
                    facet normal VALUE VALUE  VALUE
                     outer loop
                      vertex VALUE VALUE VALUE
                      vertex VALUE VALUE VALUE
                      vertex VALUE VALUE VALUE
                     endloop
                    endfacet
                    all the lines without values are skipped, the values are extracted with regex
                    */
                    if (line.Length < 2)
                        continue;

                    Vector3 normalValues = GetThreeFloatsWithRegEx(line);
                    sr.ReadLine();
                    for (int i = 0; i < 3; i++)
                    {
                        line = sr.ReadLine();
                        if (line == null)
                            break;
                        Vector3 vertex = GetThreeFloatsWithRegEx(line);

                        vertices.Add(vertex.x);
                        vertices.Add(vertex.y);
                        vertices.Add(vertex.z);

                        vertices.Add(1);
                        vertices.Add(0);

                        vertices.Add(normalValues.x);
                        vertices.Add(normalValues.y);
                        vertices.Add(normalValues.z);
                    }
                    sr.ReadLine();
                    sr.ReadLine();
                }
            }

            return false;
        }

        private static bool LoadSTLInBinary(string path, out string name, out List<float> vertices)
        {
            vertices = new();
            name = Path.GetFileName(path)[..^4];

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                for (int i = 0; i < 80; i++) //ignore the header, which is 80 bytes long
                    fs.ReadByte();

                byte[] amountByte = new byte[4];
                for (int i = 0; i < 4; i++)
                    amountByte[i] = (byte)fs.ReadByte();
                int amountOfTriangles = BitConverter.ToInt32(amountByte); //the amount of triangles is stored in 4 bytes directly after the header
                
                for (int k = 0; k < amountOfTriangles; k++)
                {
                    float[] normal = new float[3];
                    for (int j = 0; j < 3; j++)
                    {
                        byte[] normalBytes = new byte[4];
                        for (int i = 0; i < 4; i++)
                            normalBytes[i] = (byte)fs.ReadByte();
                        normal[j] = BitConverter.ToSingle(normalBytes);
                    }
                    for (int l = 0; l < 3; l++)
                    {
                        for (int i = 0; i < 3; i++) //each vertex has 3 values, where each float is 4 bytes
                        {
                            byte[] vertexBytes = new byte[4];
                            for (int j = 0; j < 4; j++) //get one float
                                vertexBytes[j] = (byte)fs.ReadByte();
                            vertices.Add(BitConverter.ToSingle(vertexBytes)); //add that one float to the list
                        }
                        vertices.Add(1);
                        vertices.Add(0);

                        vertices.Add(normal[0]);
                        vertices.Add(normal[1]);
                        vertices.Add(normal[2]);
                    }
                    //ignore the attribute bytes
                    _ = fs.ReadByte();
                    _ = fs.ReadByte();
                }
            }
            return true;
        }

        private static Vector3 GetThreeFloatsWithRegEx(string line)
        {
            Vector3 returnValue = new();
            MatchCollection matches = Regex.Matches(line, @"([-+]?[0-9]*\.?[0-9]+)"); //fuck regex
            if (matches.Count == 3)
            {
                returnValue.x = float.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture);
                returnValue.y = float.Parse(matches[1].Groups[1].Value, CultureInfo.InvariantCulture);
                returnValue.z = float.Parse(matches[2].Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return returnValue;
        }
    }
}
