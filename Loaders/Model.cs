using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using CORERenderer.shaders;

namespace CORERenderer.Loaders
{
    public partial class Model : Readers
    {
        public static Model Cube { get { return new($"{COREMain.pathRenderer}\\OBJs\\cube.stl"); } }
        public static Model Cylinder { get { return new($"{COREMain.pathRenderer}\\OBJs\\cylinder.stl"); } }
        public static Model Plane { get { return new($"{COREMain.pathRenderer}\\OBJs\\plane.stl"); } }

        public static int totalSSBOSizeUsed = 0;

        #region Properties
        /// <summary>
        /// Gives the vertices of the submodels, each submodel is a new list. Translations are not applied to this.
        /// </summary>
        public List<List<float>> Vertices { get { List<List<float>> value = new(); foreach (Submodel s in submodels) value.Add(s.vertices); return value; } } //adds the vertices from the submodels into one list

        public List<Material> Materials { get { List<Material> value = new(); foreach (Submodel s in submodels) value.Add(s.material); return value; } } //adds the materials from the submodels into one list

        /// <summary>
        /// Gives the current translations of all of the submodels
        /// </summary>
        public List<Vector3> Offsets { get { List<Vector3> value = new(); foreach (Submodel s in submodels) value.Add(s.translation); return value; } } //adds the materials from the submodels into one list

        public Submodel CurrentSubmodel { get { return submodels[selectedSubmodel]; } }

        public string AmountOfVertices { get { string value = totalAmountOfVertices / 1000 >= 1 ? $"{MathF.Round(totalAmountOfVertices / 1000):N0}k" : $"{totalAmountOfVertices}"; return value; } }

        public string Name { get { return name; } set { name = value.Length > 10 ? value[..10] : value; } }
        #endregion

        public List<Submodel> submodels = new();

        public Vector3 scaling = new(1, 1, 1), translation = new(0, 0, 0), rotation = new(0, 0, 0);

        private readonly Shader shader = GenericShaders.GenericLighting;

        public RenderMode type;
        public Error error = Error.None;

        private string name = "PLACEHOLDER";
        
        public bool highlighted = false, renderLines = false, renderNormals = false, terminate = false;

        public string mtllib;

        public int ID;

        private int totalAmountOfVertices = 0;

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
                GenerateStl(path);
        }
        
        public Model(string path, List<List<float>> vertices, List<List<uint>> indices, List<Material> materials, List<Vector3> offsets)
        {
            type = COREMain.SetRenderMode(path);

            Name = Path.GetFileName(path)[..^4];

            submodels = new();
            this.translation = offsets[0];
            for (int i = 0; i < vertices.Count; i++)
            {
                submodels.Add(new(Materials[i].Name, vertices[i], indices[i], offsets[i] - this.translation, this, Materials[i]));
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }
            submodels[0].highlighted = true;
            selectedSubmodel = 0;
        }

        public Model() { }

        private void GenerateImage(string path)
        {
            Name = Path.GetFileNameWithoutExtension(path);
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
            submodels.Add(new(Name, iVertices.ToList(), Vector3.Zero, this, material));
            submodels[^1].cullFaces = true;
        }

        private void GenerateStl(string path)
        {
            double startedReading = Glfw.Time;
            Error loaded = LoadSTL(path, out name, out List<float> localVertices, out Vector3 offset);
            double readSTLFile = Glfw.Time - startedReading;

            if (loaded != Error.None)
            {
                this.error = loaded;
                terminate = true;
                return;
            }

            submodels.Add(new(Path.GetFileNameWithoutExtension(path), localVertices, offset, new(1, 1, 1), this));

            COREMain.console.WriteDebug($"Read .stl file in {Math.Round(readSTLFile, 2)} seconds");
            COREMain.console.WriteDebug($"Amount of vertices: {submodels[^1].NumberOfVertices}");
            float[] vertexData = localVertices.ToArray();
            unsafe
            { //transfer the vertex data to the compute shader
                glBindBuffer(BufferTarget.ShaderStorageBuffer, COREMain.ssbo);
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
            Error loaded = LoadOBJ(path, out List<string> mtlNames, out List<List<float>> lVertices, out List<List<uint>> indices, out List<Vector3> lOffsets, out mtllib);
            double readOBJFile = Glfw.Time - startedReading;

            if (loaded != Error.None)
            {
                this.error = loaded;
                terminate = true;
                return;
            }

            Name = Path.GetFileNameWithoutExtension(path);

            startedReading = Glfw.Time;

            //decide to load the default mtl file or not
            loaded = mtllib != "default" ? LoadMTL($"{Path.GetDirectoryName(path)}\\{mtllib}", mtlNames, out List<Material> materials) : LoadMTL($"{COREMain.pathRenderer}\\Loaders\\default.mtl", mtlNames, out materials);

            double readMTLFile = Glfw.Time - startedReading;

            if (loaded != Error.None)
            {
                this.error = loaded;
                terminate = true;
                return;
            }

            this.translation = lOffsets[0];
            for (int i = 0; i <  lVertices.Count; i++)
            {
                submodels.Add(new(materials[i].Name, lVertices[i], indices[i], lOffsets[i] - this.translation, this, materials[i]));
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }

            SortSubmodelsByDepth();

            submodels[0].highlighted = true;
            selectedSubmodel = 0;

            COREMain.console.WriteDebug($"Read .obj file in {Math.Round(readOBJFile, 2)} seconds");
            COREMain.console.WriteDebug($"Read .mtl file in {Math.Round(readMTLFile, 2)} seconds");
            COREMain.console.WriteDebug($"Amount of vertices: {AmountOfVertices}");
        }

        private void SortSubmodelsByDepth()
        {
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
        }

        public void Reset()
        {
            translation = Vector3.Zero;
            rotation = Vector3.Zero;
            scaling = new(1);
        }

        public void Dispose()
        {
            if (COREMain.CurrentScene.currentObj == COREMain.CurrentScene.models.IndexOf(this))
                COREMain.CurrentScene.currentObj = -1;
            terminate = true;
        }

        private void SetUpShader()
        {
            shader.ActivateGenericAttributes();

            glBindBuffer(BufferTarget.ArrayBuffer, 0);
            glBindVertexArray(0);
        }
    }
}