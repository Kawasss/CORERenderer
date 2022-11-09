using COREMath;
using System;
using CORERenderer.textures;
using System.CodeDom.Compiler;
using static CORERenderer.GL;
using CORERenderer.shaders;
using System.Runtime.CompilerServices;
using CORERenderer.GLFW.Structs;

namespace CORERenderer.Loaders
{
    public class Obj : Readers
    {
        public readonly List<List<float>> vertices;
        public readonly List<List<uint>> indices;

        public readonly List<Material> Materials;

        private readonly Shader shader = new($"{CORERenderContent.pathRenderer}\\shaders\\shader.vert", $"{CORERenderContent.pathRenderer}\\shaders\\lighting.frag");

        public readonly string name = null;

        private List<uint> GeneratedBuffers;
        private List<uint> GeneratedVAOs;
        private List<uint> elementBufferObject;

        public float Scaling = 1.0f;
        public Vector3 translation = Vector3.Zero;
        public float rotationX = 0.0f;
        public float rotationY = 0.0f;
        public float rotationZ = 0.0f;

        public bool highlighted = false;

        public Obj(string path)
        {
            bool loaded = LoadOBJ(path, out List<string> mtlNames, out vertices, out indices, out string mtllib);
            _ = LoadOBJ(null, out _, out _, out _, out _);
            float Scaling = 1.0f;
            Vector3 translation = Vector3.Zero;
            float rotationX = 0.0f;
            float rotationY = 0.0f;
            float rotationZ = 0.0f;

            int error = 0;
            if (!loaded)
                throw new Exception($"Invalid file format for {name} (!.obj && !.OBJ)");
            if (path != null)
            {
                List<int> temp = new();

                for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                    temp.Add(i);

                name = path[(temp[^1] + 1)..];

                loaded = LoadMTL
                (
                    $"{path[..(temp[^1] + 1)]}{mtllib}", mtlNames, out Materials, out error
                );
            }
            else
                loaded = LoadMTL
                    (
                        null, mtlNames, out Materials, out error
                    );
            if (!loaded)
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
            if (Materials.Count > 0)
            {
                Materials[0].Texture.Use(GL_TEXTURE0);
                Materials[0].SpecularMap.Use(GL_TEXTURE1);
            }
            int aa = 0;
            for (int i = 0; i < vertices.Count; i++)
                for (int j = 0; j < vertices[i].Count; j++)
                    aa++;
            int ab = 0;
            for (int i = 0; i < indices.Count; i++)
                for (int j = 0; j < indices[i].Count; j++)
                    ab++;
            //Console.WriteLine($"\nvertices' size is: {aa * sizeof(float)} bytes, indices' size is: {ab * sizeof(int)} bytes");

            GenerateBuffers();
        }

