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

        public Vector3 scaling = new(1, 1, 1), translation = new(0, 0, 0), rotation = new(0, 0, 0);

        public Model parent = null;

        private List<Vertex> vertices = new();
        private bool verticesChanged = false;
        public List<Vertex> Vertices { get { verticesChanged = true; return vertices; } set { verticesChanged = true; vertices = value; } }

        public int NumberOfVertices { get { return vertices.Count; } }

        public readonly Material material;

        private Shader shader = GenericShaders.GenericLighting;
        private Shader IDShader = GenericShaders.IDPicking;

        public uint VBO, VAO;

        public int ID;
        private Vector3 IDColor;

        private string name = "PLACEHOLDER";
        public string Name { get { return name; } set { name = value.Length > 10 ? value[..10] : value; } }

        public bool renderLines = false, highlighted = false, isTranslucent = false, hasMaterials = true, renderIDVersion = true, cullFaces = true;

        #region constructors
        public Submodel(string name, List<Vertex> vertices, List<uint> indices, Material material)
        {
            this.Name = name;
            this.Vertices = ConvertIndices(vertices, indices); //the choice is made to merge the vertices and indices so that its easier to work with file formats that dont use indices 
            this.material = material;
            isTranslucent = material.Transparency != 1;

            DefaultSetUp();
        }

        public Submodel(string name, List<Vertex> vertices, Vector3 offset, Model parent, Material material)
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
            
            material.Transparency = 1;
            material.Texture = 2;
        }

        public Submodel(string name, List<Vertex> vertices, Vector3 offset, Vector3 scaling, Vector3 rotation, Model parent, Material material)
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

        public Submodel(string name, List<Vertex> vertices, List<uint> indices, Vector3 translation, Model parent, Material material)
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

            isTranslucent = material.Transparency != 1;

            hasMaterials = true;

            GenerateBuffers();

            ID = parent.ID;
            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.Texture", GL_TEXTURE0);
            shader.SetInt("material.diffuse", GL_TEXTURE1);
            shader.SetInt("material.specular", GL_TEXTURE2);
            shader.SetInt("material.normalMap", GL_TEXTURE3);
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

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);

                ClampValues();

                SetShaderValues();

                glBindVertexArray(VAO);
                    
                if (!cullFaces)
                    glDisable(GL_CULL_FACE);

                RenderColorVersion();

                if (COREMain.renderToIDFramebuffer && renderIDVersion)
                    RenderIDVersion();

                if (!cullFaces)
                    glEnable(GL_CULL_FACE);
            }
        }

        private void RenderColorVersion()
        {
            if (renderLines)
                glDrawArrays(PrimitiveType.Lines, 0, vertices.Count);
            else
                glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
        }

        private void RenderIDVersion()
        {
            COREMain.IDFramebuffer.Bind();

            IDShader.Use();

            IDShader.SetVector3("color", IDColor);
            IDShader.SetMatrix("model", parent.Transform.ModelMatrix);

            glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            COREMain.renderFramebuffer.Bind();
        }

        private void SetShaderValues()
        {
            shader.SetFloat("transparency", material.Transparency);
            shader.SetBool("allowAlpha", COREMain.allowAlphaOverride);

            shader.SetMatrix("model", parent.Transform.ModelMatrix);

            UseTextures();
        }

        private void UseTextures()
        {
            usedTextures[material.Texture].Use(GL_TEXTURE0);
            usedTextures[material.DiffuseMap].Use(GL_TEXTURE1);
            usedTextures[material.SpecularMap].Use(GL_TEXTURE2);
            usedTextures[material.NormalMap].Use(GL_TEXTURE3);
        }

        public static List<Vertex> ConvertIndices(List<Vertex> vertices, List<uint> indices)
        {
            List<Vertex> result = new();
            
            foreach (uint Indice in indices)
                result.Add(vertices[(int)Indice]);

            return result;
        }

        private void GenerateBuffers()
        {
            GenerateFilledBuffer(out VBO, out VAO, Vertices.ToArray());

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

        ~Submodel()
        {
            //glDeleteBuffer(VBO);
            //glDeleteVertexArray(VAO);
        }
    }
}