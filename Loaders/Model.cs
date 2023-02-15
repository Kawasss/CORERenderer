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

        public int selectedSubmodel = 0;

        private Image3D GivenImage;

        private HDRTexture hdr = null;

        public Model(string path)
        {
            type = COREMain.SetRenderMode(path);

            if (type == RenderMode.ObjFile)
                GenerateObj(path);

            else if (type == RenderMode.JPGImage)
                GivenImage = Image3D.LoadImageIn3D(false, path);

            else if (type == RenderMode.PNGImage)
                GivenImage = Image3D.LoadImageIn3D(true, path);

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
            foreach (Submodel submodel in submodels)
            {
                submodel.renderLines = renderLines;

                if (!highlighted)
                    highlighted = submodel.highlighted;

                submodel.rotation = rotation;
                submodel.translation = translation;
                submodel.scaling = Scaling;

                submodel.Render();
            }
            //rotation = Vector3.Zero;
            //translation = Vector3.Zero;
            //Scaling = new(0, 0, 0);
        }

        public void GenerateObj(string path)
        {
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out mtllib);

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");

            List<int> temp = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);

            name = path[(temp[^1] + 1)..path.IndexOf(".obj")];

            if (mtllib != "default")
                loaded = LoadMTL($"{path[..(temp[^1] + 1)]}{mtllib}", mtlNames, out Materials, out error);
            else
                loaded = LoadMTL($"{COREMain.pathRenderer}\\Loaders\\default.mtl", mtlNames, out Materials, out error);

            if (!loaded)
                ErrorLogic(error);

            submodels = new();
            for (int i = 0; i < vertices.Count; i++)
                submodels.Add(new(Materials[i].Name, vertices[i], indices[i], Materials[i]));
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
