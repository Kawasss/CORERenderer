using COREMath;
using CORERenderer.textures;
using CORERenderer.Main;
using System.Globalization;
using System.Diagnostics;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        private static int Length(string s1) 
        { 
            if (!s1.Contains('#')) 
                return s1.Length; 
            else 
                return s1.IndexOf('#'); 
        }

        public static Error LoadMTL(string path, List<string> mtlNames, out List<Material> materials)
        {
            if (path == null)
            {
                materials = new();
                return Error.InvalidPath;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"path {path} doesnt exist");
                materials = new();
                return Error.FileNotFound;
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);
            string filename = path[(temp[^1] + 1)..];
            //0 = no mtllib given (name == None), -1  = no (readable) file found

            if (filename == "None")
            {
                materials = new();
                return Error.FileNotFound;
            }

            COREMain.console.WriteDebug($"Reading {Path.GetFileName(path)} file..");

            materials = new();
            List<Material> tempMtl = new();
            Material material = new();

            if ((path[(path.Length - 4)..].ToLower() != ".mtl"))
            {
                Console.WriteLine($"Invalid file {filename}");

                return Error.InvalidFile;
            }

            bool firstMTLPassed = false;

            List<string> unreadableLines = new();
            List<string> allTextures = new();
            List<string> allDiffuse = new();
            List<string> allSpecular = new();
            List<string> allNormal = new();
            Task task = Task.Run(() =>
            {
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
                                material = new() { Name = n[7..] };
                                if (allTextures.Count < tempMtl.Count)
                                    allTextures.Add("default");
                                if (allDiffuse.Count < tempMtl.Count)
                                    allDiffuse.Add("default");
                                if (allSpecular.Count < tempMtl.Count)
                                    allSpecular.Add("default");
                                if (allNormal.Count < tempMtl.Count)
                                    allNormal.Add("default");
                                break;

                            case "Ns": //shininess
                                material.Shininess = GetOneFloatWithRegEx(n);
                                break;

                            case "Kd":
                                material.Diffuse = GetThreeFloatsWithRegEx(n);
                                break;

                            case "Ka": //ambient
                                material.Ambient = GetThreeFloatsWithRegEx(n);
                                break;

                            case "Ks": //specular
                                material.Specular = GetThreeFloatsWithRegEx(n);
                                break;

                            case "Ke": //emissive coefficient //currently unused
                                material.EmissiveCoefficient = GetThreeFloatsWithRegEx(n);
                                break;

                            case "Ni":
                                material.OpticalDensity = GetOneFloatWithRegEx(n);
                                break;

                            case "il":
                                material.Illum = GetOneIntWithRegEx(n);
                                break;

                            case "d ":
                                material.Transparency = GetOneFloatWithRegEx(n);
                                break;

                            case "ma":
                                switch (n[0..6])
                                {
                                    case "map_Kd":
                                        if (File.Exists($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}"))
                                            allTextures.Add($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}");//material.Texture = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                        else
                                            allTextures.Add("default");//material.Texture = 0;
                                        break;
                                    case "map_d ":
                                        if (File.Exists($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}"))
                                            allDiffuse.Add($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}");//material.DiffuseMap = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                        else
                                            allDiffuse.Add("default");//material.DiffuseMap = 0;
                                        break;
                                    case "map_Ks":
                                        if (File.Exists($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}"))
                                            allSpecular.Add($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}");//material.SpecularMap = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                        else
                                            allSpecular.Add("default");//material.SpecularMap = 1;
                                        break;
                                    case "map_Bu":
                                        if (File.Exists($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}"))
                                            allNormal.Add($"{Path.GetDirectoryName(path)}\\{n[(n.LastIndexOf(' ') + 1)..Length(n)]}");//material.NormalMap = Globals.FindSRGBTexture($"{Path.GetDirectoryName(path)}\\{n[(n.IndexOf(' ') + 1)..Length(n)]}");
                                        else
                                            allNormal.Add("default");//material.NormalMap = 3;
                                        break;
                                    default:
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
                    if (allTextures.Count < tempMtl.Count)
                        allTextures.Add("default");
                    if (allDiffuse.Count < tempMtl.Count)
                        allDiffuse.Add("default");
                    if (allSpecular.Count < tempMtl.Count)
                        allSpecular.Add("default");
                    if (allNormal.Count < tempMtl.Count)
                        allNormal.Add("default");
                }
            });
            task.Wait();
            for (int i = 0; i < tempMtl.Count; i++)
            {
                Material newMtl = tempMtl[i];
                newMtl.Texture = allTextures[i] != "default" ? Globals.FindTexture(allTextures[i]) : 0;
                newMtl.DiffuseMap = allDiffuse[i] != "default" ? Globals.FindTexture(allDiffuse[i]) : 0;
                newMtl.SpecularMap = allSpecular[i] != "default" ? Globals.FindTexture(allSpecular[i]) : 1;
                newMtl.NormalMap = allNormal[i] != "default" ? Globals.FindSRGBTexture(allNormal[i]) : 3;
                tempMtl[i] = newMtl;
            }
            if (unreadableLines.Count > 0)
            {
                COREMain.console.WriteError($"Couldnt read {unreadableLines.Count} lines:");
                for (int i = 0; i < unreadableLines.Count; i++)
                    COREMain.console.WriteError($"    {unreadableLines[i]}");
            }

            //puts the materials in the correct of first being called
            for (int i = 0; i < mtlNames.Count; i++)
                for (int j = 0; j < tempMtl.Count; j++)
                    if (mtlNames[i] == tempMtl[j].Name)
                        materials.Add(tempMtl[j]); 

            return Error.None;
        }
    }

}