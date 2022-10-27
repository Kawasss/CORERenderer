using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CORERenderer.Loaders
{
    public class MTLLoader
    {
        public static bool LoadMTL
        (
            string path, 
            out List<float> ambient, 
            out List<float> diffuse, 
            out List<float> specular,
            out List<float> transparency, 
            out List<int> illum, 
            out List<string> texture, 
            out List<string> diffuseMap,
            out List<string> specularMap
        )
        {
            if (!File.Exists(path))
            {
                throw new Exception($"File at {path} couldnt be found");
            }

            ambient = new();
            diffuse = new();
            specular = new();
            transparency = new();
            illum = new();
            texture = new();
            diffuseMap = new();
            specularMap = new();

            List<string> newmtls = new();

            List<string> stringNs = new();
            List<string> stringKa = new();
            List<string> stringKs = new();
            List<string> stringKe = new();
            List<string> stringNi = new();

            string[] file = File.ReadAllLines(path);
            
            foreach (string n in file)
            {
                switch (n[0..2])
                {
                    case "# ":
                        break;

                    case "ne":
                        newmtls.Add(n[7..]); //"newmtl " is 7 chars long
                        break;

                    case "Ns":
                        stringNs.Add(n);
                        break;

                    case "Ka":
                        stringKa.Add(n);
                        break;

                    case "Ks":
                        stringKs.Add(n);
                        break;

                    case "Ke":
                        stringKe.Add(n);
                        break;

                    case "Ni":
                        stringNi.Add(n);
                        break;

                    case "il":
                        illum.Add(int.Parse(n[6..], CultureInfo.InvariantCulture)); //"illum " is 6 chars long
                        break;

                    case "ma":
                        string s = GetMapType(n);
                        switch (s[0..2])
                        {
                            case "Kd":
                                texture.Add(s[2..]);
                                break;
                            case "_d":
                                diffuseMap.Add(s[2..]);
                                break;
                            case "Ks":
                                specularMap.Add(s[2..]);
                                break;
                            case "-1":
                                throw new Exception($"Couldnt read {s}");
                        }
                        break;
                }
            }

            return false;
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
