using COREMath;
using CORERenderer.Loaders;
using CORERenderer.Main;
using System.Security.AccessControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.CRS
{
    public partial class CRS : EngineProperties
    {
        public static CRS GenerateCRS(string name) //in future make it so that it takes requests for places to generate
        {                                          //for now its in the main path of the renderer
            string path = $"{CORERenderContent.pathRenderer}\\{name}.crs"; //!!change if directory can be asked
            string image = $"{path}\\folder.ico";

            if (Directory.Exists(path)) //add option to read existing .crs
                throw new Exception("file with given name already exists, cannot create new file");

            System.IO.Directory.CreateDirectory(path);
            File.SetAttributes(path, FileAttributes.System);
            File.Create(image).Close();
            File.Copy($"{CORERenderContent.pathRenderer}\\logos\\logo4.ico", image, true);
            File.SetAttributes(image, FileAttributes.Hidden);
            FileStream DekstopIni = File.Create($"{path}\\Desktop.ini");

            string DesktopContent = 
                """
                [.ShellClassInfo]
                ConfirmFileOp=0
                NoSharing=0
                IconFile=folder.ico
                IconIndex=0
                InfoTip=CORE Rendering Scene
                """;
            byte[] writeDesktop = Encoding.Unicode.GetBytes(DesktopContent);
            DekstopIni.Write(writeDesktop);

            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.Hidden);
            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.System); 

            FileStream cst = File.Create($"{path}\\{name}.cst");//CoreSceneTransformations
            cst.Close();

            return new(name, path, File.ReadAllLines($"{path}\\{name}.cst"), cst);
        }

        public void CSTAddObj(string pathToOBJ)
        {
            if (!File.Exists(pathToOBJ))
                return;
            //creates the file directory
            //using FileStream file = new($"{path}\\{name}.cst", FileMode.Truncate);
            List<int> backslashIndexes = new();
            for (int i = pathToOBJ.IndexOf("\\"); i > -1; i = pathToOBJ.IndexOf("\\", i + 1))
                backslashIndexes.Add(i);
            string objName = pathToOBJ[(backslashIndexes[^1] + 1)..^4];
            //writes the object attributes
            string addOBJ =
               $"""
                <obj id = "{nextUnusedID}">
                    name = {objName};
                    vertices = {nextUnusedID}.csv;
                    indices = {nextUnusedID}.csi;
                    mtllib = {nextUnusedID}.mtl;
                    scale = 1.0;
                    translation = 0.0, 0.0, 0.0;
                    rotateX = 0.0;
                    rotateY = 0.0;
                    rotateZ = 0.0;
                </obj>
                """;

            //byte[] writeData = Encoding.UTF8.GetBytes(addOBJ);
            using (StreamWriter sw = File.AppendText($"{path}\\{name}.cst"))
            {
                sw.WriteLine(addOBJ);
            }
            this.cstLines = File.ReadAllLines($"{path}\\{name}.cst");

            FileStream csvFile = File.Create($"{path}\\{nextUnusedID}.cv");
            FileStream csiFile = File.Create($"{path}\\{nextUnusedID}.ci");

            Obj newOBJ = new(pathToOBJ);

            this.allObjectInstances.Add(new(csvFile, csiFile, $"{path}\\{nextUnusedID}.cv", $"{path}\\{nextUnusedID}.ci", newOBJ.vertices.Count, newOBJ.indices.Count));

            //could be done in a method but would need some weird modifications
            //using (BinaryWriter writer = new(csvFile)) //writes all of the vertices and indices values to their respective files
            //{
            //writes all the vertice and indice values to their own files
            using (StreamWriter sw = new StreamWriter(csvFile))
            {
                using (StreamWriter sw1 = new(csiFile))
                {
                    for (int i = 0; i < newOBJ.vertices.Count; i++)
                    {
                        sw.Write($"<vertices id = {i}>\n");
                        for (int j = 0; j < newOBJ.vertices[i].Count; j++)
                        {
                            //writer.Write(newOBJ.vertices[i][j]);
                            sw.Write($"{newOBJ.vertices[i][j]}\n"); //Encoding.UTF8.GetBytes( .. ), applies to all the .write below and above
                }
                        sw.Write("</vertices>\n");
                    }
                    //}

                    //using (BinaryWriter writer = new(csiFile))
                    //{
                    for (int i = 0; i < newOBJ.indices.Count; i++)
                    {
                        sw1.Write($"<indices id = {i}>\n");
                        for (int j = 0; j < newOBJ.indices[i].Count; j++)
                        {
                            //writer.Write(newOBJ.indices[i][j]);
                            sw1.Write($"{newOBJ.indices[i][j]}\n");
                        }
                        sw1.Write($"</indices>\n");
                    }
                    //}
                }  
            }
            allOBJs.Add(newOBJ);
            this.UpdateIDs();
        }

        public void ScaleObj(int currentOBJ, float amount)
        {
            int local = Array.FindIndex(this.cstLines, z => z == $"<obj id = {this.nameIDBinder[allOBJs[currentOBJ].name]}>");
            allOBJs[currentOBJ].Scaling += amount;

            this.cstLines[local + 5] = $"   scale = {allOBJs[currentOBJ].Scaling};";
        }

        public void TranslateObj(int currentOBJ, Vector3 amount)
        {
            int local = Array.FindIndex(this.cstLines, z => z == $"<obj id = \"{currentOBJ}\">");

            allOBJs[currentOBJ].translation += amount;
            
            this.cstLines[local + 6] = $"    translation = {allOBJs[currentOBJ].translation.x}, {allOBJs[currentOBJ].translation.y}, {allOBJs[currentOBJ].translation.z};";
        }
        public void RotateXObj(int currentOBJ, float amount)
        {
            int local = Array.FindIndex(this.cstLines, z => z == $"<obj id = {this.nameIDBinder[allOBJs[currentOBJ].name]}>");

            allOBJs[currentOBJ].rotationX += amount;

            this.cstLines[local + 7] = $"    rotateX = {allOBJs[currentOBJ].translation.x}, {allOBJs[currentOBJ].translation.y}, {allOBJs[currentOBJ].translation.z};";
        }
        public void RotateYObj(int currentOBJ, float amount)
        {
            int local = Array.FindIndex(this.cstLines, z => z == $"<obj id = {this.nameIDBinder[allOBJs[currentOBJ].name]}>");

            allOBJs[currentOBJ].rotationY += amount;

            this.cstLines[local + 8] = $"    rotateY = {allOBJs[currentOBJ].translation.x}, {allOBJs[currentOBJ].translation.y}, {allOBJs[currentOBJ].translation.z};";
        }
        public void RotateZObj(int currentOBJ, float amount)
        {
            int local = Array.FindIndex(this.cstLines, z => z == $"<obj id = {this.nameIDBinder[allOBJs[currentOBJ].name]}>");

            allOBJs[currentOBJ].rotationZ += amount;

            this.cstLines[local + 9] = $"    rotateZ = {allOBJs[currentOBJ].translation.x}, {allOBJs[currentOBJ].translation.y}, {allOBJs[currentOBJ].translation.z};";
        }

        public void SaveChanges() //currently only saves the changes made to the last object
        {
            using (FileStream filestream = File.Open($"{path}\\{name}.cst", FileMode.Open))
                filestream.SetLength(0);
            File.WriteAllLines($"{path}\\{name}.cst", this.cstLines);
            Console.Write("\rSaved changes                                               ");
        }
    }
}
