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

            //creates directory for the files
            System.IO.Directory.CreateDirectory(path);
            File.SetAttributes(path, FileAttributes.System);

            //creates the file for the directory icon and writes the imaga data to it
            File.Create(image).Close();
            File.Copy($"{CORERenderContent.pathRenderer}\\logos\\logo4.ico", image, true);
            File.SetAttributes(image, FileAttributes.Hidden);

            FileStream DeskstopIni = File.Create($"{path}\\Desktop.ini");

            //creates the Desktop.ini file for the directory settings and the icon of it
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
            DeskstopIni.Write(writeDesktop);
            DeskstopIni.Close();

            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.Hidden);
            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.System); 

            //creates the empty cst file for the CSTAddObj() to write to
            FileStream cst = File.Create($"{path}\\{name}.cst");//CoreSceneTransformations
            cst.Close();

            return new(name, path, File.ReadAllLines($"{path}\\{name}.cst"), cst);
        }

        public void CSTAddObj(string pathToOBJ)
        {
            if (!File.Exists(pathToOBJ))
                return;
            //creates the file directory
            List<int> backslashIndexes = new();
            for (int i = pathToOBJ.IndexOf("\\"); i > -1; i = pathToOBJ.IndexOf("\\", i + 1))
                backslashIndexes.Add(i);
            string objName = pathToOBJ[(backslashIndexes[^1] + 1)..^4];

            //writes the default object attributes
            string addOBJ =
               $"""
                <obj id = "{nextUnusedID}">
                    name = {objName};
                    vertices = {nextUnusedID}.cv;
                    indices = {nextUnusedID}.ci;
                    mtllib = {nextUnusedID}.mtl;
                    scale = 1.0;
                    translation = 0.0, 0.0, 0.0;
                    rotateX = 0.0;
                    rotateY = 0.0;
                    rotateZ = 0.0;
                </obj>
                """;

            using (StreamWriter sw = File.AppendText($"{path}\\{name}.cst"))
                sw.WriteLine(addOBJ);
            this.cstLines = File.ReadAllLines($"{path}\\{name}.cst");

            //creates the vertices and indices files
            FileStream csvFile = File.Create($"{path}\\{nextUnusedID}.cv");
            FileStream csiFile = File.Create($"{path}\\{nextUnusedID}.ci");

            Obj newOBJ = new(pathToOBJ);

            //creates the mtl file bound to the object, saving all the mtl data to a new file like the vertices and indices is too much effort for the efficiency it gives
            File.Create($"{path}\\{nextUnusedID}.mtl").Close();
            File.Copy($"{pathToOBJ[..backslashIndexes[^1]]}\\{newOBJ.mtllib}", $"{path}\\{nextUnusedID}.mtl", true);

            this.allObjectInstances.Add(new(csvFile, csiFile, $"{path}\\{nextUnusedID}.cv", $"{path}\\{nextUnusedID}.ci", newOBJ.vertices.Count, newOBJ.indices.Count));

            //writes all the vertice and indice values to their respective files
            using (StreamWriter sw = new StreamWriter(csvFile))
            {   //writes all the vertices and indices, with each object or "o" in the obj file being seperate by "</vertices>" or "</indices>"
                using (StreamWriter sw1 = new(csiFile))
                {
                    for (int i = 0; i < newOBJ.vertices.Count; i++)
                    {
                        sw.Write($"<vertices id = \"{i}\" materialName = \"{newOBJ.Materials[i].Name}\">\n");
                        for (int j = 0; j < newOBJ.vertices[i].Count; j++)
                            sw.Write($"{newOBJ.vertices[i][j]}\n");
                        sw.Write("</vertices>\n");
                    }

                    for (int i = 0; i < newOBJ.indices.Count; i++)
                    {
                        sw1.Write($"<indices id = \"{i}\">\n");
                        for (int j = 0; j < newOBJ.indices[i].Count; j++)
                            sw1.Write($"{newOBJ.indices[i][j]}\n");
                        sw1.Write($"</indices>\n");
                    }
                }  
            }
            allOBJs.Add(newOBJ);
            this.UpdateIDs();
        }

        public void SaveChanges()
        {   //creates a local string to save all changes to which will then be written to an empty version of the .cst file
            string local0 = string.Empty;
            using (FileStream filestream = File.Open($"{path}\\{name}.cst", FileMode.Open))
                lock (filestream)
                    filestream.SetLength(0);

            for (int i = 0; i < nextUnusedID; i++)
            {
                local0 += 
                $"""
                <obj id = "{i}">
                    name = {allOBJs[i].name};
                    vertices = {i}.cv;
                    indices = {i}.ci;
                    mtllib = {i}.mtl;
                    scale = {allOBJs[i].Scaling};
                    translation = {allOBJs[i].translation.x}, {allOBJs[i].translation.y}, {allOBJs[i].translation.z};
                    rotateX = {allOBJs[i].rotationX};
                    rotateY = {allOBJs[i].rotationY};
                    rotateZ = {allOBJs[i].rotationZ};
                </obj>

                """;
            }
            using (StreamWriter sw = File.AppendText($"{path}\\{name}.cst"))
                sw.WriteLine(local0);
            this.cstLines = File.ReadAllLines($"{path}\\{name}.cst");

            Console.Write("\rSaved changes                                               \n");
        }
    }
}
