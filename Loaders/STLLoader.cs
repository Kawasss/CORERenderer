using COREMath;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static bool LoadSTL(string path, out string name, out List<float> vertices, out Vector3 offset)
        {
            if (!File.Exists(path))
            {
                name = "ERROR";
                vertices = new();
                offset = Vector3.Zero;
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
                    offset = Vector3.Zero;
                    return false;
                }

                bool succes = false;
                if (binaryOrASCII[..5] == "solid")
                    succes = LoadSTLInASCII(path, out name, out vertices, out offset);
                else
                    succes = LoadSTLInBinary(path, out name, out vertices, out offset);
            }
            return true;
        }

        private static bool LoadSTLInASCII(string path, out string name, out List<float> vertices, out Vector3 offset)
        {
            vertices = new();
            offset = Vector3.Zero;
            bool firstLine = true;
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
                        if (firstLine)
                            offset = vertex;
                        vertex -= offset;

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

        private static bool LoadSTLInBinary(string path, out string name, out List<float> vertices, out Vector3 offset)
        {
            vertices = new();
            name = Path.GetFileName(path)[..^4];
            List<float> holder = new();
            float dividend = 1;
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                for (int i = 0; i < 80; i++) //ignore the header, which is 80 bytes long
                    fs.ReadByte();

                byte[] bytes = new byte[4];
                for (int i = 0; i < 4; i++)
                    bytes[i] = (byte)fs.ReadByte();
                int amountOfTriangles = BitConverter.ToInt32(bytes); //the amount of triangles is stored in 4 bytes directly after the header
                
                for (int k = 0; k < amountOfTriangles; k++)
                {
                    float[] normal = new float[3];
                    for (int j = 0; j < 3; j++)
                    {
                        for (int i = 0; i < 4; i++)
                            bytes[i] = (byte)fs.ReadByte();
                        normal[j] = BitConverter.ToSingle(bytes);
                    }
                    for (int l = 0; l < 3; l++)
                    {
                        for (int i = 0; i < 3; i++) //each vertex has 3 values, where each float is 4 bytes
                        {
                            for (int j = 0; j < 4; j++) //get one float
                                bytes[j] = (byte)fs.ReadByte();
                            if (vertices.Count < 3)
                            {
                                holder.Add(BitConverter.ToSingle(bytes));
                                vertices.Add(0);
                                if (holder[0] > 20)
                                    dividend = 1 / holder[0];
                                continue;
                            }
                            vertices.Add((BitConverter.ToSingle(bytes) - holder[i]) * dividend); //add that one float to the list
                        }
                        vertices.Add(1);
                        vertices.Add(0);

                        vertices.Add(normal[0]);
                        vertices.Add(normal[1]);
                        vertices.Add(normal[2]);
                    }
                    //ignore the attribute bytes
                    fs.ReadByte();
                    fs.ReadByte();
                }
            }
            //offset = new(holder[0], holder[1], holder[2]);
            offset = Vector3.Zero;
            return true;
        }

        public static Vector3 GetThreeFloatsWithRegEx(string line)
        {
            Vector3 returnValue = new();
            MatchCollection matches = Regex.Matches(line, @"([-+]?[0-9]*\.?[0-9]+)"); //fuck regex, im not gonna explain this
            if (matches.Count == 3)
            {
                returnValue.x = float.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture);
                returnValue.y = float.Parse(matches[1].Groups[1].Value, CultureInfo.InvariantCulture);
                returnValue.z = float.Parse(matches[2].Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return returnValue;
        }

        private static Vector2 GetTwoFloatsWithRegEx(string line)
        {
            Vector2 returnValue = new();
            MatchCollection matches = Regex.Matches(line, @"([-+]?[0-9]*\.?[0-9]+)");
            if (matches.Count == 2)
            {
                returnValue.x = float.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture);
                returnValue.y = float.Parse(matches[1].Groups[1].Value, CultureInfo.InvariantCulture);
            }
            return returnValue;
        }
    }
}
