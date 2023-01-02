using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using CORERenderer.GLFW;
using CORERenderer.shaders;
using CORERenderer.OpenGL;

namespace CORERenderer.Loaders
{
    public class Obj : Readers
    {
        public List<List<float>> vertices;
        public List<List<uint>> indices;

        public List<Material> Materials;

        /*
         * $"{CORERenderContent.pathRenderer}\\shaders\\lighting.frag" for normal lighting
         * $"{CORERenderContent.pathRenderer}\\shaders\\depthVisualizer.frag" for showing the depth
         * $"{CORERenderContent.pathRenderer}\\shaders\\outlining.frag" for outlining the current object
         */
        public readonly Shader shader = new($"{CORERenderContent.pathRenderer}\\shaders\\shader.vert", $"{CORERenderContent.pathRenderer}\\shaders\\lighting.frag");
        public readonly Shader highlightShader = new($"{CORERenderContent.pathRenderer}\\shaders\\shader.vert", $"{CORERenderContent.pathRenderer}\\shaders\\outlining.frag");
        public readonly Shader normalRenderShader = new($"{CORERenderContent.pathRenderer}\\shaders\\normal.vert", $"{CORERenderContent.pathRenderer}\\shaders\\normal.frag", $"{CORERenderContent.pathRenderer}\\shaders\\normal.geom");

        public readonly string name = "PLACEHOLDER";

        //not safe to make them public but is needed to delete them
        public List<uint> GeneratedBuffers = new();
        public List<uint> GeneratedVAOs = new();
        public List<uint> elementBufferObject = new();

        public float Scaling = 1.0f;
        public Vector3 translation = Vector3.Zero;
        public float rotationX = 0.0f;
        public float rotationY = 0.0f;
        public float rotationZ = 0.0f;

        public bool renderNormals = false;

        public bool highlighted = false;

        public bool renderLines = false;

        public string mtllib;

        public int ID;

        private Shader debugShader = new($"{CORERenderContent.pathRenderer}\\shaders\\PBRDebug.vert", $"{CORERenderContent.pathRenderer}\\shaders\\PBRLighting.frag");

