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
        private Vector3 previousScaling = new(1, 1, 1), previousTranslation = new(0, 0, 0), previousRotation = new(0, 0, 0);

        public Matrix parentModel;
        public Model parent = null;

        public readonly List<float> vertices;

        public int NumberOfVertices { get { return vertices.Count / 8; } }

        public readonly Material material;

        private Shader shader = GenericShaders.GenericLighting;
        private Shader IDShader = GenericShaders.IDPicking;

        public uint VBO, VAO;

        public int ID = COREMain.NewAvaibleID;
        private Vector3 IDColor;

        private string name = "PLACEHOLDER";
        public string Name { get { return name; } set { name = value.Length > 10 ? value[..10] : value; } }

        public bool renderLines = false, highlighted = false, isTranslucent = false, hasMaterials = true, renderIDVersion = true, cullFaces = true;

        private Matrix model = Matrix.IdentityMatrix;

        #region constructors
        public Submodel(string name, List<float> vertices, List<uint> indices, Material material)
        {
            this.Name = name;
            this.vertices = ConvertIndices(vertices, indices); //the choice is made to merge the vertices and indices so that its easier to work with file formats that dont use indices 
            this.material = material;
            isTranslucent = material.Transparency != 1;

            DefaultSetUp();
        }

        public Submodel(string name, List<float> vertices, Vector3 offset, Model parent, Material material)
        {
            this.Name = name;
            this.vertices = vertices;
            this.material = material;
            this.translation = offset;
            this.scaling = new(1, 1, 1);
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<float> vertices, Vector3 offset, Vector3 scaling, Model parent)
        {
            this.Name = name;
            this.vertices = vertices;
            this.material = new();
            this.translation = offset;
            this.scaling = scaling;
            this.parent = parent;
            
            DefaultSetUp();
            
            hasMaterials = false;
            
            material.Transparency = 1;
            material.Texture = 2;
        }

        public Submodel(string name, List<float> vertices, Vector3 offset, Vector3 scaling, Vector3 rotation, Model parent, Material material)
        {
            this.Name = name;
            this.vertices = vertices;
            this.material = material;
            this.translation = offset;
            this.scaling = scaling;
            this.rotation = rotation;
            this.parent = parent;

            DefaultSetUp();
        }

        public Submodel(string name, List<float> vertices, List<uint> indices, Vector3 translation, Model parent, Material material)
        {
            this.Name = name;
            this.vertices = ConvertIndices(vertices, indices);
            this.translation = translation;
            this.parent = parent;
            this.material = material;

            DefaultSetUp();
        }
        #endregion

        private void DefaultSetUp()
        {
            glLineWidth(1.5f);

            hasMaterials = true;

            GenerateBuffers();

            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.Texture", GL_TEXTURE0);
            shader.SetInt("material.diffuse", GL_TEXTURE1);
            shader.SetInt("material.specular", GL_TEXTURE2);
            shader.SetInt("material.normalMap", GL_TEXTURE3);

            previousScaling = scaling + parent.scaling;
            previousTranslation = translation + parent.translation;
            previousRotation = rotation + parent.rotation;
        }

        public void Render()
        {
            if (!useRenderDistance || MathC.Distance(COREMain.CurrentScene.camera.position, translation + parent.translation) < renderDistance)
            {
                shader.Use();

                highlighted = COREMain.selectedID == ID;

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);

                ClampValues();

                SetModel();

                SetShaderValues();

                glBindVertexArray(VAO);
                unsafe
                {
                    if (!cullFaces)
                        glDisable(GL_CULL_FACE);

                    RenderColorVersion();

                    if (COREMain.renderToIDFramebuffer && renderIDVersion)
                        RenderIDVersion();

                    if (!cullFaces)
                        glEnable(GL_CULL_FACE);
                }
                previousScaling = scaling + parent.scaling;
                previousTranslation = translation + parent.translation;
                previousRotation = rotation + parent.rotation;
            }
        }

        private void RenderColorVersion()
        {
            if (renderLines)
                glDrawArrays(PrimitiveType.Lines, 0, vertices.Count / 8);
            else
                glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 8);
        }

        private void RenderIDVersion()
        {
            COREMain.IDFramebuffer.Bind();

            IDShader.Use();

            IDShader.SetVector3("color", IDColor);
            IDShader.SetMatrix("model", model);

            glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 8);

            if (!highlighted)
                highlighted = COREMain.selectedID == ID;
            else if (highlighted && Glfw.GetMouseButton(COREMain.window, MouseButton.Left) == InputState.Press)
                highlighted = false;

            COREMain.renderFramebuffer.Bind();
        }

        private void SetModel()//doesnt create a new matrix if the model hasnt moved in any way, this reduces amount of calculations per frame
        {
            if (previousRotation != rotation + parent.rotation || previousScaling != scaling + parent.scaling || previousTranslation != translation + parent.translation)
                model = Matrix.IdentityMatrix * MathC.GetRotationMatrix(this.rotation + parent.rotation) * MathC.GetScalingMatrix(this.scaling + parent.scaling) * MathC.GetTranslationMatrix(this.translation + parent.translation);
        }

        private void SetShaderValues()
        {
            shader.SetVector3("viewPos", COREMain.CurrentScene.camera.position);
            shader.SetFloat("transparency", material.Transparency);
            shader.SetBool("allowAlpha", COREMain.allowAlphaOverride);

            shader.SetMatrix("model", model);

            UseTextures();
        }

        private void UseTextures()
        {
            usedTextures[material.Texture].Use(GL_TEXTURE0);
            usedTextures[material.DiffuseMap].Use(GL_TEXTURE1);
            usedTextures[material.SpecularMap].Use(GL_TEXTURE2);
            usedTextures[material.NormalMap].Use(GL_TEXTURE3);
        }

        private static List<float> ConvertIndices(List<float> vertices, List<uint> indices)
        {
            List<float> result = new();
            
            foreach (uint Indice in indices)
            {
                uint indice = Indice * 8;
                for (int i = 0; i < 8; i++)
                    result.Add(vertices[(int)indice + i]);
            }

            return result;
        }

        private void GenerateBuffers()
        {
            GenerateFilledBuffer(out VBO, out VAO, vertices.ToArray());

            shader.Use();

            shader.ActivateGenericAttributes();

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
            glDeleteBuffer(VBO);
            glDeleteVertexArray(VAO);
        }
    }
}