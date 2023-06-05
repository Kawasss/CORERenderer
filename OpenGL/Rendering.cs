using COREMath;
using CORERenderer.Main;
using CORERenderer.Loaders;
using System.Diagnostics;
using CORERenderer.textures;

namespace CORERenderer.OpenGL
{
    public partial class Rendering : GL
    {
        public static int CurrentBoundVAO { get => GetBoundVAO(); }
        public static int CurrentFramebufferID { get => GetCurrentFramebufferID(); }
        public static Camera Camera { get => camera; set => camera = value; }
        public static string[] RenderStatistics { get { return renderStatistics; } }
        public static long TicksSpent3DRenderingThisFrame { get { return ticksSpent3DRenderingThisFrame; } }
        public static int[] Viewport { get => GetViewportDimensions(); }
        public static Skybox DefaultSkybox { get => standardSkybox; }

        private static uint vertexArrayObjectGrid;

        public static bool cullFaces = true;
        public static bool renderOrthographic = false;
        public static bool renderLights = true;
        public static bool renderReflections = true;
        public static bool renderShadows = true;

        public static ShaderType shaderConfig = ShaderType.PBR;

        private static Camera camera = null;

        private static string[] renderStatistics = new string[9] { "Ticks spent rendering opaque models: 0", "Ticks spent rendering translucent models: 0", "Ticks spent depth sorting: 0", "Ticks spent overall: 0", "Models rendered: 0", "Submodels rendered: 0, of which:", "   0 are translucent", "   0 are opaque", "Draw calls this frame: 0" };

        private static long ticksSpent3DRenderingThisFrame = 0;

