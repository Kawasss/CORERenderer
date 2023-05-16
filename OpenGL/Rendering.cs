using COREMath;
using CORERenderer.textures;
using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.GLFW;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CORERenderer.GUI;
using System.Collections.Concurrent;
using Assimp;
using CORERenderer.Fonts;

namespace CORERenderer.OpenGL
{
    public partial class Rendering : GL
    {
        private static uint vertexArrayObjectGrid;

        public static bool cullFaces = true;
        public static bool renderOrthographic = false;

        public static ShaderType shaderConfig = ShaderType.Lighting;

        private static Camera camera = null;

        private static string[] renderStatistics = new string[9] { "Ticks spent rendering opaque models: 0", "Ticks spent rendering translucent models: 0", "Ticks spent depth sorting: 0", "Ticks spent overall: 0", "Models rendered: 0", "Submodels rendered: 0, of which:", "   0 are translucent", "   0 are opaque", "Draw calls this frame: 0" };
        public static string[] RenderStatistics { get { return renderStatistics; } }

        private static long ticksSpent3DRenderingThisFrame = 0;
        public static long TicksSpent3DRenderingThisFrame { get { return ticksSpent3DRenderingThisFrame; } }

        private static Framebuffer reflectionFramebuffer;
        private static Cubemap reflectionCubemap;

        /// <summary>
        /// Gets the rendering quality and sets the rendering quality, whilst simultaniously changing anything that depends on the rendering quality
        /// </summary>
        public static float TextureQuality 
        { get => textureQuality; set
            { 
                textureQuality = value;
            } 
        }
        public static float ReflectionQuality
        {
            get => reflectionQuality; set
            {//both width because afaik its better to have a perfect cube and not a stretched one
                reflectionCubemap = GenerateEmptyCubemap((int)(renderingWidth / reflectionQuality), (int)(renderingWidth / reflectionQuality));
                reflectionFramebuffer = GenerateFramebuffer((int)(renderingWidth / reflectionQuality), (int)(renderingWidth / reflectionQuality));
            }
        }
        private static float reflectionQuality = OpenGL.TextureQuality.Default;
        private static float textureQuality = OpenGL.ReflectionQuality.Default;

        /// <summary>
        /// Gets the color used with glClearColor (default is 0.3f, 0.3f, 0.3f, 1), sets the same color
        /// </summary>
        public static Vector4 ClearColor { get => clearColor; set => clearColor = value; }
        private static Vector4 clearColor = new(0.3f, 0.3f, 0.3f, 1);

        public static int[] ViewportDimensions { get => GetViewportDimensions(); }

        private static int renderingWidth, renderingHeight;

        public static void Init(int RenderingWidth, int RenderingHeight)
        {
            vertexArrayObjectGrid = GenerateBufferlessVAO();
            glBindVertexArray(0);
            GenericShaders.SetShaders();
            renderingWidth = RenderingWidth;
            renderingHeight = RenderingHeight;
            TextureQuality = OpenGL.TextureQuality.Default;
            ReflectionQuality = OpenGL.ReflectionQuality.Medium;
            reflectionFramebuffer.Bind();
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glEnable(GL_DEPTH_TEST);
            glDepthFunc(GL_LEQUAL);

            glEnable(GL_STENCIL_TEST);
            glStencilFunc(GL_NOTEQUAL, 1, 0xFF);
            glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);

            glEnable(GL_DEBUG_OUTPUT);

            glEnable(GL_CULL_FACE);
            //glEnable(GL_TEXTURE_CUBE_MAP_SEAMLESS);
            glCullFace(GL_BACK);
            glFrontFace(GL_CCW);

            SetClearColor(clearColor);
        }

        public static void SetCamera(Camera currentCamera)
        {
            camera = currentCamera;
        }

