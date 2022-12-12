using COREMath;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using CORERenderer.shaders;

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

        public bool highlighted = false;

        public string mtllib;

        public int ID;

        public Obj(string path)
        {
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out mtllib);
            GenerateBuffers();

            int error;
            if (!loaded)
                throw new Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
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
            
            /*int aa = 0;
            for (int i = 0; i < vertices.Count; i++)
                for (int j = 0; j < vertices[i].Count; j++)
                    aa++;
            int ab = 0;
            for (int i = 0; i < indices.Count; i++)
                for (int j = 0; j < indices[i].Count; j++)
                    ab++;
            Console.WriteLine($"\nvertices' size is: {aa * sizeof(float)} bytes, indices' size is: {ab * sizeof(int)} bytes");*/
        }

        public Obj() { } //this has to exist otherwise it results in an error???

        public Obj(string objPath, string mtlPath)
        {
            bool loaded = LoadOBJ(objPath, out List<string> mtlNames, out vertices, out indices, out mtllib);

            int error;
            if (!loaded)
                throw new Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
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

        public unsafe void Render(Camera camera) //better to make this extend to rendereveryframe() or new render override
        {
            shader.Use();

            shader.SetVector3("viewPos", camera.position);

            //spotLight
            shader.SetVector3("spotLight.position", camera.position);
            shader.SetVector3("spotLight.direction", camera.front);
            shader.SetVector3("spotLight.ambient", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.diffuse", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.specular", 0.0f, 0.0f, 0.0f);
            shader.SetFloat("spotLight.constant", 1.0f);
            shader.SetFloat("spotLight.linear", 0.09f);
            shader.SetFloat("spotLight.quadratic", 0.032f);
            shader.SetFloat("spotLight.cutOff", MathC.Cos(MathC.DegToRad(12.5f)));
            shader.SetFloat("spotLight.outerCutOff", MathC.Cos(MathC.DegToRad(15.0f)));

            shader.SetMatrix("view", camera.GetViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            if (Scaling < 0.01f)
                Scaling = 0.01f;
            if (rotationX >= 360)
                rotationX = 0;
            if (rotationY >= 360)
                rotationY = 0;
            if (rotationZ >= 360)
                rotationZ = 0;

            shader.SetMatrix("model", Matrix.IdentityMatrix
                      * new Matrix(Scaling, translation)
                      * (MathC.GetRotationXMatrix(rotationX)
                      * MathC.GetRotationYMatrix(rotationY)
                      * MathC.GetRotationZMatrix(rotationZ)));

            for (int i = 0; i < Materials.Count; i++)
            {

                glBindVertexArray(GeneratedVAOs[i]);

                //directional light
                shader.SetVector3("dirLight.direction", -0.2f, -1.0f, -0.3f);
                shader.SetVector3("dirLight.ambient", Materials[i].Ambient.x / 5, Materials[i].Ambient.y / 5, Materials[i].Ambient.z / 5);
                shader.SetVector3("dirLight.diffuse", Materials[i].Diffuse.x / 5, Materials[i].Diffuse.y / 5, Materials[i].Diffuse.z / 5);
                shader.SetVector3("dirLight.specular", Materials[i].Specular.x / 5, Materials[i].Specular.y / 5, Materials[i].Specular.z / 5);

                shader.SetVector3("pointLights[0].position", 10, 5, 10);

                shader.SetVector3("pointLights[1].position", -10, 5, -10);

                //point lights
                for (int j = 0; j < 2; j++)
                {
                    shader.SetVector3($"pointLights[{j}].position", 0, 10, 0);
                    shader.SetFloat($"pointLights[{j}].constant", 1.0f);
                    shader.SetFloat($"pointLights[{j}].linear", 0.022f);
                    shader.SetFloat($"pointLights[{j}].quadratic", 0.0019f);
                    shader.SetVector3($"pointLights[{j}].ambient", Materials[i].Ambient.x / 5, Materials[i].Ambient.y / 5, Materials[i].Ambient.z / 5);
                    shader.SetVector3($"pointLights[{j}].diffuse", Materials[i].Diffuse.x / 5, Materials[i].Diffuse.y / 5, Materials[i].Diffuse.z / 5);
                    shader.SetVector3($"pointLights[{j}].specular", Materials[i].Specular.x / 5, Materials[i].Specular.y / 5, Materials[i].Specular.z / 5);
                }

                usedTextures[Materials[i].Texture].Use(GL_TEXTURE0);
                usedTextures[Materials[i].SpecularMap].Use(GL_TEXTURE1);

                shader.SetFloat("material.shininess", Materials[i].Shininess);

                glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
            }
        }

        public unsafe void GenerateBuffers()
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
                    int vertexLocation2 = shader.GetAttribLocation("aTexCoords");
                    glVertexAttribPointer((uint)vertexLocation2, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                    glEnableVertexAttribArray((uint)vertexLocation2);

                    //normal coordinates
                    int vertexLocation3 = shader.GetAttribLocation("aNormal");
                    glVertexAttribPointer((uint)vertexLocation3, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float)));
                    glEnableVertexAttribArray((uint)vertexLocation3);

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

        public void ErrorLogic(int error)
        {
            switch (error)
            {
                case -1:
                    throw new Exception($"Invalid file format for {name}, should end with .mtl, not {mtllib[mtllib.IndexOf('.')..]} (error == -1)");
                case 0:
                    Console.WriteLine($"No material library found for {name} (error == 0)");
                    break;
                case 1:
                    break;
                default:
                    throw new Exception($"Undefined error: {error}");
            }
        }
    }  
}
