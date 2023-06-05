using COREMath;
using CORERenderer.shaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using CORERenderer.OpenGL;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW;

namespace CORERenderer.Loaders
{
    public class Submodel
    {
        public static bool useRenderDistance = false;
        public static float renderDistance = 100;
        public static bool renderAllIDs = true;

        public Vector3 scaling = new(1, 1, 1), translation = new(0, 0, 0), rotation = new(0, 0, 0);

        public Model parent = null;

        private List<Vertex> vertices = new();
        private bool verticesChanged = false;
        public List<Vertex> Vertices { get { verticesChanged = true; return vertices; } set { verticesChanged = true; vertices = value; } }

        public int NumberOfVertices { get { return vertices.Count; } }

        public bool HasBones { get { for (int i = 0; i < vertices.Count; i++) if (vertices[i].hasBones) return true; return false; } } //inefficient

        public PBRMaterial material;

        private Shader shader = GenericShaders.Lighting;
        private Shader IDShader = GenericShaders.IDPicking;

        public VertexBuffer vbo;

        public int ID;
        private Vector3 IDColor;

        private string name = "PLACEHOLDER";
        public string Name { get { return name; } set { name = value.Length > 10 ? value[..10] : value; } }

        public bool renderLines = false, highlighted = false, isTranslucent = false, hasMaterials = true, renderIDVersion = true, cullFaces = true;

        #region constructors
        /// <summary>
        /// Only use this if you're sure that the variable will get a working value
        /// </summary>
        public Submodel() { }

        public Submodel(string name, List<Vertex> vertices, PBRMaterial material, Model parent)
        {
            this.name = name;
            this.vertices = vertices;
            this.material = material;
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, Model parent)
        {
            this.name = name;
            this.vertices = vertices;
            this.parent = parent;
            this.material = new();
            System.Console.WriteLine(vertices.Count);
            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, List<int> indices, Model parent)
        {
            this.name = name;
            this.vertices = ConvertIndices(vertices, indices);
            this.material = new();
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, List<uint> indices, PBRMaterial material)
        {
            this.Name = name;
            this.Vertices = ConvertIndices(vertices, indices); //the choice is made to merge the vertices and indices so that its easier to work with file formats that dont use indices 
            this.material = material;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, Vector3 offset, Model parent, PBRMaterial material)
        {
            this.Name = name;
            this.Vertices = vertices;
            this.material = material;
            this.translation = offset;
            this.scaling = new(1, 1, 1);
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, Vector3 offset, Vector3 scaling, Model parent)
        {
            this.Name = name;
            this.Vertices = vertices;
            this.material = new();
            this.translation = offset;
            this.scaling = scaling;
            this.parent = parent;
            
            DefaultSetUp();
            
            hasMaterials = false;
        }

        public Submodel(string name, List<Vertex> vertices, Vector3 offset, Vector3 scaling, Vector3 rotation, Model parent, PBRMaterial material)
        {
            this.Name = name;
            this.Vertices = vertices;
            this.material = material;
            this.translation = offset;
            this.scaling = scaling;
            this.rotation = rotation;
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, List<uint> indices, Vector3 translation, Model parent, PBRMaterial material)
        {
            this.Name = name;
            this.Vertices = ConvertIndices(vertices, indices);
            this.translation = translation;
            this.parent = parent;
            this.material = material;

            DefaultSetUp();
        }
        #endregion

        private void DefaultSetUp()
        {
            glLineWidth(1.5f);

            isTranslucent = false;

            hasMaterials = true;

            GenerateBuffers();

            ID = parent.ID;
            IDColor = COREMain.GenerateIDColor(ID);

            GenericShaders.NormalVisualisation.ActivateAttributes();

            //better to move this to genericShaders so that it only happens once and is easier to find
            shader.Use();
            shader.SetInt("albedoMap", 0);
            shader.SetInt("normalMap", 1);
            shader.SetInt("metallicMap", 2);
            shader.SetInt("roughnessMap", 3);
            shader.SetInt("aoMap", 4);
            shader.SetInt("heightMap", 5);
            shader.SetInt("reflectionCubemap", 6);
            shader.SetInt("irradianceMap", 7);
            shader.SetInt("shadowMap", 8);
        }

