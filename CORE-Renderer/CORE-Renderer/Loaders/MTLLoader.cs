using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Transactions;
using System.Security.Cryptography;

namespace CORERenderer.Loaders
{
    public class MTLLoader
    {
        public bool LoadMTL
        (
            string path, 
            out List<float> shininess,
            out List<float> ambient, 
            out List<float> diffuse, 
            out List<float> specular,
            out List<float> opticalDensity, 
            out List<int> illum, 
            out List<float> transparency,
            out List<string> texture, 
            out List<string> diffuseMap,
            out List<string> specularMap
        )
        {
            if (!File.Exists(path))
            {
                throw new Exception($"File at {path} couldnt be found");
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                temp.Add(i);
            }
            string filename = path[(temp[^1] + 1)..];

            if (path[(path.Length - 4)..] != ".mtl" || path[(path.Length - 4)..] != ".MTL")
            {
                Console.WriteLine($"Invalid file format for {filename}");
            }

            shininess = new();
            ambient = new();
            diffuse = new();
            specular = new();
            opticalDensity = new();
            illum = new();
            transparency = new();
            texture = new();
            diffuseMap = new();
            specularMap = new();

            List<string> newmtls = new();

            List<string> unreadableLines = new();

            string[] file = File.ReadAllLines(path);
            
            foreach (string n in file)
            {
                if (n.Length < 2)
                {
                    break;
                }
                switch (n[0..2])
                {
                    case "  ": //empty lines
                        break;

                    case "# ": //commentary
                        break;

                    case "ne":
                        newmtls.Add(n[7..]); //"newmtl " is 7 chars long
                        break;

                    case "Ns": //shininess
                        shininess.Add(float.Parse(n[n.IndexOf(" ")..], CultureInfo.InvariantCulture));
                        break;

                    case "Ka": //ambient
                        List<int> local1 = new(); //isolates all 3 values with the spaces inbetween them
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            local1.Add(i);
                        }
                        ambient.Add(float.Parse(n[local1[0]..local1[1]], CultureInfo.InvariantCulture));
                        for (int i = 1; i < local1.Count - 1; i++) //maybe unneeded
                        {
                            ambient.Add(float.Parse(n[local1[i]..local1[i + 1]], CultureInfo.InvariantCulture));
                        }
                        ambient.Add(float.Parse(n[local1[^1]..], CultureInfo.InvariantCulture));
                        break;

                    case "Ks": //specular
                        List<int> local2 = new(); //isolates all 3 values with the spaces inbetween them
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            local2.Add(i);
                        }
                        ambient.Add(float.Parse(n[local2[0]..local2[1]], CultureInfo.InvariantCulture));
                        for (int i = 1; i < local2.Count - 1; i++) //maybe unneeded
                        {
                            ambient.Add(float.Parse(n[local2[i]..local2[i + 1]], CultureInfo.InvariantCulture));
                        }
                        ambient.Add(float.Parse(n[local2[^1]..], CultureInfo.InvariantCulture));
                        break;

                    case "Ke": //emissive coefficient //currently unused
                        List<int> local3 = new(); //isolates all 3 values with the spaces inbetween them
                        for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                        {
                            local3.Add(i);
                        }
                        ambient.Add(float.Parse(n[local3[0]..local3[1]], CultureInfo.InvariantCulture));
                        for (int i = 1; i < local3.Count - 1; i++) //maybe unneeded
                        {
                            ambient.Add(float.Parse(n[local3[i]..local3[i + 1]], CultureInfo.InvariantCulture));
                        }
                        ambient.Add(float.Parse(n[local3[^1]..], CultureInfo.InvariantCulture));
                        break;

                    case "Ni":
                        opticalDensity.Add(float.Parse(n[n.IndexOf(" ")..], CultureInfo.InvariantCulture));
                        break;

                    case "il":
                        illum.Add(int.Parse(n[n.IndexOf(" ")..], CultureInfo.InvariantCulture));
                        break;

                    case "d ":
                        transparency.Add(float.Parse(n[n.IndexOf(" ")..], CultureInfo.InvariantCulture));
                        break;

                    case "ma":
                        string s = GetMapType(n);
                        switch (n[0..6])
                        {
                            case "map_Kd":
                                texture.Add(n[2..]);
                                break;
                            case "map_d ":
                                diffuseMap.Add(n[2..]);
                                break;
                            case "map_Ks":
                                specularMap.Add(n[2..]);
                                break;
                            default:
                                unreadableLines.Add(n);
                                break;
                        }
                        break;

                    default:
                        unreadableLines.Add(n);
                        break;
                }
            }

            return true;
        }

        private static string GetMapType(string s)
        {
            string s2 = "-1";

            if (s[..6] == "map_Kd")
            {
                s2 = "Kd" + s[7..];
            }
            else if (s[..6] == "map_d ")
            {
                s2 = "_d" + s[6..];
            }
            else if (s[..6] == "map_Ks")
            {
                s2 = "Ks" + s[6..];
            }

            return s2;
        }
    }
}
