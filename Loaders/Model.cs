using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using CORERenderer.shaders;
using Console = CORERenderer.GUI.Console;

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
        public List<List<Vertex>> Vertices { get { List<List<Vertex>> value = new(); foreach (Submodel s in submodels) value.Add(s.Vertices); return value; } } //adds the vertices from the submodels into one list

        public List<Material> Materials { get { List<Material> value = new(); foreach (Submodel s in submodels) value.Add(s.material); return value; } } //adds the materials from the submodels into one list

        /// <summary>
        /// Gives the current translations of all of the submodels
        /// </summary>
        public List<Vector3> Offsets { get { List<Vector3> value = new(); value.Add(transform.translation); foreach (Submodel s in submodels) value.Add(s.translation); return value; } } //adds the materials from the submodels into one list

        public Submodel CurrentSubmodel { get { return submodels[selectedSubmodel]; } }

        public string AmountOfVertices { get { string value = totalAmountOfVertices / 1000 >= 1 ? $"{MathF.Round(totalAmountOfVertices / 1000):N0}k" : $"{totalAmountOfVertices}"; return value; } }

        public string Name { get { return name; } set { name = value.Length > 10 ? value[..10] : value; } }

        public bool CanBeCulled { get { return !transform.BoundingBox.IsInFrustum(COREMain.CurrentScene.camera.Frustum, transform); } }
        #endregion

        public List<Submodel> submodels = new();

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

        private Transform transform = new();
        public Transform Transform { get { return transform; } set { transform = value; } } //!!REMOVE SET WHEN DONE DEBUGGING

        public Model(string path)
        {
            ID = COREMain.NewAvaibleID;
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
        
        public Model(string path, List<List<Vertex>> vertices, List<List<uint>> indices, List<Material> materials, List<Vector3> offsets, Vector3 center, Vector3 extents)
        {
            ID = COREMain.NewAvaibleID;
            type = COREMain.SetRenderMode(path);

            Name = Path.GetFileName(path)[..^4];

            submodels = new();
            this.transform = new(offsets[0], Vector3.Zero, new(1, 1, 1), extents, center);
            int amountOfFailures = 0;

            for (int i = 0; i < vertices.Count; i++)
            {
                try
                {
                    submodels.Add(new(materials[i].Name, vertices[i], indices[i], offsets[i] - this.Transform.translation, this, materials[i]));
                    totalAmountOfVertices += submodels[^1].NumberOfVertices;
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteError($"Couldn't create submodel {i} out of {vertices.Count - 1} for model {COREMain.CurrentScene.models.Count} \"{Name}\"");
                    amountOfFailures++;
                    continue;
                }
            }
            if (amountOfFailures >= vertices.Count - 1)
                terminate = true;
            if (!terminate)
            {
                submodels[0].highlighted = true;
                selectedSubmodel = 0;
            }
        }

        public Model() { ID = COREMain.NewAvaibleID; }

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
            Vector3 max = new(width / 2, 0.1f, height / 2);
            Vector3 min = new(-width / 2, 0.1f, -height / 2);
            Vector3 center = (min + max) * 0.5f;
            Vector3 extents = max - center;
            this.transform = new(Vector3.Zero, Vector3.Zero, new(1, 1, 1), extents, center);
            submodels.Add(new(Name, Vertex.GetVertices(iVertices.ToList()), Vector3.Zero, this, material));
            submodels[^1].cullFaces = true;
        }

        private void GenerateStl(string path)
        {
            double startedReading = Glfw.Time;
            Error loaded = LoadSTL(path, out name, out List<float> localVertices, out Vector3 offset);
            double readSTLFile = Glfw.Time - startedReading;

            Vector3 min = Vector3.Zero, max = Vector3.Zero;
            for (int i = 0; i < localVertices.Count; i += 8)
            {
                max.x = localVertices[i] > max.x ? localVertices[i] : max.x;
                max.y = localVertices[i + 1] > max.y ? localVertices[i + 1] : max.y;
                max.z = localVertices[i + 2] > max.z ? localVertices[i + 2] : max.z;

                min.x = localVertices[i] < min.x ? localVertices[i] : min.x;
                min.y = localVertices[i + 1] < min.y ? localVertices[i + 1] : min.y;
                min.z = localVertices[i + 2]< min.z ? localVertices[i + 2] : min.z;
            }
            Vector3 center = (min + max) * 0.5f;
            Vector3 extents = max - center;
            transform = new(offset, Vector3.Zero, new(1, 1, 1), extents, center);

            if (loaded != Error.None)
            {
                this.error = loaded;
                terminate = true;
                return;
            }

            submodels.Add(new(Path.GetFileNameWithoutExtension(path), Vertex.GetVertices(localVertices), offset, new(1, 1, 1), this));

            Console.WriteDebug($"Read .stl file in {Math.Round(readSTLFile, 2)} seconds");
            Console.WriteDebug($"Amount of vertices: {submodels[^1].NumberOfVertices}");
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
            Error loaded = LoadOBJ(path, out List<string> mtlNames, out List<List<Vertex>> lVertices, out List<List<uint>> indices, out List<Vector3> lOffsets, out Vector3 center, out Vector3 extents, out mtllib);
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

            this.transform = new(lOffsets[0], Vector3.Zero, new(1, 1, 1), extents, center);
            for (int i = 0; i <  lVertices.Count; i++)
            {
                submodels.Add(new(materials[i].Name, lVertices[i], indices[i], lOffsets[i] - this.Transform.translation, this, materials[i]));
                totalAmountOfVertices += submodels[^1].NumberOfVertices;
            }

            //SortSubmodelsByDepth();

            submodels[0].highlighted = true;
            selectedSubmodel = 0;

            Console.WriteDebug($"Read .obj file in {Math.Round(readOBJFile, 2)} seconds");
            Console.WriteDebug($"Read .mtl file in {Math.Round(readMTLFile, 2)} seconds");
            Console.WriteDebug($"Amount of vertices: {AmountOfVertices}");
        }

        private void SortSubmodelsByDepth()
        {
            Submodel[] submodelsInCorrectOrder = new Submodel[submodels.Count];
            List<float> distances = new();
            Dictionary<float, Submodel> distanceSubmodelTable = new();

            foreach (Submodel submodel in submodels)
            {
                float distance = submodel.translation.Length;
                while (distanceSubmodelTable.ContainsKey(distance))
                    distance += 0.01f;
                distances.Add(distance);
                distanceSubmodelTable.Add(distance, submodel);
            }

            float[] distancesArray = distances.ToArray();
            Array.Sort(distancesArray);
            for (int i = 0; i < distancesArray.Length; i++)
                submodelsInCorrectOrder[i] = distanceSubmodelTable[distancesArray[i]];
            submodels = submodelsInCorrectOrder.ToList();
        }

        public void Reset()
        {
            transform = new();
        }

        public void Dispose()
        {
            if (COREMain.CurrentScene.currentObj == COREMain.CurrentScene.models.IndexOf(this))
                COREMain.CurrentScene.currentObj = -1;
            terminate = true;
        }
    }
}