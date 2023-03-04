using COREMath;
using static CORERenderer.OpenGL.GL;
using CORERenderer.textures;
using CORERenderer.Main;
using CORERenderer.Loaders;

namespace CORERenderer.OpenGL
{
    public class Rendering
    {
        private static int totalAmountOfTransferredBytes = 0;
        private static int lastAmountOfTransferredBytes = 0;

        public static int unresolvedInstances = 0;
        private static int estimatedDataLoss = 0;
        public static int shaderByteSize = 0;

        public static int TotalAmountOfTransferredBytes { get { return totalAmountOfTransferredBytes; } set { totalAmountOfTransferredBytes += value; lastAmountOfTransferredBytes = value; } }
        public static string TotalAmountOfTransferredBytesString { get { if (totalAmountOfTransferredBytes >= 1000000) return $"{MathF.Round(totalAmountOfTransferredBytes * 0.000001f):N0} MB"; else if (totalAmountOfTransferredBytes >= 1000) return $"{MathF.Round(totalAmountOfTransferredBytes * 0.001f):N0} KB"; else return $"{totalAmountOfTransferredBytes}"; } }
        public static string LastAmountOfTransferredBytesString { get { if (lastAmountOfTransferredBytes >= 1000000) return $"{MathF.Round(lastAmountOfTransferredBytes * 0.000001f):N0} MB"; else if (lastAmountOfTransferredBytes >= 1000) return $"{MathF.Round(lastAmountOfTransferredBytes * 0.001f):N0} KB"; else return $"{lastAmountOfTransferredBytes}"; } }
        public static string EstimatedDataLossString { get { if (estimatedDataLoss >= 1000000) return $"{MathF.Round(estimatedDataLoss * 0.000001f):N0} MB"; else if (estimatedDataLoss >= 1000) return $"{MathF.Round(estimatedDataLoss * 0.001f):N0} KB"; else return $"{estimatedDataLoss}"; } }
        public static string TotalShaderByteSizeString { get { if (shaderByteSize >= 1000000) return $"{MathF.Round(shaderByteSize * 0.000001f):N0} MB"; else if (shaderByteSize >= 1000) return $"{MathF.Round(shaderByteSize * 0.001f):N0} KB"; else return $"{shaderByteSize}"; } }

        private static uint lineVBO;
        private static uint lineVAO;

        public static bool cullFaces = true;

        public static int drawCalls = 0;

