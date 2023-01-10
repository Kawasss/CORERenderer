using static CORERenderer.OpenGL.GL;
using CORERenderer.Main;
using COREMath;
using CORERenderer.shaders;
using StbiSharp;

namespace CORERenderer.textures
{
    public class Texture
    {
        public readonly uint Handle;
        public string path;
        public string name;

        private static unsafe Texture ReadFromColorFile(int mode, string imagePath)
        {
            uint handle = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);

            Stbi.SetFlipVerticallyOnLoad(true);

            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Couldnt find given texture at {imagePath}, using default texture");
                imagePath = $"{CORERenderContent.pathRenderer}\\textures\\placeholder.png";
            }

            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);
                fixed (byte* temp = &image.Data[0])
                {
                    IntPtr ptr = new(temp);
                    glTexImage2D(GL_TEXTURE_2D, 0, mode, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
                }

                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glGenerateMipmap(GL_TEXTURE_2D);
            }

            List<int> local = new();
            for (int i = imagePath.IndexOf("\\"); i > -1; i = imagePath.IndexOf("\\", i + 1))
                local.Add(i);

            return new Texture(handle) { path = imagePath, name = imagePath[local[^1]..] };
        }

        /// <summary>
        /// Reads the file in the RGBA color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromFile(string imagePath) => ReadFromColorFile(GL_RGBA, imagePath);

        /// <summary>
        /// Reads the file in the SRGB color format
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static unsafe Texture ReadFromSRGBFile(string imagePath) => ReadFromColorFile(GL_SRGB, imagePath);

        public Texture(uint newHandle)
        {
            Handle = newHandle;
        }

        public void Use(int texture)
        {
            glActiveTexture(texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }
    }

    public class HDRTexture
    {
        public readonly uint Handle;
        public string path;
        public string name;

        //public Framebuffer FBO;
        //public Cubemap cubemap;

        uint FBO;
        uint RBO;

        public uint envCubeMap;

        private uint cubeVAO = 0;
        private uint cubeVBO;

        private Shader shader = new($"{CORERenderContent.pathRenderer}\\shaders\\HDRCube.vert", $"{CORERenderContent.pathRenderer}\\shaders\\HDRCube.frag");

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
                //if (Stbi.IsHdrFromMemory(memoryStream))
                //{
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);

                //glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, h.Handle);

                float[] data = new float[image.Data.Length];
                for (int i = 0; i < data.Length; i++)
                    data[i] = image.Data[i];

                //&image.Data[0]//&data[0]
                fixed (byte* temp = &image.Data[0])
                {
                    IntPtr ptr = new(temp);
                    glTexImage2D(GL_TEXTURE_2D, 0, GL_SRGB, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);//GL_FLOAT
                }

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                List<int> local = new();
                for (int i = imagePath.IndexOf("\\"); i > -1; i = imagePath.IndexOf("\\", i + 1))
                    local.Add(i);
                //}
                //else
                    //throw new Exception($"Invalid .hdr file at {imagePath}");
            }

            h.envCubeMap = glGenTexture();
            glBindTexture(GL_TEXTURE_CUBE_MAP, h.envCubeMap);
            for (int i = 0; i < 6; i++)
                glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB16F, 512, 512, 0, GL_RGB, GL_FLOAT, NULL);

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