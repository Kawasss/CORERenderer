using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Main;
using COREMath;
using CORERenderer.shaders;
using StbiSharp;
using CORERenderer.OpenGL;
using System;

namespace CORERenderer.textures
{
    public class Texture
    {
        public readonly uint Handle;
        public string path;
        public string name;
        public int width;
        public int height;

        public byte[] FileContent;
        public byte[] Data;// { get { return GetData(); } }

        public Texture(string name, int width, int height, byte[] data)
        {
            Handle = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, Handle);

            glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

            glGenerateMipmap(GL_TEXTURE_2D);

            this.Data = data;
            this.name = name;
        }

        private static Texture ReadFromColorFile(bool flip, int mode, string imagePath)
        {
            Stbi.SetFlipVerticallyOnLoad(flip);

            uint handle = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);

            if (!File.Exists(imagePath))
            {
                COREMain.console.WriteError($"Couldn't find given texture at {imagePath}, using default texture");
                imagePath = $"{COREMain.pathRenderer}\\textures\\placeholder.png";
            }
            StbiImage image;
            int imageHeight = 0, imageWidth = 0;
            byte[] imageData = Array.Empty<byte>();
            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);

                Task task = Task.Run(() => { image = Stbi.LoadFromMemory(memoryStream, 4); imageWidth = image.Width; imageHeight = image.Height; imageData = image.Data.ToArray(); } );
                task.Wait();

                glTexImage2D(Image2DTarget.Texture2D, 0, mode, imageWidth, imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, imageData);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glGenerateMipmap(GL_TEXTURE_2D);
            }

            return new Texture(handle) { path = imagePath, name = Path.GetFileNameWithoutExtension(imagePath), width = imageWidth, height = imageHeight, FileContent = File.ReadAllBytes(imagePath) };//, Data = image.Data.ToArray() };
        }

        public static Texture GenerateEmptyTexture(int Width, int Height)
        {
            uint handle = glGenTexture();
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGBA32F, Width, Height, 0, GL_RGBA, GL_FLOAT, null);

            return new(handle) { width = Width, height = Height };
        }

        /// <summary>
        /// Reads the file in the RGBA color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromFile(string imagePath) => ReadFromColorFile(true, GL_RGBA, imagePath);

        /// <summary>
        /// Reads the file in the RGBA color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromFile(bool flip, string imagePath) => ReadFromColorFile(flip, GL_RGBA, imagePath);

        /// <summary>
        /// Reads the file in the SRGB color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromSRGBFile(string imagePath) => ReadFromColorFile(true, GL_SRGB, imagePath);

        /// <summary>
        /// Reads the file in the RGB color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromRGBFile(string imagePath) => ReadFromColorFile(true, GL_RGB, imagePath);

        public Texture(uint newHandle)
        {
            Handle = newHandle;
        }

        public void Use(int texture)
        {
            glActiveTexture(texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }

        private unsafe byte[] GetData()
        {
            this.Use(GL_TEXTURE0);

            int textureWidth = glGetTexLevelParameteri(GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH);
            int textureHeight = glGetTexLevelParameteri(GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT);
            byte[] pixels = new byte[textureWidth * textureHeight * 4];
            fixed (byte* temp = &pixels[0])
            {
                glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, temp);
            }
            return pixels;
        }
        public unsafe void GetData(out byte[] pixels, out int textureWidth, out int textureHeight)
        {
            this.Use(GL_TEXTURE0);

            textureWidth = glGetTexLevelParameteri(GL_TEXTURE_2D, 0, GL_TEXTURE_WIDTH);
            textureHeight = glGetTexLevelParameteri(GL_TEXTURE_2D, 0, GL_TEXTURE_HEIGHT);
            pixels = new byte[textureWidth * textureHeight * 4];
            fixed (byte* temp = &pixels[0])
            {
                glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, temp);
            }
        }

        private uint VAO, VBO;
        public void RenderAs2DImage(int x, int y)
        {
            if (VBO == 0)
            {
                Rendering.GenerateFilledBuffer(out VBO, out VAO, Rendering.GenerateQuadVerticesWithUV(x, y, (int)((float)width * (float)(200f / width)), (int)((float)height * (float)(200f / height)))); //may cause images to appear stretched or shrunk
                int vertexLocation = GenericShaders.Image2D.GetAttribLocation("vertex");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 4, GL_FLOAT, false, 4 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);
            }

            GenericShaders.Image2D.SetInt("Texture", GL_TEXTURE0);
            this.Use(GL_TEXTURE0);

            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        public static unsafe void WriteAsPNG(string destination, uint textureHandle, int width, int height)
        {
            StbImageWriteSharp.StbImageWrite.stbi_flip_vertically_on_write(1);

            byte[] pixels = new byte[width * height * 4];
            fixed (byte* temp = &pixels[0])
            {
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, textureHandle);
                glGetTexImage(GL_TEXTURE_2D, 0, GL_RGBA, GL_UNSIGNED_BYTE, temp);
                using FileStream fs = File.Create(destination);
                int amount;
                byte* image = StbImageWriteSharp.StbImageWrite.stbi_write_png_to_mem(temp, width * 4, width, height, 4, &amount);
                Span<byte> writeableBytes = new(image, amount);
                fs.Write(writeableBytes);
            }
        }

        public static void WriteAsPNG(string destination, Texture texture, int width, int height) => WriteAsPNG(destination, texture.Handle, width, height);

        public void WriteAsPNG(string destination) => WriteAsPNG(destination, this.Handle, this.width, this.height);
    }

    public class HDRTexture
    {
        public readonly uint Handle;
        public string path;
        public string name;

        uint FBO;
        uint RBO;

        public uint envCubeMap;

        private uint VAO2D;
        private uint VBO2D;

        public int width2D;
        public int height2D;

        private Shader shader = new($"{COREMain.pathRenderer}\\shaders\\HDRCube.vert", $"{COREMain.pathRenderer}\\shaders\\HDRCube.frag");

        private Shader testShader = new ($"{COREMain.pathRenderer}\\shaders\\2DImage.vert", $"{COREMain.pathRenderer}\\shaders\\2DImage.frag");

        public static unsafe HDRTexture ReadFromFile(string imagePath)
        {
            Stbi.SetFlipVerticallyOnLoad(true);

            HDRTexture h = new(glGenTexture());

            if (!File.Exists(imagePath))
            {
                throw new Exception($"Couldnt find file at {imagePath}");
            }

            h.FBO = glGenFramebuffer();
            h.RBO = glGenRenderbuffer();

            glBindFramebuffer(GL_FRAMEBUFFER, h.FBO);
            glBindRenderbuffer(GL_RENDERBUFFER, h.RBO);
            glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT24, 512, 512);
            glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, h.RBO);

            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);

                h.width2D = image.Width;
                h.height2D = image.Height;

                //for optimal coordinates height is relative to the width of the image
                float height = (float)image.Height / (float)image.Width;

                h.VBO2D = glGenBuffer();
                glBindBuffer(BufferTarget.ArrayBuffer, h.VBO2D);

                float[] vertices = new float[]
                {
                    -1, height, 0, 1,
                    -1,      -1, 0, 0,
                    1,      -1, 1, 0,

                    -1, height, 0, 1,
                    1,      -1, 1, 0,
                    1, height, 1, 1
                };

                fixed (float* temp = &vertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                }

                h.VAO2D = glGenVertexArray();
                glBindVertexArray(h.VAO2D);

                int vertexLocation = h.testShader.GetAttribLocation("vertex");
                glVertexAttribPointer((uint)vertexLocation, 4, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                h.testShader.Use();
                h.testShader.SetInt("Texture", GL_TEXTURE0);
                h.testShader.SetMatrix("projection", GetOrthograpicProjectionMatrix(COREMain.Width, COREMain.Height));

                glBindBuffer(BufferTarget.ArrayBuffer, 0);
                glBindVertexArray(0);

                glBindTexture(GL_TEXTURE_2D, h.Handle);

                float[] data = new float[image.Data.Length];
                for (int i = 0; i < data.Length; i++)
                    data[i] = image.Data[i];

                glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGB, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, image.Data);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                List<int> local = new();
                for (int i = imagePath.IndexOf("\\"); i > -1; i = imagePath.IndexOf("\\", i + 1))
                    local.Add(i);
            }

            h.envCubeMap = glGenTexture();
            glBindTexture(GL_TEXTURE_CUBE_MAP, h.envCubeMap);
            for (int i = 0; i < 6; i++)
                glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB16F, 512, 512, 0, GL_RGB, GL_FLOAT, null);

            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            Matrix captureProjection = Matrix.CreatePerspectiveFOV(MathC.PiF / 2, 1, 0.1f, 10);//COREMain.Width / COREMain.Height
            Matrix[] captureViews =
            {
                MathC.LookAt(new(0, 0, 0), new(1, 0, 0), new(0, -1, 0)),
                MathC.LookAt(new(0, 0, 0), new(-1, 0, 0), new(0, -1, 0)),
                MathC.LookAt(new(0, 0, 0), new(0, 1, 0), new(0, 0, 1)),
                MathC.LookAt(new(0, 0, 0), new(0, -1, 0), new(0, 0, -1)),
                MathC.LookAt(new(0, 0, 0), new(0, 0, 1), new(0, -1, 0)),
                MathC.LookAt(new(0, 0, 0), new(0, 0, -1), new(0, -1, 0)),
            };

            h.shader.Use();

            h.shader.SetInt("equirectangularMap", GL_TEXTURE0);
            h.shader.SetMatrix("projection", captureProjection);
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, h.Handle);

            glViewport(0, 0, 512, 512);
            glBindFramebuffer(GL_FRAMEBUFFER, h.FBO);

            glDisable(GL_CULL_FACE);

            for (int i = 0; i < 6; i++)
            {
                h.shader.SetMatrix("view", captureViews[i]);
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, h.envCubeMap, 0);
                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                Rendering.RenderCube();
            }

            glEnable(GL_CULL_FACE);
            return h;
        }

        public void RenderAs2DTexture()
        {
            glDisable(GL_CULL_FACE);
            
            glViewport(COREMain.Width - 350, 0, 350, (int)(350f * ((float)height2D / (float)width2D)));

            GenericShaders.Image2D.Use();
            this.Use(GL_TEXTURE0);

            glBindVertexArray(VAO2D);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);

            glViewport(0, 0, COREMain.Width, COREMain.Height);
            glEnable(GL_CULL_FACE);
        }

        public HDRTexture(uint newHandle)
        {
            Handle = newHandle;
        }

        public void Use(int texture)
        {
            glActiveTexture(texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }
    }
} 