using CORERenderer.shaders;
using CORERenderer.Main;
using CORERenderer.textures;
using static CORERenderer.OpenGL.GL;
using COREMath;
using System.Transactions;

namespace CORERenderer.Loaders
{
    public class PBRSphere : Readers
    {
        public List<List<float>> vertices;
        public List<List<uint>> indices;

        public PBRMaterial material;

        public readonly Shader shader = new($"{CORERenderContent.pathRenderer}\\shaders\\PBRDebug.vert", $"{CORERenderContent.pathRenderer}\\shaders\\PBRLighting.frag");
        public readonly Shader normalRenderShader = new($"{CORERenderContent.pathRenderer}\\shaders\\normal.vert", $"{CORERenderContent.pathRenderer}\\shaders\\normal.frag", $"{CORERenderContent.pathRenderer}\\shaders\\normal.geom");

        public List<uint> GeneratedBuffers = new();
        public List<uint> GeneratedVAOs = new();
        public List<uint> elementBufferObject = new();

        public bool renderNormals = false;

        public PBRSphere(PBRSphereType type)
        {
            if (type == PBRSphereType.RustedIron)
            {
                material.albedoMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\rustediron2_basecolor.png");
                material.normalMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\rustediron2_normal.png");
                material.metallicMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\rustediron2_metallic.png");
                material.roughnessMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\rustediron2_roughness.png");
                material.AOMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\ao.png");
            }

            bool loaded = LoadOBJ($"{CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\PBRSphere.obj", out _, out vertices, out indices, out _);
            if (!loaded)
                throw new Exception($"Failed to load {CORERenderContent.pathRenderer}\\Loaders\\PBRSphereMaterials\\PBRSphere.obj");

            GenerateBuffers();

            shader.SetInt("albedoMap", GL_TEXTURE0);
            shader.SetInt("normalMap", GL_TEXTURE1);
            shader.SetInt("metallicMap", GL_TEXTURE2);
            shader.SetInt("roughnessMap", GL_TEXTURE3);
            shader.SetInt("aoMap", GL_TEXTURE4);
        }

        public unsafe void Render()
        {
            shader.Use();

            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glStencilMask(0xFF);

            shader.SetVector3("camPos", CORERenderContent.camera.position);

            for (int i = 0; i < CORERenderContent.lights.Count; i++)
            {
                shader.SetVector3($"basicLightInfo[{i}].lightPosition", CORERenderContent.lights[i].position);
                shader.SetVector3($"basicLightInfo[{i}].lightColor", CORERenderContent.lights[i].color);
            }

            shader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetScalingMatrix(0.3f, 0.3f, 0.3f));

            material.albedoMap.Use(GL_TEXTURE0);
            material.normalMap.Use(GL_TEXTURE1);
            material.metallicMap.Use(GL_TEXTURE2);
            material.roughnessMap.Use(GL_TEXTURE3);
            material.AOMap.Use(GL_TEXTURE4);

            glBindVertexArray(GeneratedVAOs[0]);
            glDrawElements(GL_TRIANGLES, indices[0].Count, GL_UNSIGNED_INT, (void*)0);

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            if (renderNormals)
            {
                normalRenderShader.Use();

                normalRenderShader.SetMatrix("model", Matrix.IdentityMatrix);

                glBindVertexArray(GeneratedVAOs[0]);
                glDrawElements(GL_TRIANGLES, indices[0].Count, GL_UNSIGNED_INT, (void*)0);
            }
        }

        private unsafe void GenerateBuffers()
        {
            GeneratedBuffers = new();
            GeneratedVAOs = new();
            elementBufferObject = new();

            for (int i = 0; i < vertices.Count; i++)
            {
                {
                    //gets current vertices and puts in a buffer
                    float[] local = vertices[i].ToArray();
                    uint buffer = glGenBuffer();
                    glBindBuffer(GL_ARRAY_BUFFER, buffer);

                    fixed (float* temp = &local[0])
                    {
                        IntPtr intptr = new(temp);
                        glBufferData(GL_ARRAY_BUFFER, local.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                    }
                    GeneratedBuffers.Add(buffer);
                }
                {
                    //could be put in a for loop but not that necessary
                    uint GeneratedVAO = glGenVertexArray();
                    glBindVertexArray(GeneratedVAO);
                    GeneratedVAOs.Add(GeneratedVAO);
                    //3D coordinates
                    int vertexLocation = shader.GetAttribLocation("aPos");
                    glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0);
                    glEnableVertexAttribArray((uint)vertexLocation);

                    //UV texture coordinates
                    vertexLocation = shader.GetAttribLocation("aTexCoords");
                    glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                    glEnableVertexAttribArray((uint)vertexLocation);

                    //normal coordinates
                    vertexLocation = shader.GetAttribLocation("aNormal");
                    glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float)));
                    glEnableVertexAttribArray((uint)vertexLocation);

                    //adds ebo to the vao
                    uint[] local2 = indices[i].ToArray();
                    uint local3 = glGenBuffer();
                    glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, local3);
                    fixed (uint* temp = &local2[0])
                    {
                        IntPtr intptr = new(temp);
                        glBufferData(GL_ELEMENT_ARRAY_BUFFER, local2.Length * sizeof(uint), intptr, GL_STATIC_DRAW);
                    }
                    elementBufferObject.Add(local3);
                }
                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);
            }
        }
    }
}
