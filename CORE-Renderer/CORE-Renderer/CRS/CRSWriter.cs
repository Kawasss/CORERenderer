using CORERenderer.Main;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using CORERenderer.Loaders;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.CRS
{
    public partial class CRS : Obj, EngineProperties
    {
        public static CRS GenerateCRS(string path, string name) //in future make it so that it takes requests for places to generate
        {                                                       //for now its in the main path of the renderer
            string image = $"{path}\\folder.ico";

            if (Directory.Exists($"{CORERenderContent.pathRenderer}\\{name}.crs")) //shouldnt be needed since LoadCRS() checks this but just in case
                throw new Exception("file with given name already exists, cannot create new file");

            //creates directory for the files
            System.IO.Directory.CreateDirectory(path);
            File.SetAttributes(path, FileAttributes.System);

            path = $"{CORERenderContent.pathRenderer}\\{name}.crs";

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

            return new(name, path, cst);
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

            allOBJs.Add(new(pathToOBJ));

            //writes the default object attributes
            string addOBJ =
               $"""
                <obj id = "{nextUnusedID}">
                name = {objName};
                objFile = {nextUnusedID}.obj;
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

            //creates the mtl file bound to the object
            File.Create($"{path}\\{nextUnusedID}.mtl").Close();
            if (allOBJs[^1].mtllib != "default")
                File.Copy($"{pathToOBJ[..backslashIndexes[^1]]}\\{allOBJs[^1].mtllib}", $"{path}\\{nextUnusedID}.mtl", true);
            else
                File.Copy($"{CORERenderContent.pathRenderer}\\Loaders\\default.mtl", $"{path}\\{nextUnusedID}.mtl", true);

            for (int i = 0; i < allOBJs[^1].Materials.Count; i++)
            {
                if (!File.Exists($"{path}\\{allOBJs[^1].Materials[i].DiffuseMap.name}"))
                {
                    File.Create($"{path}\\{allOBJs[^1].Materials[i].DiffuseMap.name}").Close();
                    File.Copy(allOBJs[^1].Materials[i].DiffuseMap.path, $"{path}\\{allOBJs[^1].Materials[i].DiffuseMap.name}", true);
                    Console.WriteLine(allOBJs[^1].Materials[i].DiffuseMap.name);
                }
                if (!File.Exists($"{path}\\{allOBJs[^1].Materials[i].SpecularMap.name}"))
                {
                    File.Create($"{path}\\{allOBJs[^1].Materials[i].SpecularMap.name}").Close();
                    File.Copy(allOBJs[^1].Materials[i].SpecularMap.path, $"{path}\\{allOBJs[^1].Materials[i].SpecularMap.name}", true);
                    Console.WriteLine(allOBJs[^1].Materials[i].SpecularMap.name);
                }
                if (!File.Exists($"{path}\\{allOBJs[^1].Materials[i].Texture.name}"))
                {
                    File.Create($"{path}\\{allOBJs[^1].Materials[i].Texture.name}").Close();
                    File.Copy(allOBJs[^1].Materials[i].Texture.path, $"{path}\\{allOBJs[^1].Materials[i].Texture.name}", true);
                    Console.WriteLine(allOBJs[^1].Materials[i].Texture.name);
                }
            }

            File.Create($"{path}\\{nextUnusedID}.obj").Close();
            File.Copy($"{pathToOBJ}", $"{path}\\{nextUnusedID}.obj", true);

            this.allObjectInstances.Add(new($"{path}\\{nextUnusedID}.obj", allOBJs[^1].vertices.Count, allOBJs[^1].indices.Count));

            this.allOBJs[^1].ID = nextUnusedID;

            this.UpdateIDs();
            CORERenderContent.currentObj = nextUnusedID - 1;
            CORERenderContent.HighlightLogic();
        }

        public void SaveChanges() //only works with object modifications, not with objects themselves
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
                objFile = {i}.obj;
                mtllib = {i}.mtl;
                scale = {allOBJs[i].Scaling.ToString(CultureInfo.InvariantCulture)};
                translation = {allOBJs[i].translation.x.ToString(CultureInfo.InvariantCulture)}, {allOBJs[i].translation.y.ToString(CultureInfo.InvariantCulture)}, {allOBJs[i].translation.z.ToString(CultureInfo.InvariantCulture)};
                rotateX = {allOBJs[i].rotationX.ToString(CultureInfo.InvariantCulture)};
                rotateY = {allOBJs[i].rotationY.ToString(CultureInfo.InvariantCulture)};
                rotateZ = {allOBJs[i].rotationZ.ToString(CultureInfo.InvariantCulture)};
                </obj>

                """;
            }
            using (StreamWriter sw = File.AppendText($"{path}\\{name}.cst"))
                sw.WriteLine(local0);

            Console.Write("\rSaved changes                                               ");
        }

        public void RemoveObject(int ID)
        {
            CORERenderContent.canDelete = false;
            if (ID < this.allOBJs.Count)
            {
                glDeleteBuffers(this.allOBJs[ID].GeneratedBuffers.ToArray());
                glDeleteBuffers(this.allOBJs[ID].elementBufferObject.ToArray());
                glDeleteVertexArrays(this.allOBJs[ID].GeneratedVAOs.ToArray());

                for (int j = 0; j < this.allOBJs[ID].Materials.Count; j++)
                {
                    glDeleteTexture(this.allOBJs[ID].Materials[j].Texture.Handle);
                    glDeleteTexture(this.allOBJs[ID].Materials[j].SpecularMap.Handle);
                    glDeleteTexture(this.allOBJs[ID].Materials[j].DiffuseMap.Handle);
                }

                glDeleteShader(this.allOBJs[ID].shader.Handle);

                this.allOBJs.RemoveAt(ID);
                nextUnusedID -= 1;

                File.Delete($"{CORERenderContent.givenCRS.path}\\{ID}.obj");
                if (File.Exists($"{CORERenderContent.givenCRS.path}\\{ID}.mtl"))
                    File.Delete($"{CORERenderContent.givenCRS.path}\\{ID}.mtl");

                int newID = 0;
                for (int i = ID; i < this.allOBJs.Count; i++)
                {
                    if (File.Exists($"{path}\\{i}.obj") && File.Exists($"{path}\\{i}.mtl"))
                    {
                        File.Move($"{path}\\{i}.obj", $"{path}\\{newID}.obj");
                        File.Move($"{path}\\{i}.obj", $"{path}\\{newID}.mtl");
                        newID++;
                    }
                    else if (File.Exists($"{path}\\{i}.obj"))
                    {
                        File.Move($"{path}\\{i}.obj", $"{path}\\{newID}.obj");
                        newID++;
                    }
                }

                string local0 = string.Empty;
                using (FileStream filestream = File.Open($"{path}\\{name}.cst", FileMode.Open))
                    lock (filestream)
                        filestream.SetLength(0);
                if (nextUnusedID != 0)
                {
                    for (int i = 0; i < nextUnusedID; i++)
                    {
                        local0 +=
                        $"""
                        <obj id = "{i}">
                        name = {allOBJs[i].name};
                        objFile = {i}.obj;
                        mtllib = {i}.mtl;
                        scale = {allOBJs[i].Scaling.ToString(CultureInfo.InvariantCulture)};
                        translation = {allOBJs[i].translation.x.ToString(CultureInfo.InvariantCulture)}, {allOBJs[i].translation.y.ToString(CultureInfo.InvariantCulture)}, {allOBJs[i].translation.z.ToString(CultureInfo.InvariantCulture)};
                        rotateX = {allOBJs[i].rotationX.ToString(CultureInfo.InvariantCulture)};
                        rotateY = {allOBJs[i].rotationY.ToString(CultureInfo.InvariantCulture)};
                        rotateZ = {allOBJs[i].rotationZ.ToString(CultureInfo.InvariantCulture)};
                        </obj>

                        """;
                    }
                    using (StreamWriter sw = File.AppendText($"{path}\\{name}.cst"))
                        sw.WriteLine(local0);
                }

                CORERenderContent.givenCRS.RemoveObject(ID);

                if (this.allOBJs.Count > 0)
                {
                    CORERenderContent.HighlightLogic();
                    CORERenderContent.currentObj = ID;
                }
                else
                {
                    CORERenderContent.currentObj = -1;
                    CORERenderContent.loaded = false;
                }
            }
        }
    }
}
