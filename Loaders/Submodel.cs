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

        public Vector3 scaling = new(1, 1, 1);
        public Vector3 translation = new(0, 0, 0);
        public Vector3 rotation = new(0, 0, 0);

        public Matrix parentModel;
        public Model parent = null;

        public readonly List<float> vertices;

        public int NumberOfVertices { get { return vertices.Count / 8; } }

        private Material material;

        private Shader shader = GenericShaders.GenericLighting;
        private Shader IDShader = GenericShaders.IDPicking;

        private uint VBO, VAO;

        public int ID = COREMain.NewAvaibleID;
        private Vector3 IDColor;

        private string name;
        public string Name { get { return name; } }

        public bool renderLines = false;

        public bool highlighted = false;

        public bool isTranslucent = false;

        public bool hasMaterials = true;

        public Submodel(string name, List<float> vertices, List<uint> indices, Material material)
        {
            this.name = name;
            this.vertices = ConvertIndices(vertices, indices); //the choice is made to merge the vertices and indices so that its easier to work with file formats that dont use indices 
            //this.indices = indices;
            this.material = material;

            isTranslucent = material.Transparency != 1;

            GenerateBuffers();

            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.Texture", GL_TEXTURE0);
            shader.SetInt("material.diffuse", GL_TEXTURE1);
            shader.SetInt("material.specular", GL_TEXTURE2);
            shader.SetInt("material.normalMap", GL_TEXTURE3);
            
        }

        public Submodel(string name, List<float> vertices, Vector3 offset, Model parent, Material material)
        {
            this.name = name;
            this.vertices = vertices;
            this.material = material;
            this.translation = offset;
            this.scaling = new(1, 1, 1);
            this.parent = parent;

            GenerateBuffers();

            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.diffuse", GL_TEXTURE0);
        }

        public Submodel(string name, List<float> vertices, Vector3 offset, Vector3 scaling, Model parent)
        {
            this.name = name;
            this.vertices = vertices;
            this.material = new();
            this.translation = offset;
            this.scaling = scaling;
            this.parent = parent;

            GenerateBuffers();

            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.diffuse", GL_TEXTURE0);
            material.Transparency = 1;
            material.Texture = 2;
        }

        public void Render()
        {
            if (!useRenderDistance || MathC.Distance(COREMain.GetCurrentScene.camera.position, translation + parent.translation) < renderDistance)
            {
                highlighted = COREMain.selectedID == ID;

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);
                
                shader.SetVector3("viewPos", COREMain.scenes[COREMain.selectedScene].camera.position);
                shader.SetFloat("transparency", material.Transparency);
                shader.SetBool("allowAlpha", COREMain.allowAlphaOverride);

                ClampValues();

                Matrix model = Matrix.IdentityMatrix * new Matrix(scaling * parent.Scaling, translation + parent.translation) * (MathC.GetRotationXMatrix(rotation.x + parent.rotation.x) * MathC.GetRotationYMatrix(rotation.y + parent.rotation.y) * MathC.GetRotationZMatrix(rotation.z + parent.rotation.z));

                shader.SetMatrix("model", model);

                usedTextures[material.Texture].Use(GL_TEXTURE0);
                usedTextures[material.DiffuseMap].Use(GL_TEXTURE1);
                usedTextures[material.SpecularMap].Use(GL_TEXTURE2);
                usedTextures[material.NormalMap].Use(GL_TEXTURE3);

                glBindVertexArray(VAO);
                unsafe
                {
                    GL.glLineWidth(1.5f);
                    if (renderLines)
                        glDrawArrays(PrimitiveType.Lines, 0, vertices.Count / 8);
                    else
                        glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count / 8);

                    if (COREMain.renderToIDFramebuffer)
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
                }
            }
        }

        private List<float> ConvertIndices(List<float> vertices, List<uint> indices)
        {
            List<float> result = new();

            foreach (uint Indice in indices)
            {
                uint indice = Indice * 8;
                result.Add(vertices[(int)indice]);
                result.Add(vertices[(int)indice + 1]);
                result.Add(vertices[(int)indice + 2]);
                result.Add(vertices[(int)indice + 3]);
                result.Add(vertices[(int)indice + 4]);
                result.Add(vertices[(int)indice + 5]);
                result.Add(vertices[(int)indice + 6]);
                result.Add(vertices[(int)indice + 7]);
            }

            return result;
        }

        private void GenerateBuffers()
        {
            GenerateFilledBuffer(out VBO, out VAO, vertices.ToArray());

            shader.Use();

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

        private void ClampValues()
        {
            if (scaling.x < 0.01f)
                scaling.x = 0.01f;
            if (scaling.y < 0.01f)
                scaling.y = 0.01f;
            if (scaling.z < 0.01f)
                scaling.z = 0.01f;
            if (rotation.x >= 360)
                rotation.x = 0;
            if (rotation.y >= 360)
                rotation.y = 0;
            if (rotation.z >= 360)
                rotation.z = 0;
        }

        public void Dispose()
        {
            glDeleteBuffer(VBO);
            glDeleteVertexArray(VAO);
            GC.Collect();
        }
    }
}