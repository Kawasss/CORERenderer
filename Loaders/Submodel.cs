﻿using COREMath;
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
        private List<uint> indices;

        public int numberOfVertices = 0;

        private Material material;

        private Shader shader = GenericShaders.GenericLighting;
        private Shader IDShader = GenericShaders.IDPicking;

        private uint VBO, VAO, EBO;

        public int ID = COREMain.NewAvaibleID;
        private Vector3 IDColor;

        private string name;
        public string Name { get { return name; } }

        public bool renderLines = false;

        public bool highlighted = false;

        public bool isTranslucent = false;

        public bool useGlDrawElements = true;

        public bool hasMaterials = true;

        public Submodel(string name, List<float> vertices, List<uint> indices, Material material)
        {
            this.name = name;
            this.vertices = vertices;
            this.indices = indices;
            this.material = material;

            isTranslucent = material.Transparency != 1;

            numberOfVertices = vertices.Count / 8;

            GenerateBuffers();

            IDColor = COREMain.GenerateIDColor(ID);

            shader.SetInt("material.Texture", GL_TEXTURE0);
            shader.SetInt("material.diffuse", GL_TEXTURE1);
            shader.SetInt("material.specular", GL_TEXTURE2);
            shader.SetInt("material.normalMap", GL_TEXTURE3);
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
                    if (renderLines)
                        glDrawElements(PrimitiveType.Lines, indices.Count, GLType.UnsingedInt, (void*)0);
                    else
                        glDrawElements(PrimitiveType.Triangles, indices.Count, GLType.UnsingedInt, (void*)0);

                    if (COREMain.renderToIDFramebuffer)
                    {
                        COREMain.IDFramebuffer.Bind();

                        IDShader.Use();

                        IDShader.SetVector3("color", IDColor);
                        IDShader.SetMatrix("model", model);

                        if (useGlDrawElements)
                            glDrawElements(PrimitiveType.Triangles, indices.Count, GLType.UnsingedInt, (void*)0);
                        else
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

            GenerateFilledBuffer(out EBO, indices.ToArray());

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
            glDeleteBuffer(EBO);
            GC.Collect();
        }
    }
}