        private static Framebuffer shadowFramebuffer;
        public static Cubemap shadowCubemap;
        private static Framebuffer reflectionFramebuffer;
        public static Cubemap reflectionCubemap;
        private static Camera reflectionCamera;
        private static Camera shadowCamera;
        private static Skybox standardSkybox;

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
                reflectionQuality = value;
                reflectionCubemap = GenerateEmptyCubemap((int)(renderingWidth / reflectionQuality), (int)(renderingWidth / reflectionQuality));
                reflectionFramebuffer = GenerateFramebuffer((int)(renderingWidth / reflectionQuality), (int)(renderingWidth / reflectionQuality));
                reflectionFramebuffer.Bind();
                glBindFramebuffer(0);
            }
        }
        public static float ShadowQuality
        {
            get => shadowQuality; set
            {//both width because afaik its better to have a perfect cube and not a stretched one
                shadowQuality = value;
                shadowCubemap = GenerateEmptyCubemap((int)(renderingWidth / shadowQuality), (int)(renderingWidth / shadowQuality), GL_LINEAR);
                shadowFramebuffer = GenerateFramebuffer((int)(renderingWidth / shadowQuality), (int)(renderingWidth / shadowQuality));
                shadowFramebuffer.Bind();
                glBindFramebuffer(0);
            }
        }
        private static float shadowQuality = OpenGL.TextureQuality.Ultra;
        private static float reflectionQuality = OpenGL.TextureQuality.Ultra;
        private static float textureQuality = OpenGL.ShadowQuality.Default;

        /// <summary>
        /// Gets the color used with glClearColor (default is 0.3f, 0.3f, 0.3f, 1), sets the same color
        /// </summary>
        public static Vector4 ClearColor { get => clearColor; set => clearColor = value; }
        private static Vector4 clearColor = new(1f, 1f, 1f, 1);

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
            ReflectionQuality = OpenGL.ReflectionQuality.Default;
            ShadowQuality = OpenGL.ShadowQuality.Default;
            standardSkybox = Skybox.ReadFromFile($"{COREMain.BaseDirectory}\\textures\\hdr\\defaultSkybox.hdr", TextureQuality);
            reflectionFramebuffer.Bind();
            SetClearColor(clearColor);
            camera = new(Vector3.Zero, 1);
            reflectionCamera = new(Vector3.Zero, 1);
        }

        private unsafe static void RenderShadowCubemap(List<Model> models, List<Main.Light> lights)
        {
            if (lights.Count <= 0)
                return;

            int[] originalViewportDimensions = GetViewportDimensions();
            int previousFB = GetCurrentFramebufferID();

            shadowFramebuffer.Bind();

            SetShadowValues(lights);

            glViewport(0, 0, (int)((float)renderingWidth / (float)shadowQuality), (int)((float)renderingWidth / (float)shadowQuality));

            shadowFramebuffer.Bind();

            Matrix[] viewMatrices = new Matrix[]
            {
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3( 1,  0,  0), new(0, -1,  0)),
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3(-1,  0,  0), new(0, -1,  0)),
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3( 0,  1,  0), new(0,  0,  1)),
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3( 0, -1,  0), new(0,  0, -1)),
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3( 0,  0,  1), new(0, -1,  0)),
                MathC.LookAt(lights[0].position, lights[0].position + new Vector3( 0,  0, -1), new(0, -1,  0))
            };

            RenderShadows(models, viewMatrices);

            glBindFramebuffer((uint)previousFB);
            glViewport(originalViewportDimensions[0], originalViewportDimensions[1], originalViewportDimensions[2], originalViewportDimensions[3]);
        }

        private static void SetShadowValues(List<Light> lights)
        {
            glEnable(GL_CULL_FACE);
            glCullFace(GL_FRONT);

            float nearPlane = camera.NearPlane;
            float farPlane = camera.FarPlane;
            float oldFov = Camera.Fov;
            float oldAR = camera.AspectRatio;
            Camera.Fov = 90;
            Camera.AspectRatio = 1;
            Matrix shadowProjection = Camera.ProjectionMatrix;//Matrix.CreatePerspectiveFOV(MathC.DegToRad(90), aspectRatio, nearPlane, farPlane);
            camera.Fov = oldFov;
            Camera.AspectRatio = oldAR;
            
            GenericShaders.Shadow.Use();
            //for (int i = 0; i < 6; i++)
            //    GenericShaders.Shadow.SetMatrix($"shadowMatrices[{i}]", viewMatrices[i]);
            GenericShaders.Shadow.SetMatrix("projection", shadowProjection);
            GenericShaders.Shadow.SetVector3("lightPos", lights[0].position);
            GenericShaders.Shadow.SetFloat("farPlane", Camera.FarPlane);
        }

        private static void RenderShadows(List<Model> models, Matrix[] views)
        {
            shadowCubemap.Use(GL_TEXTURE0);

            for (int i = 0; i < 6; i++)
            {
                shadowFramebuffer.Bind();
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, shadowCubemap.textureID, 0);
                glClearColor(1, 1, 1, 1);
                glClear(GL_DEPTH_BUFFER_BIT | GL_COLOR_BUFFER_BIT);

                GenericShaders.Shadow.SetMatrix($"view", views[i]);
                foreach (Model model in models)
                    model.RenderShadow();
            }
            glCullFace(GL_BACK);
        }

        private static Vector2[] pitchAndYaw = new Vector2[] { new(0, 0), new(0, 180), new(90, 180), new(-90, 180), new(0, 90), new(0, -90) };
        private static Vector3[] ups = new Vector3[] { new(0, -1, 0), new(0, -1, 0), new(0, 0, 1), new(0, 0, -1), new(0, -1, 0), new(0, -1, 0) };
        private static Vector3[] fronts = new Vector3[] { new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, -1, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) };
        private static void RenderReflections(List<Model> models, List<Light> lights, Skybox skybox)
        {
            int previousFB = CurrentFramebufferID;

            GenericShaders.Lighting.Use();
            reflectionFramebuffer.Bind();
            shadowCubemap.Use(GL_TEXTURE8);
            skybox?.irradianceMap.Use(GL_TEXTURE7);
            reflectionCubemap.Use(GL_TEXTURE6);
            
            GenericShaders.Lighting.SetInt("irradianceMap", 7);
            int[] viewport = ViewportDimensions;

            reflectionCamera = new(camera);
            reflectionCamera.Fov = 90;
            reflectionCamera.AspectRatio = 1;

            glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
            MatrixToUniformBuffer(reflectionCamera.ProjectionMatrix, 0);

            glViewport(0, 0, (int)((float)renderingWidth / (float)shadowQuality), (int)((float)renderingWidth / (float)shadowQuality));

            Submodel.renderAllIDs = false;
            for (int i = 0; i < 6; i++)
            {
                reflectionCamera.Pitch = pitchAndYaw[i].x;
                reflectionCamera.Yaw = pitchAndYaw[i].y;
                reflectionCamera.up = ups[i];

                glBindBuffer(BufferTarget.UniformBuffer, uboMatrices);
                MatrixToUniformBuffer(reflectionCamera.ViewMatrix, GL_MAT4_FLOAT_SIZE);

                reflectionFramebuffer.Bind();
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, reflectionCubemap.textureID, 0);
                glClearColor(0.3f, 0.3f, 0.3f, 1);
                glClear(GL_DEPTH_BUFFER_BIT | GL_COLOR_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

                //RenderGrid();

                RenderLights(reflectionCamera, lights);
                foreach (Model model in models)
                    //if (model.Transform.BoundingBox.IsInFrustum(reflectionCamera.Frustum, model.Transform))
                        model.Render();
                    
                skybox?.Render();
            }
            reflectionCubemap.Use(GL_TEXTURE0);
            glGenerateMipmap(GL_TEXTURE_CUBE_MAP);

            Submodel.renderAllIDs = true;

            glViewport(viewport[0], viewport[1], viewport[2], viewport[3]);
            glBindFramebuffer((uint)previousFB);

            UpdateUniformBuffers();
        }

        public static List<Submodel> translucentSubmodels = new();

        public static void RenderScene(Scene scene) //experimental but can work
        {
            if (renderReflections)
                RenderReflections(scene.models, scene.lights, scene.skybox);
            if (renderLights)
                RenderLights(camera, scene.lights);
            RenderAllModels(scene.models);
            //shadowCubemap.Render();
            scene.skybox?.Render();
        }

        public static void RenderAllModels(List<Model> models)
        {
            int modelsFrustumCulled = 0;
            int currentDrawCalls = drawCalls;
            Stopwatch sw = new();

            //GenericShaders.GenericLighting.SetVector3("viewPos", camera.position);

            if (cullFaces)
                glEnable(GL_CULL_FACE);
            else
                glDisable(GL_CULL_FACE);

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            GenericShaders.Lighting.Use();

            sw.Start();

            reflectionCubemap.Use(GL_TEXTURE6);
            //GenericShaders.PBR.SetFloat("farPlane", camera.FarPlane);

            for (int i = 0; i < models.Count; i++)
            {
                if (models[i] == null)
                    continue;

                if (models[i].CanBeCulled)
                {
                    modelsFrustumCulled++;
                    continue;
                }
                models[i].Render();
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

        private static unsafe int GetCurrentFramebufferID()
        {
            int value;
            glGetIntegerv(GL_FRAMEBUFFER_BINDING, &value);
            return value;
        }

        private static unsafe int GetBoundVAO()
        {
            int value;
            glGetIntegerv(GL_VERTEX_ARRAY_BINDING, &value);
            return value;
        }

        public static float[] GenerateQuadVerticesWithoutUV(float x, float y, float width, float height)
        {
            return new float[]
            {
                x, y + height,
                x, y,
                x + width, y,

                x, y + height,
                x + width, y,
                x + width, y + height
            };
        }

        public static float[] GenerateQuadVerticesWithUV(float x, float y, float width, float height)
        {
            return new float[]
            {
                x,         y + height,         0, 1,
                x,         y,                  0, 0,
                x + width, y,                  1, 0,

                x,         y + height,         0, 1,
                x + width, y,                  1, 0,
                x + width, y + height,         1, 1
            };
        }

        public static uint GenerateBufferlessVAO()
        {
            //allows the object to render bufferless
            uint VAO = glGenVertexArray();
            glBindVertexArray(VAO);
            glBindVertexArray(0);
            return VAO;
        }

        /// <summary>
        /// Generates an empty buffer for buffersubdata, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO">VBO</param>
        /// <param name="VAO">VAO</param>
        /// <param name="size">size of the buffer in bytes</param>
        public static void GenerateEmptyBuffer(out uint VBO, out uint VAO, int sizeInBytes) => GenerateEmptyBuffer(Usage.DynamicDraw, out VBO, out VAO, sizeInBytes);
        public static void GenerateEmptyBuffer(Usage usage, out uint VBO, out uint VAO, int sizeInBytes)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            glBufferData(GL_ARRAY_BUFFER, sizeInBytes, (IntPtr)null, (int)usage);

            TotalAmountOfTransferredBytes = sizeInBytes;
        }

        /// <summary>
        /// Generates a buffer with the given vertices, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO"></param>
        /// <param name="VAO"></param>
        /// <param name="vertices"></param>
        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, float[] vertices) => GenerateFilledBuffer(Usage.StaticDraw, out VBO, out VAO, vertices);
        public static void GenerateFilledBuffer(Usage usage, out uint VBO, out uint VAO, float[] vertices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), vertices, (int)usage);

            TotalAmountOfTransferredBytes = vertices.Length * sizeof(float);
        }

        /// <summary>
        /// Generates a buffer with the given vertices, does not active the vertex attribute arrays however.
        /// </summary>
        /// <param name="VBO"></param>
        /// <param name="VAO"></param>
        /// <param name="vertices"></param>
        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, Vertex[] vertices) => GenerateFilledBuffer(Usage.StaticDraw, out VBO, out VAO, vertices);
        public static void GenerateFilledBuffer(Usage usage, out uint VBO, out uint VAO, Vertex[] vertices)
        {
            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            float[] nVertices = Vertex.GetFloatList(vertices.ToList()).ToArray();

            glBufferData(GL_ARRAY_BUFFER, nVertices.Length * sizeof(float), nVertices, (int)usage);

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

            TotalAmountOfTransferredBytes = 16 * matrices.Length * sizeof(float);
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

        private static Transform lightTransform = new(Vector3.Zero, Vector3.Zero, new(1, 1, 1), new(.1f), Vector3.Zero);
        public static void RenderLights(Camera camera, List<Light> locations)
        {
            GenericShaders.Light.Use();

            glBindVertexArray(Main.COREMain.vertexArrayObjectLightSource);

            for (int i = 0; i < locations.Count; i++)
            {
                lightTransform.translation = locations[i].position;
                lightTransform.boundingBox.center = locations[i].position;

                //if (lightTransform.boundingBox.IsInFrustum(camera.Frustum, lightTransform))
                //{
                    GenericShaders.Light.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(locations[i].position) * MathC.GetScalingMatrix(0.2f));
                    glDrawArrays(PrimitiveType.Triangles, 0, 36);
                //}
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