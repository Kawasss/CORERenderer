using COREMath;
using CORERenderer.textures;
using CORERenderer.Main;
using System.Globalization;

namespace CORERenderer.Loaders
{
    public partial class Readers : LoaderDebug
    {
        private static int Length(string s1) 
        { 
            if (!s1.Contains('#')) 
                return s1.Length; 
            else 
                return s1.IndexOf('#'); 
        }
        private static Vector3 GetVector3(string s, string s1, string s2) //removes incredibly long and repetitive lines of code
        {
            return new
            (
                float.Parse(s, CultureInfo.InvariantCulture),
                float.Parse(s1, CultureInfo.InvariantCulture),
                float.Parse(s2, CultureInfo.InvariantCulture)
            );
        }

        public static bool LoadMTL(string path, List<string> mtlNames, out List<Material> materials, out int error)
        {
            if (path == null)
            {
                materials = new();
                error = 0;
                return false;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"path {path} doesnt exist");
                materials = new();
                error = -1;
                return false;
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);
            string filename = path[(temp[^1] + 1)..];
            //0 = no mtllib given (name == None), -1  = no (readable) file found

            if (filename == "None")
            {
                materials = new();
                error = 0;
                return false;
            }

            Console.WriteLine("Reading .mtl file..");

            materials = new();
            List<Material> tempMtl = new();
            Material material = new();

            if ((path[(path.Length - 4)..].ToLower() != ".mtl"))
            {
                Console.WriteLine($"Invalid file {filename}");
                error = -1;

                return false;
            }

            bool firstMTLPassed = false;

            List<string> unreadableLines = new();

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                for (string n = sr.ReadLine(); n != null; n = sr.ReadLine())
                {
                    if (n.Length < 2) //removes empty lines so that errors arent produced later on
                        n = "  ";
                    switch (n[0..2])
                    {
                        case "  ": //empty lines
                            break;

                        case "ne": //newmtl
                            if (!firstMTLPassed)
                                firstMTLPassed = true; //if its the first material dont add the current material, because that one is empty
                            else
                                tempMtl.Add(material);
                            material = new() {Name = n[7..]};
                            break;

                        case "Ns": //shininess
                            material.Shininess = float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture);
                            break;

                        case "Kd":
                            List<int> local0 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                                local0.Add(i);
                            material.Diffuse = GetVector3(n[local0[0]..local0[1]], n[local0[1]..local0[2]], n[local0[^1]..Length(n)]);
                            break;

                        case "Ka": //ambient
                            List<int> local1 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                                local1.Add(i);
                            material.Ambient = GetVector3(n[local1[0]..local1[1]], n[local1[1]..local1[2]], n[local1[^1]..Length(n)]);
                            break;

                        case "Ks": //specular
                            List<int> local2 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                                local2.Add(i);
                            material.Specular = GetVector3(n[local2[0]..local2[1]], n[local2[1]..local2[2]], n[local2[^1]..Length(n)]);
                            break;

                        case "Ke": //emissive coefficient //currently unused
                            List<int> local3 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                                local3.Add(i);
                            material.EmissiveCoefficient = GetVector3(n[local3[0]..local3[1]], n[local3[1]..local3[2]], n[local3[^1]..Length(n)]);
                            break;

                        case "Ni":
                            material.OpticalDensity = float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture);
                            break;

                        case "il":
                            material.Illum = int.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture);
                            break;

                        case "d ":
                            material.Transparency = float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture);
                            break;

                        case "ma":
                            switch (n[0..6])
                            {
                                case "map_Kd":
                                    if (!n.Contains("  "))
                                        material.Texture = Globals.FindTexture($"{path[..(temp[^1] + 1)]}{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                    else
                                        material.Texture = 0;
                                    break;
                                case "map_d ":
                                    if (!n.Contains("  "))
                                    {

                                        material.DiffuseMap = Globals.FindTexture($"{path[..(temp[^1] + 1)]}{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                    }
                                    else
                                        material.DiffuseMap = 0;
                                    break;
                                case "map_Ks":
                                    if (!n.Contains("  "))
                                        material.SpecularMap = Globals.FindTexture($"{path[..(temp[^1] + 1)]}{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                    else
                                        material.SpecularMap = 1;
                                    break;
                                default:
                                    if (LoaderDebug.showErrors)
                                        Console.WriteLine($"Unsupported map type: {n[..n.IndexOf(' ')]} at {n}");
                                    unreadableLines.Add(n);
                                    break;
                            }
                            break;

                        default:
                            if (n[0] == '#')
                                break;
                            unreadableLines.Add(n);
                            break;
                    }
                }
                tempMtl.Add(material);
            }
            if (unreadableLines.Count > 0)
            {
                Console.WriteLine($"Couldnt read {unreadableLines.Count} lines:");
                for (int i = 0; i < unreadableLines.Count; i++)
                    Console.WriteLine($"    {unreadableLines[i]}");
            }

            //puts the materials in the correct of first being called
            for (int i = 0; i < mtlNames.Count; i++)
                for (int j = 0; j < tempMtl.Count; j++)
                    if (mtlNames[i] == tempMtl[j].Name)
                        materials.Add(tempMtl[j]); 
            error = 1;

            return true;
        }
    }

}
