using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using StbiSharp;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.textures
{
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

        private Shader shader = new($"{Main.COREMain.pathRenderer}\\shaders\\HDRCube.vert", $"{Main.COREMain.pathRenderer}\\shaders\\HDRCube.frag");

        private Shader testShader = new($"{Main.COREMain.pathRenderer}\\shaders\\2DImage.vert", $"{Main.COREMain.pathRenderer}\\shaders\\2DImage.frag");

        public byte[] data;

        public static unsafe HDRTexture ReadFromFile(string imagePath, float quality)
        {
            glDisable(GL_CULL_FACE);

            Stbi.SetFlipVerticallyOnLoad(true);

            if (!File.Exists(imagePath))
            {
                throw new Exception($"Couldnt find file at {imagePath}");
            }

            HDRTexture h = new(glGenTexture());
            h.data = File.ReadAllBytes(imagePath);

            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);

                h.FBO = glGenFramebuffer();
                h.RBO = glGenRenderbuffer();

                int qualityWidth = (int)(1024 / quality);// (float)quality);
                int qualityHeight = (int)(1024 / quality);// (float)quality);

                glBindFramebuffer(GL_FRAMEBUFFER, h.FBO);
                glBindRenderbuffer(GL_RENDERBUFFER, h.RBO);
                glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH_COMPONENT24, qualityWidth, qualityHeight);
                glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, h.RBO);

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
                h.testShader.SetInt("Texture", 0);
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
            
                h.envCubeMap = glGenTexture();
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_CUBE_MAP, h.envCubeMap);
                for (int i = 0; i < 6; i++)
                    glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB16F, qualityWidth, qualityHeight, 0, GL_RGB, GL_FLOAT, null);

                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                Matrix captureProjection = Matrix.CreatePerspectiveFOV(MathC.PiF / 2, 1, 0.01f, 1000);//COREMain.Width / COREMain.Height
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

                h.shader.SetInt("equirectangularMap", 1);
                h.shader.SetMatrix("projection", captureProjection);
                glActiveTexture(GL_TEXTURE1);
                glBindTexture(GL_TEXTURE_2D, h.Handle);

                glViewport(0, 0, qualityWidth, qualityHeight);
                glBindFramebuffer(GL_FRAMEBUFFER, h.FBO);

                for (int i = 0; i < 6; i++)
                {
                    h.shader.SetMatrix("view", captureViews[i]);
                    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, h.envCubeMap, 0);
                    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                    Rendering.RenderCube();
                }

                glEnable(GL_CULL_FACE);
            }
            return h;
        }

        public void Render()
        {
            glDisable(GL_CULL_FACE);
            GenericShaders.Background.Use();
            GenericShaders.Background.SetInt("environmentMap", 0);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, envCubeMap);
            glDepthFunc(GL_LEQUAL);
            glDrawArrays(PrimitiveType.Triangles, 0, 36);
            glEnable(GL_CULL_FACE);
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