        public Obj(string path)
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
                    loaded = LoadMTL
                    (
                        $"{path[..(temp[^1] + 1)]}{mtllib}", mtlNames, out Materials, out error
                    );
                else
                    loaded = LoadMTL
                    (
                        $"{CORERenderContent.pathRenderer}\\Loaders\\default.mtl", mtlNames, out Materials, out error
                    );
            }
            else
                loaded = LoadMTL
                    (
                        null, mtlNames, out Materials, out error
                    );
            if (!loaded)
                ErrorLogic(error);
        }

        public Obj() { } //this has to exist otherwise it results in an error???

        public Obj(string objPath, string mtlPath)
        {
            bool loaded = LoadOBJ(objPath, out List<string> mtlNames, out vertices, out indices, out mtllib);

            int error;
            if (!loaded)
                throw new GLFW.Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
            if (objPath != null || !File.Exists(objPath))
                loaded = LoadMTL(mtlPath, mtlNames, out Materials, out error);
            else
                loaded = LoadMTL
                    (
                        null, mtlNames, out Materials, out error
                    );
            if (!loaded)
                ErrorLogic(error);

            GenerateBuffers();

            shader.SetInt("material.diffuse", GL_TEXTURE0);
            shader.SetInt("material.specular", GL_TEXTURE1);
        }

        public unsafe void Render() //better to make this extend to rendereveryframe() or new render override
        {
            shader.Use();

            if (renderLines)
            {
                glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
                glDisable(GL_CULL_FACE);
            }

            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glStencilMask(0xFF);

            shader.SetVector3("viewPos", CORERenderContent.camera.position);

            //spotLight
            shader.SetVector3("spotLight.position", CORERenderContent.camera.position);
            shader.SetVector3("spotLight.direction", CORERenderContent.camera.front);
            shader.SetVector3("spotLight.ambient", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.diffuse", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.specular", 0.0f, 0.0f, 0.0f);
            shader.SetFloat("spotLight.constant", 1.0f);
            shader.SetFloat("spotLight.linear", 0.09f);
            shader.SetFloat("spotLight.quadratic", 0.032f);
            shader.SetFloat("spotLight.cutOff", MathC.Cos(MathC.DegToRad(12.5f)));
            shader.SetFloat("spotLight.outerCutOff", MathC.Cos(MathC.DegToRad(15.0f)));

            ClampValues();

            shader.SetMatrix("model", Matrix.IdentityMatrix
                      * new Matrix(Scaling, translation)
                      * (MathC.GetRotationXMatrix(rotationX)
                      * MathC.GetRotationYMatrix(rotationY)
                      * MathC.GetRotationZMatrix(rotationZ)));

            for (int i = 0; i < Materials.Count; i++)
            {

                glBindVertexArray(GeneratedVAOs[i]);

                //directional light
                shader.SetVector3("dirLight.direction", 0.2f, 1.0f, 0.3f);
                shader.SetVector3("dirLight.ambient", Materials[i].Ambient.x, Materials[i].Ambient.y, Materials[i].Ambient.z);
                shader.SetVector3("dirLight.diffuse", Materials[i].Diffuse.x, Materials[i].Diffuse.y, Materials[i].Diffuse.z);
                shader.SetVector3("dirLight.specular", Materials[i].Specular.x, Materials[i].Specular.y, Materials[i].Specular.z);

                //point lights
                for (int j = 0; j < CORERenderContent.lights.Count; j++)
                {
                    shader.SetVector3($"pointLights[{j}].position", CORERenderContent.lights[j].position.x, CORERenderContent.lights[j].position.y, CORERenderContent.lights[j].position.z);
                    shader.SetFloat($"pointLights[{j}].constant", 1.0f);
                    shader.SetFloat($"pointLights[{j}].linear", 0.09f);
                    shader.SetFloat($"pointLights[{j}].quadratic", 0.032f);
                    shader.SetVector3($"pointLights[{j}].ambient", Materials[i].Ambient.x, Materials[i].Ambient.y, Materials[i].Ambient.z);
                    shader.SetVector3($"pointLights[{j}].diffuse", Materials[i].Diffuse.x, Materials[i].Diffuse.y, Materials[i].Diffuse.z);
                    shader.SetVector3($"pointLights[{j}].specular", Materials[i].Specular.x, Materials[i].Specular.y, Materials[i].Specular.z);
                }

                usedTextures[Materials[i].Texture].Use(GL_TEXTURE0);
                usedTextures[Materials[i].SpecularMap].Use(GL_TEXTURE1);

                shader.SetFloat("material.shininess", Materials[i].Shininess);

                glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
            }

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            if (renderNormals)
            {
                normalRenderShader.Use();

                normalRenderShader.SetMatrix("model", Matrix.IdentityMatrix
                * new Matrix(Scaling, translation)
                * (MathC.GetRotationXMatrix(rotationX)
                * MathC.GetRotationYMatrix(rotationY)
                * MathC.GetRotationZMatrix(rotationZ)));

                for (int i = 0; i < Materials.Count; i++)
                {
                    glBindVertexArray(GeneratedVAOs[i]);
                    glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
                }
            }
        }

        public unsafe void RenderOutlines()
        {
            if (highlighted)
            {
                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);

                glStencilFunc(GL_NOTEQUAL, 1, 0xFF);
                glStencilMask(0x00);
                glDisable(GL_DEPTH_TEST);

                highlightShader.Use();

                highlightShader.SetMatrix("model", Matrix.IdentityMatrix
                      * new Matrix(Scaling * 1.02f, translation)
                      * (MathC.GetRotationXMatrix(rotationX)
                      * MathC.GetRotationYMatrix(rotationY)
                      * MathC.GetRotationZMatrix(rotationZ)));

                for (int i = 0; i < Materials.Count; i++)
                {
                    glBindVertexArray(GeneratedVAOs[i]);
                    glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
                }
                glStencilMask(0xFF);
                glStencilFunc(GL_ALWAYS, 0, 0xFF);
                glEnable(GL_DEPTH_TEST);
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

        private void ClampValues()
        {
            if (Scaling < 0.01f)
                Scaling = 0.01f;
            if (rotationX >= 360)
                rotationX = 0;
            if (rotationY >= 360)
                rotationY = 0;
            if (rotationZ >= 360)
                rotationZ = 0;
        }

        public unsafe void PBRDebugRender()
        {
            debugShader.Use();

            if (renderLines)
            {
                glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
                glDisable(GL_CULL_FACE);
            }

            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glStencilMask(0xFF);

            debugShader.SetVector3("camPos", CORERenderContent.camera.position);
            debugShader.SetFloat("metallic", 0.5f);
            debugShader.SetFloat("roughness", 0.7f);
            debugShader.SetVector3("albedo", 0.5f, 0, 0);
            debugShader.SetFloat("AO", 1);

            ClampValues();

            debugShader.SetVector3("basicLightInfo[0].lightPosition", CORERenderContent.lights[0].position);
            debugShader.SetVector3("basicLightInfo[0].lightColor", CORERenderContent.lights[0].color);

            shader.SetMatrix("model", Matrix.IdentityMatrix
                      * new Matrix(Scaling, translation)
                      * (MathC.GetRotationXMatrix(rotationX)
                      * MathC.GetRotationYMatrix(rotationY)
                      * MathC.GetRotationZMatrix(rotationZ)));

            for (int i = 0; i < Materials.Count; i++)
            {
                glBindVertexArray(GeneratedVAOs[i]);
                glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
            }

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            if (renderNormals)
            {
                normalRenderShader.Use();

                normalRenderShader.SetMatrix("model", Matrix.IdentityMatrix
                * new Matrix(Scaling, translation)
                * (MathC.GetRotationXMatrix(rotationX)
                * MathC.GetRotationYMatrix(rotationY)
                * MathC.GetRotationZMatrix(rotationZ)));

                for (int i = 0; i < Materials.Count; i++)
                {
                    glBindVertexArray(GeneratedVAOs[i]);
                    glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
                }
            }
        }
    }  
}