        private unsafe static void RenderCubemapReflections(List<Model> models, List<Main.Light> lights)
        {
            Matrix originalView = camera.ViewMatrix;
            Vector3 previousCamFront = camera.front;
            Vector2 originalYawPitch = new(camera.Yaw, camera.Pitch);
            Vector3 previousUp = camera.up;

            int[] originalViewportDimensions = GetViewportDimensions();
            int previousFB = GetCurrentFramebufferID();
            float previousCamAspectRatio = camera.AspectRatio, previousFov = camera.Fov;

            Vector3[] fronts = new Vector3[]
            {
                new Vector3( 1,  0,  0),
                new Vector3(-1,  0,  0),
                new Vector3( 0,  1,  0),
                new Vector3( 0, -1,  0),
                new Vector3( 0,  0,  1),
                new Vector3( 0,  0,  -1)
            };
            Vector3[] ups = new Vector3[]
            {
                new(0, -1,  0),
                new(0, -1,  0),
                new(0,  0,  1),
                new(0,  0, -1),
                new(0, -1,  0),
                new(0, -1,  0)
            };

            glViewport(0, 0, (int)((float)renderingWidth / (float)reflectionQuality), (int)((float)renderingWidth / (float)reflectionQuality));
            camera.AspectRatio = 1;

            reflectionFramebuffer.Bind();
            reflectionCubemap.Use(GL_TEXTURE6);
            glDepthFunc(GL_LEQUAL);
            Submodel.renderAllIDs = false;
            for (int i = 0; i < 6; i++)
            {
                camera.front = fronts[i].Normalized;
                camera.up = ups[i].Normalized;
                camera.right = MathC.Normalize(MathC.GetCrossProduct(fronts[i], Vector3.UnitVectorY));
                
                //camera.up = MathC.Normalize(MathC.GetCrossProduct(camera.right, fronts[i]));
                UpdateUniformBuffers();

                reflectionCubemap.Use(GL_TEXTURE6);
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, reflectionCubemap.textureID, 0);
                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                //RenderLights(lights);
                //RenderAllModels(models);
                foreach (Model model in models)
                    model.Render();
                RenderGrid();
                
                translucentSubmodels.Clear();
            }
            Submodel.renderAllIDs = true;

            glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            glBindFramebuffer((uint)previousFB);
            glViewport(originalViewportDimensions[0], originalViewportDimensions[1], originalViewportDimensions[2], originalViewportDimensions[3]);
            camera.Yaw = originalYawPitch.x;
            camera.Pitch = originalYawPitch.y;
            camera.AspectRatio = previousCamAspectRatio;
            camera.front = previousCamFront;
            camera.up = previousUp;
            camera.Fov = previousFov;
            UpdateUniformBuffers();
        }

        private static Model backgroundModel = null;

        public static List<Submodel> translucentSubmodels = new();

        public static void RenderScene(Scene scene) //experimental but can work
        {
            RenderCubemapReflections(scene.models, scene.lights);
            //RenderLights(scene.lights);
            RenderAllModels(scene.models);
            //reflectionCubemap.Render();
        }

        public static void RenderAllModels(List<Model> models)
        {
            if (models.Count == 0)
                return;

            int modelsFrustumCulled = 0;
            int currentDrawCalls = drawCalls;
            Stopwatch sw = new();

            GenericShaders.GenericLighting.SetVector3("viewPos", camera.position);

            if (cullFaces)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            GenericShaders.GenericLighting.Use();

            sw.Start();

            reflectionCubemap.Use(GL_TEXTURE4);
            for (int i = 0; i < models.Count; i++)
            {
                if (models[i] == null)
                    continue;

                if (models[i].CanBeCulled)
                {
                    modelsFrustumCulled++;
                    continue;
                }

                //if (models[i].type != RenderMode.HDRFile)
                    models[i].Render();
                //else
                //    backgroundModel = models[i];
            }
            sw.Stop();
            long timeSpentRenderingOpaque = sw.ElapsedTicks;

            #region depth sorting
            sw = new();
            sw.Start();
            translucentSubmodels = DepthSortSubmodels(translucentSubmodels);
            sw.Stop();
            long timeSpentDepthSorting = sw.ElapsedTicks;
            #endregion

            sw = new();
            sw.Start();
            foreach (Submodel model in translucentSubmodels)
                model.Render();
            sw.Stop();
            long timeSpentRenderingTranslucent = sw.ElapsedTicks;

            //backgroundModel?.Render();
            //backgroundModel = null;

            ticksSpent3DRenderingThisFrame = timeSpentRenderingOpaque + timeSpentRenderingTranslucent + timeSpentDepthSorting;

            renderStatistics[0] = $"Ticks spent rendering opaque models: {timeSpentRenderingOpaque}";
            renderStatistics[1] = $"Ticks spent rendering translucent models: {timeSpentRenderingTranslucent}";
            renderStatistics[2] = $"Ticks spent depth sorting: {timeSpentDepthSorting}";
            renderStatistics[3] = $"Ticks spent overall: {ticksSpent3DRenderingThisFrame}";
            renderStatistics[4] = $"Total models: {models.Count}, of which {modelsFrustumCulled} are frustum culled";
            renderStatistics[5] = $"Submodels rendered: ~{drawCalls - currentDrawCalls}, of which:";
            renderStatistics[6] = $"   {translucentSubmodels.Count} are translucent";
            renderStatistics[7] = $"  ~{drawCalls - currentDrawCalls - translucentSubmodels.Count} are opaque";
            renderStatistics[8] = $"Draw calls this frame: {drawCalls - currentDrawCalls}, cull faces: {cullFaces}";


            translucentSubmodels = new();
        }

        private static List<Submodel> DepthSortSubmodels(List<Submodel> submodels)
        {
            List<Submodel> returnValue = new();
            List<float> distances = new();
            Dictionary<float, Submodel> distanceModelTable = new();
            foreach (Submodel model in submodels)
            {
                float distance = MathC.Distance(camera.position, model.parent.Transform.translation);
                while (distanceModelTable.ContainsKey(distance))
                    distance += 0.01f;
                distances.Add(distance);
                distanceModelTable.Add(distance, model);
            }

            float[] distancesArray = distances.ToArray();
            Array.Sort(distancesArray);
            Array.Reverse(distancesArray);
            for (int i = 0; i < distancesArray.Length; i++)
                returnValue.Add(distanceModelTable[distancesArray[i]]);

            return returnValue;
        }

        private static unsafe int[] GetViewportDimensions()
        {
            int[] vpd = new int[4];
            fixed (int* pointer = &vpd[0])
            {
                glGetIntegerv(GL_VIEWPORT, pointer);
            }
            return vpd;
        }

        public static unsafe int GetCurrentFramebufferID()
        {
            int value;
            glGetIntegerv(GL_FRAMEBUFFER_BINDING, &value);
            return value;
        }

        public static float[] GenerateQuadVerticesWithUV(int x, int y, int width, int height)
        {
            return new float[]
            {
                x, y + height,         0, 1,
                x, y,                  0, 0,
                x + width, y, 1, 0,

                x, y + height,         0, 1,
                x + width, y, 1, 0,
                x + width, y + height, 1, 1
            };
        }

        public static uint GenerateBufferlessVAO()
        {
            //allows the object to render bufferless
            uint VAO = glGenVertexArray();
            glBindVertexArray(VAO);
            return VAO;
        }

        /// <summary>
        /// Generates an empty buffer for buffersubdata, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO">VBO</param>
        /// <param name="VAO">VAO</param>
        /// <param name="size">size of the buffer in bytes</param>
        public static void GenerateEmptyBuffer(out uint VBO, out uint VAO, int sizeInBytes)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            glBufferData(GL_ARRAY_BUFFER, sizeInBytes, (IntPtr)null, GL_DYNAMIC_DRAW);

            TotalAmountOfTransferredBytes = sizeInBytes;
        }

        /// <summary>
        /// Generates a buffer with the given vertices, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO"></param>
        /// <param name="VAO"></param>
        /// <param name="vertices"></param>
        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, float[] vertices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);
            
            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), vertices, GL_STATIC_DRAW);

            TotalAmountOfTransferredBytes = vertices.Length * sizeof(float);
        }


        /// <summary>
        /// Generates a buffer with the given vertices, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO"></param>
        /// <param name="VAO"></param>
        /// <param name="vertices"></param>
        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, Vertex[] vertices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            float[] nVertices = Vertex.GetFloatList(vertices.ToList()).ToArray();

            glBufferData(GL_ARRAY_BUFFER, nVertices.Length * sizeof(float), nVertices, GL_STATIC_DRAW);

            TotalAmountOfTransferredBytes = vertices.Length * sizeof(float);
        }
        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, Matrix[] matrices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            unsafe
            {
                fixed (float* temp = &matrices[0].matrix4x4[0, 0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, matrices.Length * GL_MAT4_FLOAT_SIZE, temp, GL_STATIC_DRAW);
                }
            }

            TotalAmountOfTransferredBytes += 16 * matrices.Length * sizeof(float);
        }

        public static void GenerateFilledBuffer(out uint VBO, Matrix[] matrices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            unsafe
            {
                fixed (float* temp = &matrices[0].matrix4x4[0, 0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, matrices.Length * GL_MAT4_FLOAT_SIZE, temp, GL_STATIC_DRAW);
                }
            }

            TotalAmountOfTransferredBytes = 16 * matrices.Length * sizeof(float);
        }

        /// <summary>
        /// Generates a buffer with the given vertices, does not active the vertex attribute arrays however, this expects the VAO to already be bound.
        /// </summary>
        /// <param name="EBO"></param>
        /// <param name="indices"></param>
        public static void GenerateFilledBuffer(out uint EBO, uint[] indices)
        {
            EBO = glGenBuffer();
            glBindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            glBufferData(GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), indices, GL_STATIC_DRAW);

            TotalAmountOfTransferredBytes = indices.Length * sizeof(uint);
        }

        //try referring to offcenter version with 0, width, height, 0.01, 100
        public static Matrix GetOrthograpicProjectionMatrix(int width, int height) => Matrix.Createorthographic(width, height, -1000f, 1000f);//Matrix.Createorthographic(COREMain.Width, COREMain.Height, 0.01f, 1000f);
        
        public static void RenderLights(List<CORERenderer.Main.Light> locations)
        {
            GenericShaders.Light.Use();

            glBindVertexArray(Main.COREMain.vertexArrayObjectLightSource);

            for (int i = 0; i < locations.Count; i++)
            {
                GenericShaders.Light.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(locations[i].position) * MathC.GetScalingMatrix(0.2f));
                glDrawArrays(PrimitiveType.Triangles, 0, 36);
            }
        }

        public static void RenderGrid()
        {
            GenericShaders.Grid.Use();

            GenericShaders.Grid.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetScalingMatrix(1000));

            GenericShaders.Grid.SetVector3("playerPos", camera.position);

            glDisable(GL_CULL_FACE);
            glBindVertexArray(vertexArrayObjectGrid);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            glEnable(GL_CULL_FACE);
        }

        private static uint uboMatrices;

        public static void UpdateUniformBuffers()//can be made private if render loop is put here
        {
            //assigns values to the freed up gpu memory for global uniforms
            glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            MatrixToUniformBuffer(camera.ViewMatrix, GL_MAT4_FLOAT_SIZE);
            MatrixToUniformBuffer(camera.TranslationlessViewMatrix, GL_MAT4_FLOAT_SIZE * 2);
            if (!renderOrthographic)
                MatrixToUniformBuffer(camera.ProjectionMatrix, 0);
            else
                MatrixToUniformBuffer(camera.OrthographicProjectionMatrix, 0);
        }

        public unsafe static void SetUniformBuffers()//can be made private if render loop is put here
        {
            uboMatrices = glGenBuffer();

            glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            glBufferData(GL_UNIFORM_BUFFER, 3 * GL_MAT4_FLOAT_SIZE, NULL, GL_STATIC_DRAW);
            glBindBuffer(BufferTarget.UniformBuffer, 0);
            glBindBufferRange(GL_UNIFORM_BUFFER, 0, uboMatrices, 0, 3 * GL_MAT4_FLOAT_SIZE);

            //assigns values to the freed up gpu memory for global uniforms
            glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            if (!renderOrthographic)
                MatrixToUniformBuffer(camera.ProjectionMatrix, 0);
            else
                MatrixToUniformBuffer(camera.OrthographicProjectionMatrix, 0);
            glBindBuffer(BufferTarget.UniformBuffer, 0);
        }

        static uint cubeVAO;
        static uint cubeVBO;

        public unsafe static void RenderCube()
        {
            // initialize (if necessary)
            if (cubeVAO == 0)
            {
                float[] vertices = {
                        // back face
                        -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                        1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 1.0f, // top-right
                        1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 0.0f, // bottom-right         
                        1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 1.0f, 1.0f, // top-right
                        -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 0.0f, // bottom-left
                        -1.0f,  1.0f, -1.0f,  0.0f,  0.0f, -1.0f, 0.0f, 1.0f, // top-left
                        // front face
                        -1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 0.0f, // bottom-left
                        1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 0.0f, // bottom-right
                        1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 1.0f, // top-right
                        1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 1.0f, 1.0f, // top-right
                        -1.0f,  1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 1.0f, // top-left
                        -1.0f, -1.0f,  1.0f,  0.0f,  0.0f,  1.0f, 0.0f, 0.0f, // bottom-left
                        // left face
                        -1.0f,  1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-right
                        -1.0f,  1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 1.0f, // top-left
                        -1.0f, -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-left
                        -1.0f, -1.0f, -1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-left
                        -1.0f, -1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 0.0f, 0.0f, // bottom-right
                        -1.0f,  1.0f,  1.0f, -1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-right
                        // right face
                        1.0f,  1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-left
                        1.0f, -1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-right
                        1.0f,  1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 1.0f, // top-right         
                        1.0f, -1.0f, -1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 1.0f, // bottom-right
                        1.0f,  1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 1.0f, 0.0f, // top-left
                        1.0f, -1.0f,  1.0f,  1.0f,  0.0f,  0.0f, 0.0f, 0.0f, // bottom-left     
                        // bottom face
                        -1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 1.0f, // top-right
                        1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 1.0f, // top-left
                        1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 0.0f, // bottom-left
                        1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 1.0f, 0.0f, // bottom-left
                        -1.0f, -1.0f,  1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 0.0f, // bottom-right
                        -1.0f, -1.0f, -1.0f,  0.0f, -1.0f,  0.0f, 0.0f, 1.0f, // top-right
                        // top face
                        -1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 1.0f, // top-left
                        1.0f,  1.0f , 1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 0.0f, // bottom-right
                        1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 1.0f, // top-right     
                        1.0f,  1.0f,  1.0f,  0.0f,  1.0f,  0.0f, 1.0f, 0.0f, // bottom-right
                        -1.0f,  1.0f, -1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 1.0f, // top-left
                        -1.0f,  1.0f,  1.0f,  0.0f,  1.0f,  0.0f, 0.0f, 0.0f  // bottom-left        
                    };
                cubeVAO = glGenVertexArray();
                cubeVBO = glGenBuffer();
                // fill buffer
                glBindBuffer(BufferTarget.ArrayBuffer, cubeVBO);
                fixed (float* temp = &vertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, intptr, GL_STATIC_DRAW);
                }

                // link vertex attributes
                glBindVertexArray(cubeVAO);
                glEnableVertexAttribArray(0);
                glVertexAttribPointer(0, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0);
                glEnableVertexAttribArray(1);
                glVertexAttribPointer(2, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(6 * sizeof(float)));
                glEnableVertexAttribArray(2);
                glVertexAttribPointer(1, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                
                glBindBuffer(BufferTarget.ArrayBuffer, 0);
                glBindVertexArray(0);
            }
            // render Cube
            glBindVertexArray(cubeVAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 36);
            glBindVertexArray(0);
        }
    }
}