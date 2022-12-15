using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using COREMath;
using CORERenderer.Loaders;

namespace CORERenderer.CRSFile
{
    public partial class CRS
    {
        public static CRS ReadCRS(string path)
        {
            //finds the name of the file from the given path
            List<int> temp = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);

            string name = path[(temp[^1] + 1)..^4];

            FileStream fileStream = File.OpenRead($"{path}\\{name}.cst");
            fileStream.Close();
            CRS newCRS = new(name, path, fileStream);

            using (FileStream fs = File.Open($"{path}\\{name}.cst", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                //string objName;
                //string objLocation;
                int currentOBJ = -1;
                int currentLine = 1;
                for (string n = sr.ReadLine(); n != null; n = sr.ReadLine(), currentLine++)
                {
                    if (n.Length < 2 || n[0] == '#')
                        n = "  ";
                    switch (n[..2])
                    {
                        case "  ":
                            break;

                        case "<o":
                            currentOBJ++;
                            newCRS.allOBJs.Add(new($"{path}\\{currentOBJ}.obj", $"{path}\\{currentOBJ}.mtl"));
                            break;

                        case "na": //name not needed because core saves objects by id starting with 0, names can be found by simply incrementing "currentOBJ"
                            //objName = n[(n.IndexOf('=') + 1)..n.IndexOf(';')];
                            break;

                        case "ob":
                            //objLocation = $"{path}\\{currentOBJ}.obj";
                            break;

                        case "mt": //see case na
                            break;

                        case "tr":
                            List<int> local = new();
                            for (int i = n.IndexOf(','); i > -1; i = n.IndexOf(',', i + 1))
                                local.Add(i);
                            newCRS.allOBJs[^1].translation = new(
                                n[(n.IndexOf('=') + 1)..local[0]],
                                n[(local[0] + 1)..local[1]],
                                n[(local[1] + 1)..n.IndexOf(';')]);
                            break;

                        case "sc":
                            newCRS.allOBJs[^1].Scaling = float.Parse(n[(n.IndexOf('=') + 1)..n.IndexOf(';')], CultureInfo.InvariantCulture);
                            break;

                        case "ro":
                            switch (n[..7])
                            {
                                case "rotateX":
                                    newCRS.allOBJs[^1].rotationX = float.Parse(n[(n.IndexOf('=') + 1)..n.IndexOf(';')], CultureInfo.InvariantCulture);
                                    break;
                                case "rotateY":
                                    newCRS.allOBJs[^1].rotationY = float.Parse(n[(n.IndexOf('=') + 1)..n.IndexOf(';')], CultureInfo.InvariantCulture);
                                    break;
                                case "rotateZ":
                                    newCRS.allOBJs[^1].rotationZ = float.Parse(n[(n.IndexOf('=') + 1)..n.IndexOf(';')], CultureInfo.InvariantCulture);
                                    break;
                                default:
                                    break;
                            }
                            break;

                        case "</":
                            break;

                        default:
                            Console.WriteLine($"Couldn't read line {n}");
                            break;
                    }
                }
                newCRS.nextUnusedID = currentOBJ + 1;
                CORERenderContent.currentObj = -1;
                if (newCRS.allOBJs.Count > 0)
                    CORERenderContent.loaded = true;
            }
            

            return newCRS;
        }
    }
}