        public void Render()
        {
            if (!useRenderDistance || MathC.Distance(COREMain.CurrentScene.camera.position, parent.Transform.translation) < renderDistance)
            {
                if (verticesChanged)
                {
                    GenerateBuffers(); //might be better to do glBufferSubData but dont know if thatll work
                    verticesChanged = false;
                }

                shader.Use();

                highlighted = COREMain.selectedID == ID;

                //glStencilFunc(GL_ALWAYS, 1, 0xFF);
                //glStencilMask(0xFF);

                ClampValues();

                SetShaderValues();

                //glBindVertexArray(VAO);
                    
                if (!cullFaces)
                    glDisable(GL_CULL_FACE);

                RenderColorVersion();

                if (COREMain.renderToIDFramebuffer && renderIDVersion && renderAllIDs)
                    RenderIDVersion();

                if (!cullFaces)
                    glEnable(GL_CULL_FACE);
            }
        }

        public void RenderShadowVersion()
        {
            GenericShaders.Shadow.Use();

            GenericShaders.Shadow.SetMatrix("model", parent.Transform.ModelMatrix);

            vbo.Draw(PrimitiveType.Triangles, 0, vertices.Count);
        }

        private void RenderColorVersion()
        {
            if (renderLines)
                vbo.Draw(PrimitiveType.Lines, 0, vertices.Count);
            else
                vbo.Draw(PrimitiveType.Triangles, 0, vertices.Count);
        }

        public void RenderIDVersion()
        {
            COREMain.IDFramebuffer.Bind();

            IDShader.Use();

            IDShader.SetVector3("color", IDColor);
            IDShader.SetMatrix("model", parent.Transform.ModelMatrix);

            vbo.Draw(PrimitiveType.Triangles, 0, vertices.Count);

            COREMain.renderFramebuffer.Bind();
        }

        public void RenderNormals()
        {
            GenericShaders.NormalVisualisation.Use();

            GenericShaders.NormalVisualisation.SetMatrix("model", parent.Transform.ModelMatrix);

            vbo.Draw(PrimitiveType.Triangles, 0, vertices.Count);
        }

        private void SetShaderValues()
        {
            shader.SetMatrix("model", parent.Transform.ModelMatrix);
            shader.SetVector3("viewPos", Rendering.Camera.position);
            shader.SetBool("isHighlighted", parent.highlighted);
            shader.SetFloat("farPlane", Rendering.Camera.FarPlane);

            UseTextures();
        }

        private void UseTextures()
        {
            shader.Use();
            
            material.albedo.Use(ActiveTexture.Texture0);
            material.normal.Use(ActiveTexture.Texture1);
            material.metallic.Use(ActiveTexture.Texture2);
            material.roughness.Use(ActiveTexture.Texture3);
            material.AO.Use(ActiveTexture.Texture4);
            material.height.Use(ActiveTexture.Texture5);
        }

        public static List<Vertex> ConvertIndices(List<Vertex> vertices, List<uint> indices)
        {
            List<Vertex> result = new();
            
            foreach (uint Indice in indices)
                result.Add(vertices[(int)Indice]);

            return result;
        }
        public static List<Vertex> ConvertIndices(List<Vertex> vertices, List<int> indices)
        {
            List<Vertex> result = new();

            foreach (int Indice in indices)
                result.Add(vertices[Indice]);

            return result;
        }

        private void GenerateBuffers()
        {
            vbo = new(vertices);
            shader.Use();

            shader.ActivateAttributes();

            glBindBuffer(BufferTarget.ArrayBuffer, 0);
            glBindVertexArray(0);
        }

        private void ClampValues()
        {
            /*scaling.x = Math.Max(0, scaling.x);
            scaling.y = Math.Max(0, scaling.y);
            scaling.z = Math.Max(0, scaling.z);*/

            rotation.x = Math.Max(0, rotation.x);
            rotation.y = Math.Max(0, rotation.y);
            rotation.z = Math.Max(0, rotation.z);
        }

        public void Dispose()
        {
            vbo.Dispose();
            //should also dispose of the materials (if they arent being used by other models) but thats not that important since vram isnt a main concern
        }
    }
}