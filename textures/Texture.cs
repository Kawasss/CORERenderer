using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Main;
using COREMath;
using CORERenderer.shaders;
using StbiSharp;
using CORERenderer.OpenGL;
using Console = CORERenderer.GUI.Console;
using System.Reflection.Metadata;
using System.Diagnostics;
using SharpFont;
using System.Text.Unicode;

namespace CORERenderer.textures
{
    public class Texture
    {
        public uint Handle;
        public string path;
        public string name;
        public int width;
        public int height;
        private int mode;

        public byte[] FileContent;
        public byte[] Data = Array.Empty<byte>();// { get { return GetData(); } }
        private int dataSize = 0;

        private long timeToRead = 0;
        private bool flipped = true;

        public string Log
        {
            get =>
                $"""
                Read 2D texture {name}:
                    Path: {path}
                    Mode: 0x{string.Concat(BitConverter.ToString(BitConverter.GetBytes(mode)).Where(c => c != '-'))}
                    Read in: {timeToRead} ms
                    Flipped: {flipped}
                    Dimensions: {width}x{height}
                    Data size: {dataSize}
                """;
        }

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
            Stopwatch timer = new();
            timer.Start();

            Stbi.SetFlipVerticallyOnLoad(flip);

            uint handle = glGenTexture();
            
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);

            if (!File.Exists(imagePath))
            {
                Console.WriteError($"Couldn't find given texture at {imagePath}, using default texture");
                imagePath = $"{COREMain.BaseDirectory}\\textures\\placeholder.png";
            }
            StbiImage image;
            int imageHeight = 0, imageWidth = 0;
            byte[] imageData = Array.Empty<byte>();
            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                stream.CopyTo(memoryStream);
                Job task = new(() => { image = Stbi.LoadFromMemory(memoryStream, 4); imageWidth = image.Width; imageHeight = image.Height; imageData = image.Data.ToArray(); } );
                task.Start();
                task.Wait();

                glTexImage2D(Image2DTarget.Texture2D, 0, mode, imageWidth, imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, imageData);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glGenerateMipmap(GL_TEXTURE_2D);
            }
            timer.Stop();
            System.Console.WriteLine($"Read 2D texture {Path.GetFileNameWithoutExtension(imagePath)}:\n    Path: {imagePath}\n    Mode: 0x{string.Concat(BitConverter.ToString(BitConverter.GetBytes(mode)).Where(c => c != '-'))}\n    Read in: {timer.ElapsedMilliseconds} ms\n    Flipped: {flip}\n    Dimensions: {imageWidth}x{imageHeight}\n    Data size: {imageData.Length} bytes\n");

            return new Texture(handle) { path = imagePath, name = Path.GetFileNameWithoutExtension(imagePath), width = imageWidth, height = imageHeight, FileContent = File.ReadAllBytes(imagePath), mode = mode, flipped = flip, timeToRead = timer.ElapsedMilliseconds, dataSize = imageData.Length };//, Data = image.Data.ToArray() };
        }

        public void Downscale(float quality)
        {
            if (quality == 1)
                return;

            int newWidth = (int)(width / quality), newHeight = (int)(height / quality);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, Handle);

            Framebuffer downscaling = GenerateFramebuffer(newWidth, newHeight);
            downscaling.Bind();

            glViewport(0, 0, newWidth, newHeight);
            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE0, this.Handle, 0);

            downscaling.shader.Use();

            glBindVertexArray(downscaling.VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            downscaling.Dispose();
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

        public void Use(ActiveTexture texture)
        {
            glActiveTexture((int)texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }

        private unsafe byte[] GetData()
        {
            this.Use(ActiveTexture.Texture0);

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
            this.Use(ActiveTexture.Texture0);

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
                Rendering.GenerateFilledBuffer(out VBO, out VAO, Rendering.GenerateQuadVerticesWithUV(x, y, (int)((float)width * (float)(150f / width)), (int)((float)height * (float)(150f / height)))); //may cause images to appear stretched or shrunk
                GenericShaders.Image2D.ActivateAttributes();
            }

            GenericShaders.Image2D.SetInt("Texture", 0);
            this.Use(ActiveTexture.Texture0);

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
} 