        public unsafe void Render(Camera camera) //better to make this extend to rendereveryframe() or new render override
        {
            shader.Use();
            
            shader.SetVector3("viewPos", camera.position);

            //all till the last for loop is temporary
            //directional light
            shader.SetVector3("dirLight.direction", -0.2f, -1.0f, -0.3f);
            shader.SetVector3("dirLight.ambient", 0.1f, 0.1f, 0.1f);
            shader.SetVector3("dirLight.diffuse", 0.5f, 0.5f, 0.5f);
            shader.SetVector3("dirLight.specular", 1.0f, 1.0f, 1.0f);

            shader.SetVector3("pointLights[0].position", 10, 5, 10);

            shader.SetVector3("pointLights[1].position", -10, 5, -10);

            //point lights
            for (int j = 0; j < 2; j++) 
            {
                shader.SetVector3($"pointLights[{j}].position", 0, 10, 0);
                shader.SetFloat($"pointLights[{j}].constant", 1.0f);
                shader.SetFloat($"pointLights[{j}].linear", 0.022f);
                shader.SetFloat($"pointLights[{j}].quadratic", 0.0019f);
                shader.SetVector3($"pointLights[{j}].ambient", 0.2f, 0.2f, 0.2f);
                shader.SetVector3($"pointLights[{j}].diffuse", 0.5f, 0.5f, 0.5f);
                shader.SetVector3($"pointLights[{j}].specular", 1.0f, 1.0f, 1.0f);
            }

            //spotLight
            shader.SetVector3("spotLight.position", camera.position);
            shader.SetVector3("spotLight.direction", camera.front);
            shader.SetVector3("spotLight.ambient", 0.2f, 0.2f, 0.2f);
            shader.SetVector3("spotLight.diffuse", 0.5f, 0.5f, 0.5f);
            shader.SetVector3("spotLight.specular", 1.0f, 1.0f, 1.0f);
            shader.SetFloat("spotLight.constant", 1.0f);
            shader.SetFloat("spotLight.linear", 0.09f);
            shader.SetFloat("spotLight.quadratic", 0.032f);
            shader.SetFloat("spotLight.cutOff", MathC.Cos(MathC.DegToRad(12.5f)));
            shader.SetFloat("spotLight.outerCutOff", MathC.Cos(MathC.DegToRad(15.0f)));

            shader.SetMatrix("view", camera.GetViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            shader.SetBool("highlighted", highlighted);

            for (int i = 0; i < Materials.Count; i++)
            {
                Materials[i].Texture.Use(GL_TEXTURE0);
                Materials[i].SpecularMap.Use(GL_TEXTURE1);

                glBindBuffer(GL_ARRAY_BUFFER, GeneratedBuffers[i]);

                shader.SetFloat("material.shininess", Materials[i].Shininess);
                shader.SetInt("material.diffuse", GL_TEXTURE0);
                shader.SetInt("material.specular", GL_TEXTURE1);

                if (Scaling < 0.01f)
                    Scaling = 0.01f;
                if (rotationX >= 360)
                    rotationX = 0;
                if (rotationY >= 360)
                    rotationY = 0;
                if (rotationZ >= 360)
                    rotationZ = 0;
                shader.SetMatrix("model", Matrix.IdentityMatrix
                      .MultiplyWith(new Matrix(Scaling, translation))
                      .MultiplyWith(MathC.GetRotationXMatrix(rotationX))
                      .MultiplyWith(MathC.GetRotationYMatrix(rotationY))
                      .MultiplyWith(MathC.GetRotationZMatrix(rotationZ)));

                glBindVertexArray(GeneratedVAOs[i]);
                glDrawElements(GL_TRIANGLES, indices[i].Count, GL_UNSIGNED_INT, (void*)0);
            } 
        }

        private unsafe void GenerateBuffers()
        {
            GeneratedBuffers = new();
            GeneratedVAOs = new();
            elementBufferObject = new();

            for (int i = 0; i < vertices.Count; i++) //is currently one because theres only one instance of vertices, might change in the future 
            {
                float[] local = vertices[i].ToArray();
                uint buffer = glGenBuffer();
                glBindBuffer(GL_ARRAY_BUFFER, buffer);
                fixed (float* temp = &local[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, local.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                }
                GeneratedBuffers.Add(buffer);

                uint GeneratedVAO = glGenVertexArray();
                glBindVertexArray(GeneratedVAO);
                //could be put in a for loop but not that necessary
                int vertexLocation = shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                int vertexLocation2 = shader.GetAttribLocation("aTexCoords");
                glVertexAttribPointer((uint)vertexLocation2, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation2);

                int vertexLocation3 = shader.GetAttribLocation("aNormal");
                glVertexAttribPointer((uint)vertexLocation3, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation3);

                uint[] local2 = indices[i].ToArray();
                uint local3 = glGenBuffer();
                glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, local3);
                fixed (uint* temp = &local2[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ELEMENT_ARRAY_BUFFER, local2.Length * sizeof(uint), intptr, GL_STATIC_DRAW);
                }
                elementBufferObject.Add(local3);
                GeneratedVAOs.Add(GeneratedVAO);
            }
        }
    }  
}
