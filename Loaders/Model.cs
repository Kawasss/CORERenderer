using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.shaders;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using System.Drawing;

namespace CORERenderer.Loaders
{
    public class Model : Readers
    {
        public List<List<float>> vertices;
        public List<List<uint>> indices;

        public List<Material> Materials;

        public List<Submodel> submodels;

        public Vector3 Scaling = new(1, 1, 1);
        public Vector3 translation = new(0, 0, 0);
        public Vector3 rotation = new(0, 0, 0);

        public RenderMode type;

        public string name = "PLACEHOLDER";

        public List<string> submodelNames = new();

        public bool renderNormals = false;

        public bool highlighted = false;

        public bool renderLines = false;

        public string mtllib;

        public int ID;

        public int debug = 0;
        private int totalAmountOfVertices = 0;

        public string amountOfVertices { get 
            {
                if (totalAmountOfVertices / 1000 >= 1) 
                    return $"{MathF.Round(totalAmountOfVertices / 1000):N0}k"; 
                else 
                    return $"{totalAmountOfVertices}"; 
            } }

        public int selectedSubmodel = 0;

        private Image3D GivenImage;

        private HDRTexture hdr = null;

        public Model(string path)
        {
            type = COREMain.SetRenderMode(path);

            if (type == RenderMode.ObjFile)
            {
                GenerateObj(path);
                return;
            }

            else if (type == RenderMode.JPGImage || type == RenderMode.PNGImage || type == RenderMode.RPIFile)
                GivenImage = Image3D.LoadImageIn3D(type, path);

            else if (type == RenderMode.HDRFile && hdr == null)
                hdr = HDRTexture.ReadFromFile(path);
        }

        public void Render()
        {
            if (type == RenderMode.ObjFile)
                RenderObj();

            else if (type == RenderMode.JPGImage)
                GivenImage.Render();

            else if (type == RenderMode.PNGImage)
                GivenImage.Render();
            else if (type == RenderMode.HDRFile)
                return;
        }

        public void RenderBackground() => Rendering.RenderBackground(hdr);

        private unsafe void RenderObj() //better to make this extend to rendereveryframe() or new render override
        {
            /*int i = 0;
            foreach (Submodel submodel in submodels)
            {
                submodel.renderLines = renderLines;
                submodel.parentModel = Matrix.IdentityMatrix * new Matrix(Scaling, translation) * (MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z));

                if (submodel.highlighted)
                    selectedSubmodel = i;

                submodel.Render();
                i++;
            }*/
            for (int i = submodels.Count - 1; i >= 0; i--)
            {
                submodels[i].renderLines = renderLines;
                submodels[i].parentModel = Matrix.IdentityMatrix * new Matrix(Scaling, translation) * (MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z));

                if (submodels[i].highlighted)
                    selectedSubmodel = i;

                submodels[i].Render();
            }
        }

        public void GenerateObj(string path)
        {
            double startedReading = Glfw.Time;
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out List<Vector3> offsets, out mtllib);
            double readOBJFile = Glfw.Time - startedReading;

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");

            List<int> temp = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);

            name = path[(temp[^1] + 1)..path.IndexOf(".obj")];

            startedReading = Glfw.Time;
            if (mtllib != "default")
                loaded = LoadMTL($"{path[..(temp[^1] + 1)]}{mtllib}", mtlNames, out Materials, out error);
            else
                loaded = LoadMTL($"{COREMain.pathRenderer}\\Loaders\\default.mtl", mtlNames, out Materials, out error);
            double readMTLFile = Glfw.Time - startedReading;

            if (!loaded)
                ErrorLogic(error);

            submodels = new();
            for (int i = 0; i < vertices.Count; i++)
            {
                submodels.Add(new(Materials[i].Name, vertices[i], indices[i], Materials[i]));
                submodels[i].translation = offsets[i];
                totalAmountOfVertices += submodels[^1].numberOfVertices;


                COREMain.renderFramebuffer.Bind();
                Render(); //renders the submodel to make the app not crash

                glViewport(COREMain.viewportX, COREMain.viewportY, COREMain.renderWidth, COREMain.renderHeight);

                COREMain.renderFramebuffer.RenderFramebuffer();
                if (COREMain.renderIDFramebuffer)
                {
                    glViewport((int)(COREMain.viewportX + COREMain.renderWidth * 0.75f), (int)(COREMain.viewportY + COREMain.renderHeight * 0.75f), (int)(COREMain.renderWidth * 0.25f), (int)(COREMain.renderHeight * 0.25f));
                    COREMain.IDFramebuffer.RenderFramebuffer();
                }
                glViewport(0, 0, COREMain.monitorWidth, COREMain.monitorHeight);

                Glfw.SwapBuffers(COREMain.window);
            }

            COREMain.console.WriteLine($"Read .obj file in {Math.Round(readOBJFile, 2)} seconds");
            COREMain.console.WriteLine($"Read .mtl file in {Math.Round(readMTLFile, 2)} seconds");
            COREMain.console.WriteLine($"Amount of vertices: {amountOfVertices}");
        }

        private void ErrorLogic(int error)
        {
            switch (error)
            {
                case -1:
                    throw new GLFW.Exception($"Invalid file format for {name}, should end with .mtl, not {mtllib[mtllib.IndexOf('.')..]} (error == -1)");
                case 0:
                    Console.WriteLine($"No material library found for {name} (error == 0)");
                    break;
                case 1:
                    break;
                default:
                    throw new GLFW.Exception($"Undefined error: {error}");
            }
        }
    }
}