        public static void Init()
        {
            GenericShaders.SetShaders();
        }

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, ReadOnlySpan<byte> pixels)
        {
            unsafe
            {
                fixed (byte* temp = &pixels[0])
                {
                    IntPtr intptr = new(temp);
                    GlTexImage2D(target, level, internalFormat, width, height, border, format, type, intptr);
                }
            }
            TotalAmountOfTransferredBytes = pixels.Length * sizeof(byte); //bytes are 1 byte of size but still
        }

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr pixels)
        {
            unsafe
            {
                GlTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels);
            }
            unresolvedInstances++;
            estimatedDataLoss += width * height * 4;
        }

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, byte[] pixels)
        {
            unsafe
            {
                if (pixels != null)
                    fixed (byte* temp = &pixels[0])
                    {
                        IntPtr intptr = new(temp);
                        GlTexImage2D(target, level, internalFormat, width, height, border, format, type, intptr);
                        TotalAmountOfTransferredBytes = pixels.Length;
                    }
                else
                    GlTexImage2D(target, level, internalFormat, width, height, border, format, type, null);
            }
        }

        public static byte[] SaveAsFile(Framebuffer fb, int width, int height)
        {
            glBindTexture(GL_TEXTURE_2D, fb.Texture);

            byte[] pixels = new byte[width * height * 4];
            unsafe
            {
                fixed (byte* p = pixels)
                {
                    IntPtr intptr = new(p);
                    glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, p);
                }
            }
            return pixels;
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">Specifies the starting index in the enabled arrays.</param>
        /// <param name="count">Specifies the number of indices to be rendered.</param>
        public static void glDrawArrays(PrimitiveType mode, int first, int count)
        {
            drawCalls++;
            GlDrawArrays((int)mode, first, count);
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        public unsafe static void glDrawElements(PrimitiveType mode, int count, GLType type, void* indices)
        {
            drawCalls++;
            GlDrawElements((int)mode, count, (int)type, indices);
        }

        /// <summary>
        /// best to not use when drawing many lines since it will use a lot of draw calls
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="color"></param>
        public static void RenderLine(Vector3 start, Vector3 end, Vector3 color)
        {
            if (lineVBO == 0)
            {
                GenerateEmptyBuffer(out lineVBO, out lineVAO, sizeof(float) * 4);

                int vertexLocation = GenericShaders.solidColorQuadShader.GetAttribLocation("aPos");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);
            }

            GenericShaders.solidColorQuadShader.Use();

            GenericShaders.solidColorQuadShader.SetVector3("color", color);
            glDrawArrays(PrimitiveType.Lines, 0, 2);
        }

        /// <summary>
        /// best to not use when drawing many lines since it will use a lot of draw calls
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static void RenderLine(Vector3 start, Vector3 end) => RenderLine(start, end, new Vector3(1, 1, 1));

        /// <summary>
        /// best to not use when drawing many lines since it will use a lot of draw calls
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static void RenderLine(Vector2 start, Vector2 end) => RenderLine(new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0), new Vector3(1, 1, 1));
        public static void RenderLine(Vector2 start, Vector2 end, Vector3 color) => RenderLine(new Vector3(start.x, start.y, 0), new Vector3(end.x, end.y, 0), color);

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
            glBindBuffer(GL_ARRAY_BUFFER, VBO);

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
            glBindBuffer(GL_ARRAY_BUFFER, VBO);
            
            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), vertices, GL_STATIC_DRAW);

            TotalAmountOfTransferredBytes = vertices.Length * sizeof(float);
        }

        public static void GenerateFilledBuffer(out uint VBO, out uint VAO, Matrix[] matrices)
        {
            VBO = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, VBO);

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
            glBindBuffer(GL_ARRAY_BUFFER, VBO);

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
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);

            glBufferData(GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), indices, GL_STATIC_DRAW);

            TotalAmountOfTransferredBytes = indices.Length * sizeof(uint);
        }

        //try referring to offcenter version with 0, width, height, 0.01, 100
        public static Matrix GetOrthograpicProjectionMatrix() => Matrix.Createorthographic(COREMain.Width, COREMain.Height, -1000f, 1000f);//Matrix.Createorthographic(COREMain.Width, COREMain.Height, 0.01f, 1000f);

        public static void RenderBackground(HDRTexture h)
        {
            GenericShaders.backgroundShader.Use();
            GenericShaders.backgroundShader.SetInt("environmentMap", GL_TEXTURE0);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, h.envCubeMap);

            glDrawArrays(PrimitiveType.Triangles, 0, 36);
        }
        
        private static Model backgroundModel = null;

        public static List<Submodel> translucentSubmodels = new();

        public static void RenderAllModels(List<Model> models)
        {
            if (cullFaces)
                glEnable(GL_CULL_FACE);
            else 
                glDisable(GL_CULL_FACE);

            

            List<Model> translucentModels = new();
            foreach (Model model in models)
            {
                if (model == null)
                    continue;
                if (model.type != RenderMode.HDRFile)
                    model.Render();
                else
                    backgroundModel = model;
            }
            
            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            //glStencilMask(0x00);

            RenderLights(COREMain.lights);

            backgroundModel?.Render();

            //depth sorting
            List<float> distances = new();
            Dictionary<float, Submodel> distanceModelTable = new();
            foreach (Submodel model in translucentSubmodels) //unfinished
            {
                float distance = MathC.Distance(COREMain.GetCurrentScene.camera.position, model.translation + model.parent.translation);
                distances.Add(distance);
                if (!distanceModelTable.ContainsKey(distance))
                    distanceModelTable.Add(distance, model);
                else
                    distanceModelTable.Add(distance + 0.1f, model);
            }

            float[] distancesArray = distances.ToArray();
            Array.Sort(distancesArray);
            Array.Reverse(distancesArray);
            for (int i = 0; i < distancesArray.Length; i++)
                translucentSubmodels[i] = distanceModelTable[distancesArray[i]];

            foreach (Submodel model in translucentSubmodels)
                model.Render();

            translucentSubmodels = new();
        }

        public static void RenderLights(List<Light> locations)
        {
            GenericShaders.lightingShader.Use();

            glBindVertexArray(COREMain.vertexArrayObjectLightSource);

            for (int i = 0; i < locations.Count; i++)
            {
                GenericShaders.lightingShader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(locations[i].position) * MathC.GetScalingMatrix(0.2f));
                glDrawArrays(PrimitiveType.Triangles, 0, 36);
            }
        }

        public static void RenderGrid()
        {
            GenericShaders.gridShader.Use();

            GenericShaders.gridShader.SetMatrix("model", Matrix.IdentityMatrix * new Matrix(true, 500));

            GenericShaders.gridShader.SetVector3("playerPos", COREMain.scenes[COREMain.selectedScene].camera.position);

            glDisable(GL_CULL_FACE);
            glBindVertexArray(COREMain.vertexArrayObjectGrid);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            glEnable(GL_CULL_FACE);
        }

        public static void RenderCubemap(Cubemap cubemap)
        {
            glDepthFunc(GL_LEQUAL);
            cubemap.shader.Use();

            glBindVertexArray(cubemap.VAO);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, cubemap.textureID);

            glDrawArrays(PrimitiveType.Triangles, 0, 36);

            glBindVertexArray(0);
            glDepthFunc(GL_LESS);
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
                glBindBuffer(GL_ARRAY_BUFFER, cubeVBO);
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
                glVertexAttribPointer(1, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                glEnableVertexAttribArray(2);
                glVertexAttribPointer(2, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(6 * sizeof(float)));
                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);
            }
            // render Cube
            glBindVertexArray(cubeVAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 36);
            glBindVertexArray(0);
        }
    }
}
