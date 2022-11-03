 using COREMath;
using CORERenderer.textures;
using CORERenderer;
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
    public partial class Readers
    {
        private static int Length(string s1) { if (!s1.Contains('#')) return s1.Length; else return s1.IndexOf("#"); }

        public static bool LoadMTL
        (
            string path, 
            out List<float> shininess,
            out List<Vector3> ambient, 
            out List<Vector3> diffuse, 
            out List<Vector3> specular,
            out List<float> opticalDensity, 
            out List<int> illum, 
            out List<float> transparency,
            out List<Texture> texture, 
            out List<Texture> diffuseMap,
            out List<Texture> specularMap
        )
        {
            //determines if a line has commentary or not
            

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                temp.Add(i);
            }
            string filename = path[(temp[^1] + 1)..];

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

            List<Vector3> emissiveCoefficient = new(); //unused

            if ((path[(path.Length - 4)..] != ".mtl" && path[(path.Length - 4)..] != ".MTL") || !File.Exists(path))
            {
                Console.WriteLine($"Invalid file {filename}");
                
                shininess.Add(0);
                ambient.Add(new(0.2f, 0.2f, 0.2f));
                diffuse.Add(new(0.8f, 0.8f, 0.8f));
                specular.Add(new(1, 1, 1));
                transparency.Add(1);
                opticalDensity.Add(1);
                illum.Add(1);
                diffuse.Add(null);
                specular.Add(null);
                
                return false;
            }

            List<string> newmtls = new();

            List<string> unreadableLines = new();

            string[] file = File.ReadAllLines(path);
            
            foreach (string n in file)
            {
                if (n.Length > 3)
                {
                    
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
                            shininess.Add(float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture));
                            break;

                        case "Ka": //ambient
                            List<int> local1 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local1.Add(i);
                            }
                            ambient.Add
                            (new
                                (
                                    float.Parse(n[local1[0]..local1[1]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local1[1]..local1[2]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local1[^1]..Length(n)], CultureInfo.InvariantCulture)
                                )
                            );
                            break;

                        case "Ks": //specular
                            List<int> local2 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local2.Add(i);
                            }
                            specular.Add
                            (new
                                (
                                    float.Parse(n[local2[0]..local2[1]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local2[1]..local2[2]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local2[^1]..Length(n)], CultureInfo.InvariantCulture)
                                )
                            );
                            break;

                        case "Ke": //emissive coefficient //currently unused
                            List<int> local3 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local3.Add(i);
                            }
                            emissiveCoefficient.Add
                            (new
                                (
                                    float.Parse(n[local3[0]..local3[1]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local3[1]..local3[2]], CultureInfo.InvariantCulture),
                                    float.Parse(n[local3[^1]..Length(n)], CultureInfo.InvariantCulture)
                                )
                            ); ;
                            break;

                        case "Ni":
                            opticalDensity.Add(float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture));
                            break;

                        case "il":
                            illum.Add(int.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture));
                            break;

                        case "d ":
                            transparency.Add(float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture));
                            break;

                        case "ma":
                            switch (n[0..6])
                            {
                                case "map_Kd":
                                    texture.Add(Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[7..Length(n)]}"));
                                    break;
                                case "map_d ":
                                    diffuseMap.Add(Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[6..Length(n)]}"));
                                    break;
                                case "map_Ks":
                                    specularMap.Add(Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[7..Length(n)]}"));
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
            }
            if (texture.Count == 0)
                texture.Add(null);
                //CORE doesnt use textures, only diffuse and specular maps (for now)
            if (diffuseMap.Count == 0)
                diffuseMap.Add(Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png"));
            if (specularMap.Count == 0)
                specularMap.Add(Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholderspecular.png"));

            return true;
        }
    }
}
