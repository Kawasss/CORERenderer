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
using System.Drawing;

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

            materials = new();
            List<Material> tempMtl = new();

            List<string> materialNames = new();

            List<Vector3> emissiveCoefficient = new(); //unused

            List<int> mtlPositions = new();
            List<string[]> mtlData = new();

            Dictionary<string, int> materialOrder = new();

            if ((path[(path.Length - 4)..] != ".mtl" && path[(path.Length - 4)..] != ".MTL") || !File.Exists(path))
            {
                Console.WriteLine($"Invalid file {filename}");
                error = -1;

                return false;
            }

            List<string> unreadableLines = new();

            string[] file = File.ReadAllLines(path);

            for (int i = 0; i < file.Length; i++)
                if (file[i].Length > 5 && file[i][0..6] == "newmtl")
                    mtlPositions.Add(Array.FindIndex(file, z => z == file[i])); //maybe change to just mtlPositions.Add(i), test first tho
            //adds all of the data of one material into an array
            for (int i = 0; i < mtlPositions.Count - 1; i++)
                mtlData.Add(file[mtlPositions[i]..mtlPositions[i + 1]]);
            mtlData.Add(file[mtlPositions[^1]..]);


            if (mtlData.Count == 0)
            {
                materials.Add(new());
                error = 0;
                return false;
            }

            for (int k = 0; k < mtlData.Count; k++)
            {
                Material material = new();
                for (int j = 0; j < mtlData[k].Length; j++)
                {
                    string n = mtlData[k][j][..Length(mtlData[k][j])];
                    if (n.Length == 0) //removes empty lines so that errors arent produced later on
                        n = "  ";
                    switch (n[0..2])
                    {
                        case "  ": //empty lines
                            break;

                        case "ne": //newmtl
                            material.Name = n[7..];
                            break;

                        case "Ns": //shininess
                            material.Shininess = float.Parse(n[n.IndexOf(" ")..Length(n)], CultureInfo.InvariantCulture);
                            break;

                        case "Ka": //ambient
                            List<int> local1 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local1.Add(i);
                            }
                            material.Ambient = GetVector3(n[local1[0]..local1[1]], n[local1[1]..local1[2]], n[local1[^1]..Length(n)]);
                            break;

                        case "Ks": //specular
                            List<int> local2 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local2.Add(i);
                            }
                            material.Specular = GetVector3(n[local2[0]..local2[1]], n[local2[1]..local2[2]], n[local2[^1]..Length(n)]);
                            break;

                        case "Ke": //emissive coefficient //currently unused
                            List<int> local3 = new(); //isolates all 3 values with the spaces inbetween them
                            for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                            {
                                local3.Add(i);
                            }
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
                                    material.Texture = Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[7..Length(n)]}");
                                    break;
                                case "map_d ":
                                    material.DiffuseMap = Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[6..Length(n)]}");
                                    break;
                                case "map_Ks":
                                    material.SpecularMap = Texture.ReadFromFile($"{path[..(temp[^1] + 1)]}{n[7..Length(n)]}");
                                    break;
                                case "map_Bu": //bump map
                                    Console.WriteLine($"Unsupported map type: Bump map at {n}");
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
            }
            Console.WriteLine($"Couldnt read {unreadableLines.Count} lines");

            //puts the materials in the correct of first being called
            for (int i = 0; i < mtlNames.Count; i++)
                for (int j = 0; j < tempMtl.Count; j++)
                    if (mtlNames[i] == tempMtl[j].Name)
                        materials.Add(tempMtl[j]);
            tempMtl = new();

            materialOrder = new();

            error = 1;
            return true;
        }
    }

    public class Material
    {
        public string Name;
        public float Shininess;
        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;
        public Vector3 EmissiveCoefficient;
        public float OpticalDensity;
        public int Illum;
        public float Transparency;
        public Texture Texture;
        public Texture DiffuseMap;
        public Texture SpecularMap;

        public Material()
        {
            Name = "placeholder";
            Texture = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png"); //for now textures and diffuse maps are the same
            DiffuseMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png");
            SpecularMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholderspecular.png");

            Ambient = new(0.2f, 0.2f, 0.2f);
            Diffuse = new(0.5f, 0.5f, 0.5f);
            Specular = new(1, 1, 1);
            EmissiveCoefficient = Vector3.Zero;
            Illum = 2;
            Shininess = 32;

        }
    }
}
