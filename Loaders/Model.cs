using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.shaders;
using CORERenderer.OpenGL;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public class Model : Readers
    {
        public List<List<float>> vertices;
        public List<List<uint>> indices;

        public List<Material> Materials;

        public PBRMaterial material; //remove when done debugging pbr

        public RenderMode type;

        public Shader shader = GenericShaders.GenericLightingShader;

        public string name = "PLACEHOLDER";

        //not safe to make them public but is needed to delete them
        public List<uint> GeneratedBuffers = new();
        public List<uint> GeneratedVAOs = new();
        public List<uint> elementBufferObject = new();

        public List<Vector3> Scaling = new();
        public List<Vector3> translation = new();
        public List<Vector3> rotation = new();

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

        public Model() { }

        public Model(string objPath, string mtlPath)
        {
            bool loaded = LoadOBJ(objPath, out List<string> mtlNames, out vertices, out indices, out mtllib);

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
            if (objPath != null || !File.Exists(objPath))
                loaded = LoadMTL(mtlPath, mtlNames, out Materials, out error);
            else
                loaded = LoadMTL(null, mtlNames, out Materials, out error);
            if (!loaded)
                ErrorLogic(error);

            GenerateBuffers();

            shader.SetInt("material.diffuse", GL_TEXTURE0);
            shader.SetInt("material.specular", GL_TEXTURE1);
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
            shader.Use();

            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glStencilMask(0xFF);

            shader.SetVector3("viewPos", COREMain.scenes[COREMain.selectedScene].camera.position);

            for (int i = 0; i < vertices.Count; i++)
            {
                shader.Use();

                ClampValues(i);

                shader.SetMatrix("model", Matrix.IdentityMatrix
                * new Matrix(Scaling[i], translation[i])
                * (MathC.GetRotationXMatrix(rotation[i].x)
                * MathC.GetRotationYMatrix(rotation[i].y)
                * MathC.GetRotationZMatrix(rotation[i].z)));

                glBindVertexArray(GeneratedVAOs[i]);

                usedTextures[Materials[i].Texture].Use(GL_TEXTURE0);
                usedTextures[Materials[i].SpecularMap].Use(GL_TEXTURE1);

                if (renderLines)
                    glDrawElements(PrimitiveType.Lines, indices[i].Count, GLType.UnsingedInt, (void*)0);
                else
                    glDrawElements(PrimitiveType.Triangles, indices[i].Count, GLType.UnsingedInt, (void*)0);
            }
        }

        private void GenerateBuffers()
        {
            GeneratedBuffers = new();
            GeneratedVAOs = new();
            elementBufferObject = new();

            for (int i = 0; i < vertices.Count; i++)
            {
                //gets current vertices and puts in a buffer
                GenerateFilledBuffer(out uint buffer, out uint GeneratedVAO, vertices[i].ToArray());
                GeneratedBuffers.Add(buffer);

                //could be put in a for loop but not that necessary
                GeneratedVAOs.Add(GeneratedVAO);

                //3D coordinates
                int vertexLocation = shader.GetAttribLocation("aPos");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);

                //UV texture coordinates
                vertexLocation = shader.GetAttribLocation("aTexCoords");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float))); }
                glEnableVertexAttribArray((uint)vertexLocation);

                //normal coordinates
                vertexLocation = shader.GetAttribLocation("aNormal");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float))); }
                glEnableVertexAttribArray((uint)vertexLocation);

                //adds ebo to the vao
                GenerateFilledBuffer(out uint local3, indices[i].ToArray());
                elementBufferObject.Add(local3);
                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);
            }
        }

        public void GenerateObj(string path)
        {
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out mtllib);
            GenerateBuffers();

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
            if (path != null || !File.Exists(path))
            {
                List<int> temp = new();
                for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                    temp.Add(i);

                name = path[(temp[^1] + 1)..path.IndexOf(".obj")];

                if (mtllib != "default")
                    loaded = LoadMTL($"{path[..(temp[^1] + 1)]}{mtllib}", mtlNames, out Materials, out error);
                else
                    loaded = LoadMTL($"{COREMain.pathRenderer}\\Loaders\\default.mtl", mtlNames, out Materials, out error);
            }
            else
                loaded = LoadMTL(null, mtlNames, out Materials, out error);

            if (!loaded)
                ErrorLogic(error);

            for (int i = 0; i < vertices.Count; i++)
            {
                translation.Add(new(0, 0, 0));
                rotation.Add(new(0, 0, 0));
                Scaling.Add(new(1, 1, 1));
                submodelNames.Add(Materials[i].Name);
                Console.WriteLine(Materials[i].Name);
            }
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

        private void ClampValues(int index)
        {
            if (Scaling[index].x < 0.01f)
                Scaling[index].x = 0.01f;
            if (Scaling[index].y < 0.01f)
                Scaling[index].y = 0.01f;
            if (Scaling[index].z < 0.01f)
                Scaling[index].z = 0.01f;
            if (rotation[index].x >= 360)
                rotation[index].x = 0;
            if (rotation[index].y >= 360)
                rotation[index].y = 0;
            if (rotation[index].z >= 360)
                rotation[index].z = 0;
        }
    }  
}
