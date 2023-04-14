using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using CORERenderer.shaders;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace CORERenderer.Loaders
{
    public class Model : Readers
    {
        public static int totalSSBOSizeUsed = 0;

        public List<List<float>> vertices;
        public List<List<uint>> indices;

        public List<Material> Materials;

        public List<Submodel> submodels = new();

        public List<Vector3> offsets;

        public Vector3 Scaling = new(1, 1, 1);
        public Vector3 translation = new(0, 0, 0);
        public Vector3 rotation = new(0, 0, 0);

        private Shader shader = GenericShaders.GenericLighting;

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

        private uint VBO, VAO;

        public string amountOfVertices { get 
            {
                if (totalAmountOfVertices / 1000 >= 1) 
                    return $"{MathF.Round(totalAmountOfVertices / 1000):N0}k"; 
                else 
                    return $"{totalAmountOfVertices}"; 
            } }

        public int selectedSubmodel = 0;

        private HDRTexture hdr = null;

        public Model(string path)
        {
            type = COREMain.SetRenderMode(path);

            if (type == RenderMode.ObjFile)
                GenerateObj(path);
            else if (type == RenderMode.JPGImage || type == RenderMode.PNGImage)
                GenerateImage(path);

            else if (type == RenderMode.HDRFile && hdr == null)
                hdr = HDRTexture.ReadFromFile(path);
            else if (type == RenderMode.STLFile)
                generateStl(path);
        }
        
        public Model(string path, List<List<float>> vertices, List<List<uint>> indices, List<Material> materials, List<Vector3> offsets)
        {
            type = COREMain.SetRenderMode(path);

            this.vertices = vertices;
            this.indices = indices;
            this.Materials = materials;
            this.offsets = offsets;

            name = Path.GetFileName(path)[..^4];

            submodels = new();
            this.translation = offsets[0];
            for (int i = 0; i < vertices.Count; i++)
            {

                submodels.Add(new(Materials[i].Name, vertices[i], indices[i], Materials[i]));
                submodels[i].translation = offsets[i] - this.translation;
                submodels[i].parent = this;
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }
            submodels[0].highlighted = true;
            selectedSubmodel = 0;
        }

        public Model() { }

        private void GenerateImage(string path)
        {
            Material material = new() { Texture = FindTexture(path) };
            float width = usedTextures[material.Texture].width * 0.01f;
            float height = usedTextures[material.Texture].height * 0.01f;

            float[] iVertices = new float[48]
            {
                -width / 2, 0.1f, -height / 2,    0, 1,   0, 1, 0,
                -width / 2, 0.1f,  height / 2,    0, 0,   0, 1, 0,
                width / 2,  0.1f,  height / 2,    1, 0,   0, 1, 0,

                -width / 2, 0.1f, -height / 2,    0, 1,   0, 1, 0,
                width / 2,  0.1f,  height / 2,    1, 0,   0, 1, 0,
                width / 2,  0.1f, -height / 2,    1, 1,   0, 1, 0
            };

            GenerateFilledBuffer(out VBO, out VAO, iVertices);

            SetUpShader();

            vertices = new();
            vertices.Add(new());
            foreach (float value in iVertices)
                vertices[0].Add(value);

            Materials = new();
            Materials.Add(material);
        }

        private void generateStl(string path)
        {
            double startedReading = Glfw.Time;
            bool loaded = LoadSTL(path, out name, out List<float> localVertices, out Vector3 offset);
            double readSTLFile = Glfw.Time - startedReading;

            submodels.Add(new(Path.GetFileNameWithoutExtension(path), localVertices, offset, this));

            COREMain.console.WriteDebug($"Read .stl file in {Math.Round(readSTLFile, 2)} seconds");
            COREMain.console.WriteDebug($"Amount of vertices: {submodels[^1].NumberOfVertices}");
            float[] vertexData = localVertices.ToArray();
            unsafe
            { //transfer the vertex data to the compute shader
                glBindBuffer(GL_SHADER_STORAGE_BUFFER, COREMain.ssbo);
                //glBindBufferRange(GL_SHADER_STORAGE_BUFFER, 0, COREMain.ssbo, 0, totalSSBOSizeUsed + vertexData.Length * sizeof(float));

                int size = totalSSBOSizeUsed / sizeof(float) + vertexData.Length;
                glBufferSubData(GL_SHADER_STORAGE_BUFFER, 0, sizeof(int), &size);

                glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(int) + totalSSBOSizeUsed, vertexData.Length * sizeof(float), vertexData);
                totalSSBOSizeUsed += vertexData.Length * sizeof(float);
            }
        }
        
        private void GenerateObj(string path)
        {
            double startedReading = Glfw.Time;
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out offsets, out mtllib);
            double readOBJFile = Glfw.Time - startedReading;

            this.highlighted = true;
            COREMain.scenes[COREMain.selectedScene].currentObj = COREMain.scenes[COREMain.selectedScene].allModels.Count - 1;

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");

            name = Path.GetFileNameWithoutExtension(path);

            startedReading = Glfw.Time;
            if (mtllib != "default")
                loaded = LoadMTL($"{Path.GetDirectoryName(path)}\\{mtllib}", mtlNames, out Materials, out error);
            else
                loaded = LoadMTL($"{COREMain.pathRenderer}\\Loaders\\default.mtl", mtlNames, out Materials, out error);
            double readMTLFile = Glfw.Time - startedReading;

            if (!loaded)
                ErrorLogic(error);

            submodels = new();
            this.translation = offsets[0];
            for (int i = 0; i < vertices.Count; i++)
            {

                submodels.Add(new(Materials[i].Name, vertices[i], indices[i], Materials[i]));
                submodels[i].translation = offsets[i] - this.translation;
                submodels[i].parent = this;
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }

            //depth sorting
            Submodel[] submodelsInCorrectOrder = new Submodel[submodels.Count];
            List<float> distances = new();
            Dictionary<float, Submodel> distanceSubmodelTable = new();
            foreach (Submodel submodel in submodels)
            {
                float distance = submodel.translation.Length;
                distances.Add(distance);
                if (!distanceSubmodelTable.ContainsKey(distance))
                    distanceSubmodelTable.Add(distance, submodel);
                else
                    distanceSubmodelTable.Add(distance + 0.1f, submodel);
            }

            float[] distancesArray = distances.ToArray();
            Array.Sort(distancesArray);
            for (int i = 0; i < distancesArray.Length; i++)
                submodelsInCorrectOrder[i] = distanceSubmodelTable[distancesArray[i]];
            submodels = submodelsInCorrectOrder.ToList();

            submodels[0].highlighted = true;
            selectedSubmodel = 0;

            COREMain.console.WriteDebug($"Read .obj file in {Math.Round(readOBJFile, 2)} seconds");
            COREMain.console.WriteDebug($"Read .mtl file in {Math.Round(readMTLFile, 2)} seconds");
            COREMain.console.WriteDebug($"Amount of vertices: {amountOfVertices}");
        }

        public void Render()
        {
            if (type == RenderMode.ObjFile || type == RenderMode.STLFile)
                RenderModel();

            else if (type == RenderMode.JPGImage)
                RenderImage();

            else if (type == RenderMode.PNGImage)
                RenderImage();
            else if (type == RenderMode.HDRFile)
                Rendering.RenderBackground(hdr);
        }

        public void RenderBackground() => Rendering.RenderBackground(hdr);

        private unsafe void RenderModel()
        {
            for (int i = 0; i < submodels.Count; i++)
            {
                submodels[i].renderLines = renderLines;
                submodels[i].parentModel = Matrix.IdentityMatrix * new Matrix(Scaling, translation) * (MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z));

                if (submodels[i].highlighted)
                    selectedSubmodel = i;

                if (!submodels[i].isTranslucent)
                    submodels[i].Render();
                else
                    translucentSubmodels.Add(submodels[i]);
            }
        }

        private void RenderImage()
        {
            Matrix model = Matrix.IdentityMatrix * MathC.GetScalingMatrix(Scaling) * MathC.GetTranslationMatrix(translation) * MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z);
            shader.SetMatrix("model", model);
            shader.SetInt("material.diffuse", GL_TEXTURE0);
            usedTextures[Materials[0].Texture].Use(GL_TEXTURE0);

            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, vertices[0].Count / 8);
            if (renderLines)
            {
                GL.glLineWidth(1.5f);
                shader.SetVector3("overrideColor", new(1, 0, 1));
                glDrawArrays(PrimitiveType.Lines, 0, vertices[0].Count / 8);
                shader.SetVector3("overrideColor", new(0, 0, 0));
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

        public void Dispose()
        {
            if (COREMain.scenes[COREMain.selectedScene].currentObj == COREMain.scenes[COREMain.selectedScene].allModels.IndexOf(this))
                COREMain.scenes[COREMain.selectedScene].currentObj = -1;
            COREMain.scenes[COREMain.selectedScene].allModels.Remove(this);
            foreach (Submodel sub in submodels)
                sub.Dispose();
        }

        private void SetUpShader()
        {
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

            glBindBuffer(GL_ARRAY_BUFFER, 0);
            glBindVertexArray(0);
        }

        ~Model()
        {
            if (VBO != 0 && VAO != 0)
            {
                glDeleteBuffer(VBO);
                glDeleteVertexArray(VAO);
            }
        }
    }
}