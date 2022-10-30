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
        public bool LoadOBJ(string path, out float[] outVertices, out uint[] outIndices)//, out Vector3[] vertices, out Vector2[] UVs, out Vector3[] normals)
        {
            if (path == "None")
            {
                outVertices = Array.Empty<float>();
                outIndices = Array.Empty<uint>();
                return false;
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                temp.Add(i);
            }
            string filename = path[(temp[^1] + 1)..];

            List<float> vertices = new();
            List<float> tempVertices = new();

            List<float> normals = new();

            List<float> UVCoordinates = new();

            List<int> fValues = new();
            List<uint> indicesValues = new();

            List<string> usemtls = new();
            
            List<string> oValues = new();
            List<string> sValues = new();

            List<int> oPositions = new();

            List<int> bindingsV = new();
            List<int> bindingsN = new();

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

            bool withTextures = true;

            Console.WriteLine($"Reading {filename}:");
            //maybe could be done better?
            int a = 0;
            //randomly causes a crash (?????????)
            Parallel.ForEach(tempString, (line, _, lineNumber) =>
            {
                //foreach (string n in tempString)
                //{
                string n = line;
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
                            vertices.Add(float.Parse(n[localV[i]..localV[i + 1]], CultureInfo.InvariantCulture)); //random crashes here (?)
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
                            normals.Add(float.Parse(n[localVn[i]..localVn[i + 1]], CultureInfo.InvariantCulture)); //random crashes here (?)
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
                            if (withTextures)
                            {
                                //isolates the int values by taking the space between the / indexes
                                fValues.Add(int.Parse(s[..local2[0]], CultureInfo.InvariantCulture));
                                //checks if its an indice without texture coords ( ../../.. or ..//.. )
                                if (local2[0] != local2[1] - 1)
                                {
                                    fValues.Add(int.Parse(s[(local2[0] + 1)..local2[1]], CultureInfo.InvariantCulture));
                                }
                                else
                                {
                                    withTextures = false;
                                    //fValues.Add(0);
                                }
                                fValues.Add(int.Parse(s[(local2[local2.Count - 1] + 1)..s.Length], CultureInfo.InvariantCulture));
                            }
                            //binds the vertice with its normal values
                            if (!bindingsV.Contains(int.Parse(s[..local2[0]], CultureInfo.InvariantCulture)))
                            {
                                bindingsV.Add(int.Parse(s[..local2[0]], CultureInfo.InvariantCulture));
                                bindingsN.Add(int.Parse(s[(local2[local2.Count - 1] + 1)..s.Length], CultureInfo.InvariantCulture));
                            }
                        }
                        if (local3.Count == 3)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                indicesValues.Add(uint.Parse(local3[i][..local3[i].IndexOf("/")], CultureInfo.InvariantCulture) - 1);
                            }
                        }
                        if (local3.Count == 4)
                        {
                            indicesValues.Add(uint.Parse(local3[0][..local3[0].IndexOf("/")], CultureInfo.InvariantCulture) - 1);
                            indicesValues.Add(uint.Parse(local3[1][..local3[1].IndexOf("/")], CultureInfo.InvariantCulture) - 1);
                            indicesValues.Add(uint.Parse(local3[3][..local3[3].IndexOf("/")], CultureInfo.InvariantCulture) - 1);
                            for (int i = 1; i < 4; i++)
                            {
                                indicesValues.Add(uint.Parse(local3[i][..local3[i].IndexOf("/")], CultureInfo.InvariantCulture) - 1);
                            }
                        }

                        break;

                    default:
                        unreadableLines.Add(n);
                        break;
                }
                //Console.Write($"\r {n}                                                  ");
            });

            if (unreadableLines.Count > 0)
            {
                Console.WriteLine($" Couldn't read {unreadableLines.Count} lines in {filename}");
            }

            Console.WriteLine(" done reading all lines, assigning values...");

            //outVertices = new float[vertices.Count + UVCoordinates.Count + normals.Count];//outVertices = new float[fValues.Count / 3 * 8];
            int e = 0;
            int t = 0;
            Parallel.For(0, vertices.Count, j => {
                //for (int i = 0; i < vertices.Count; i += 3) //fValues.Count
                //{
                int t = 0;
                tempVertices.Add(vertices[t]);
                tempVertices.Add(vertices[t + 1]);
                tempVertices.Add(vertices[t + 2]);
                t += 3;
                if (withTextures)//((fValues[i + 1] * 2) - 1 != -1)
                {
                    tempVertices.Add(UVCoordinates[e]);
                    tempVertices.Add(UVCoordinates[e + 1]);
                    e += 2;
                }
                else
                {
                    tempVertices.Add(0);
                    tempVertices.Add(0);
                }
                //Console.WriteLine($"{bindingsV.IndexOf(i / 3 + 1)}//{(bindingsN[bindingsV.IndexOf(i / 3 + 1)])}");
                int location = bindingsN[bindingsV[vertices.IndexOf(vertices[j]) / 3 + 1] - 1];
                tempVertices.Add(normals[location - 1] * 3 - 1);//outVertices[t + 5] = 0;//normals[(bindingsN[bindingsV.IndexOf(i / 3 + 1)] - 1) * 3];
                tempVertices.Add(normals[location] * 3);//outVertices[t + 6] = 0;//normals[(bindingsN[bindingsV.IndexOf(i / 3 + 1)] - 1) * 3 + 1];
                tempVertices.Add(normals[location - 1] * 3 + 1);//outVertices[t + 7] = 0;//normals[(bindingsN[bindingsV.IndexOf(i / 3 + 1)] - 1) * 3 + 2];
            });

            outVertices = tempVertices.ToArray();
            outIndices = indicesValues.ToArray();

            Console.WriteLine($"finished reading {filename}");

            Console.WriteLine();

            return true;
        }
    }
}