using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections;

namespace CORERenderer.Loaders
{
    public class OBJLoader
    {
        public bool LoadOBJ(string path, out float[] outVertices)//, out Vector3[] vertices, out Vector2[] UVs, out Vector3[] normals)
        {
            if (path == "None")
            {
                outVertices = Array.Empty<float>();
                return false;
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                temp.Add(i);
            }
            string filename = path[(temp[^1] + 1)..];

            List<float> vertices = new();
            List<Vector3> vectorVertices = new();

            List<float> normals = new();
            List<Vector3> vectorNormals = new();

            List<float> UVCoordinates = new();
            List<Vector2> vectorUVCoordinates = new();

            List<int> fValues = new();
            List<Vector3> VectorfValues = new();

            List<string> usemtls = new();
            
            List<string> oValues = new();
            List<string> sValues = new();

            List<int> oPositions = new();

            List<float> ambient;
            List<float> diffuse;
            List<float> specular;
            List<float> transparent;
            List<float> shininess;
            List<int> illum;
            List<string> texture;
            List<string> map;

            string mtllib = "Null";

            string[] tempString = File.ReadAllLines(path);

            List<string> unreadableLines = new();

            Console.WriteLine($"Reading {filename}:");
            //maybe could be done better?
            foreach (string n in tempString)
            {
                switch (n[0..2])
                {
                    case "# ": //comment
                        break;

                    case "mt": //mtllib (maybe better way?)
                        mtllib = n[7..]; //"mtllib " is 7 chars long
                        break;

                    case "o ": //texture name
                        oValues.Add(n[2..]);
                        oPositions.Add(Array.FindIndex(tempString, z => z == n));
                        break;

                    case "v ": //vector
                        int[] localV = new int[3];
                        int z = 0;
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            localV[z] = i;
                            z++;
                        }
                        for (int i = 0; i < localV.Length - 1; i++)
                        {
                            vertices.Add(float.Parse(n[localV[i]..localV[i + 1]], CultureInfo.InvariantCulture));
                        }
                        vertices.Add(float.Parse(n[localV[2]..n.Length], CultureInfo.InvariantCulture));
                        break;

                    case "vn": //vector normal
                        int[] localVn = new int[3];
                        int y = 0;
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            localVn[y] = i;
                            y++;
                        }
                        for (int i = 0; i < localVn.Length - 1; i++)
                        {
                            normals.Add(float.Parse(n[localVn[i]..localVn[i + 1]], CultureInfo.InvariantCulture));
                        }
                        normals.Add(float.Parse(n[localVn[2]..n.Length], CultureInfo.InvariantCulture));
                        break;

                    case "vt": //UV coordinates
                        int[] localVt = new int[2];
                        int x = 0;
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            localVt[x] = i;
                            x++;
                        }
                        UVCoordinates.Add(float.Parse(n[localVt[0]..localVt[1]], CultureInfo.InvariantCulture));
                        UVCoordinates.Add(float.Parse(n[localVt[1]..n.Length], CultureInfo.InvariantCulture));
                        break;

                    case "s ": //s value
                        sValues.Add(n[2..]);
                        break;

                    case "us": //usemtl (maybe better way?)
                        usemtls.Add(n[7..]); //"usemtl " is 7 chars long
                        break;

                    case "f ": //v / vn / vt indicator
                        List<int> local = new();
                        List<int> local2;
                        List<string> local3 = new();

                        //isolates the indices into ../../.. then by / so the int value remains
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            local.Add(i);
                        }

                        //isolates the indices to ../../.. by using the indexes of the surrounding " "'s
                        for (int i = 0; i < local.Count - 1; i++)
                        {
                            local3.Add(n[(local[i] + 1)..local[i + 1]]);
                        }
                        local3.Add(n[(local[local.Count - 1] + 1)..n.Length]);

                        //isolates each int from local ( ../../.. ) and parses it
                        foreach (string s in local3)
                        {
                            local2 = new(); //new list because otherwise its .Count is too big for it to handle

                            for (int i = s.IndexOf("/"); i > -1; i = s.IndexOf("/", i + 1))
                            {
                                local2.Add(i);
                            }
                            //isolates the int values by taking the space between the / indexes
                            fValues.Add(int.Parse(s[..local2[0]], CultureInfo.InvariantCulture));
                            //checks if its an indice without texture coords ( ../../.. or ..//.. )
                            if (local2[0] != local2[1] - 1)
                            {
                                fValues.Add(int.Parse(s[(local2[0] + 1)..local2[1]], CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                fValues.Add(0);
                            }
                            fValues.Add(int.Parse(s[(local2[local2.Count - 1] + 1)..s.Length], CultureInfo.InvariantCulture));
                        }
                        break;

                    default:
                        unreadableLines.Add(n);
                        break;
                }
                
            }
            if (unreadableLines.Count > 0)
            {
                Console.WriteLine($" Couldn't read {unreadableLines.Count} lines in {filename}");
            }

            Console.WriteLine(vertices[4626]);

            outVertices = new float[fValues.Count / 3 * 8];
            int t = 0;
            for (int i = 0; i < fValues.Count; i += 3)
            {
                outVertices[t] = vertices[(fValues[i] * 3) - 3];
                outVertices[t + 1] = vertices[(fValues[i] * 3) - 2];
                outVertices[t + 2] = vertices[(fValues[i] * 3) - 1];

                outVertices[t + 3] = normals[(fValues[i + 2] * 3) - 3];
                outVertices[t + 4] = normals[(fValues[i + 2] * 3) - 2];
                outVertices[t + 5] = normals[(fValues[i + 2] * 3) - 1];

                if ((fValues[i + 1] * 2) - 1 != -1)
                {
                    outVertices[t + 6] = UVCoordinates[(fValues[i + 1] * 2) - 2];
                    outVertices[t + 7] = UVCoordinates[(fValues[i + 1] * 2) - 1];
                } else
                {
                    outVertices[t + 6] = 0;
                    outVertices[t + 7] = 0;
                }
                t += 8;
            }

            Console.WriteLine($"finished reading {filename}");

            Console.WriteLine();

            return true;
        }
    }
}
 