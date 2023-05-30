using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using StbiSharp;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.textures
{
    public class Skybox
    {
        public readonly uint Handle;
        public string path;
        public string name;

        public Cubemap envCubeMap;
        public Cubemap irradianceMap;

        private uint VAO2D;
        private uint VBO2D;

        public int width2D;
        public int height2D;

        private Shader shader = new($"{COREMain.BaseDirectory}\\shaders\\HDRCube.vert", $"{COREMain.BaseDirectory}\\shaders\\HDRCube.frag");

        private Shader testShader = new($"{COREMain.BaseDirectory}\\shaders\\2DImage.vert", $"{COREMain.BaseDirectory}\\shaders\\2DImage.frag");

        public byte[] data;

        public static unsafe Skybox ReadFromFile(string imagePath, float quality)
        {
            glDisable(GL_CULL_FACE);

            Stbi.SetFlipVerticallyOnLoad(true);

            if (!File.Exists(imagePath))
            {
                throw new Exception($"Couldnt find file at {imagePath}");
            }

            Skybox h = new(glGenTexture());
            h.data = File.ReadAllBytes(imagePath);

            using (FileStream stream = File.OpenRead(imagePath))
            using (MemoryStream memoryStream = new())
            {
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);

                int qualityWidth = (int)(1024 / quality);
                int qualityHeight = (int)(1024 / quality);

                Framebuffer renderToCubemap = GenerateFramebuffer(qualityWidth, qualityHeight);

                h.width2D = image.Width;
                h.height2D = image.Height;

                //for optimal coordinates height is relative to the width of the image
                float height = (float)image.Height / (float)image.Width;
                float[] vertices = GenerateQuadVerticesWithUV(-1, -1, 1, height);

                GenerateFilledBuffer(out h.VBO2D, out h.VAO2D, vertices);

                glBindVertexArray(h.VAO2D);

                h.testShader.ActivateAttributes();
                h.testShader.Use();
                h.testShader.SetInt("Texture", 0);
                h.testShader.SetMatrix("projection", GetOrthograpicProjectionMatrix(COREMain.Width, COREMain.Height));

                glBindBuffer(BufferTarget.ArrayBuffer, 0);
                glBindVertexArray(0);

                glBindTexture(GL_TEXTURE_2D, h.Handle);

                glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGB, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, image.Data);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                h.envCubeMap = GenerateEmptyCubemap(qualityWidth, qualityHeight, GL_LINEAR);

                Matrix captureProjection = Matrix.CreatePerspectiveFOV(MathC.PiF / 2, 1, 0.01f, 1000);
                Matrix[] captureViews = Cubemap.ViewMatrices;

                h.shader.Use();

                h.shader.SetInt("equirectangularMap", 1);
                h.shader.SetMatrix("projection", captureProjection);
                glActiveTexture(GL_TEXTURE1);
                glBindTexture(GL_TEXTURE_2D, h.Handle);

                glViewport(0, 0, qualityWidth, qualityHeight);
                renderToCubemap.Bind();

                for (int i = 0; i < 6; i++)
                {
                    h.shader.SetMatrix("view", captureViews[i]);
                    glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, h.envCubeMap.textureID, 0);
                    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                    Rendering.RenderCube();
                }
                renderToCubemap.Dispose();

                h.irradianceMap = GenerateIrradianceMap(h.envCubeMap, captureProjection, captureViews);

                glEnable(GL_CULL_FACE);
            }
            return h;
        }

        private static Cubemap GenerateIrradianceMap(Cubemap originalCubemap, Matrix projection, Matrix[] views)
        {
            int width = 1280, height = 1280;
            Cubemap irradianceMap = GenerateEmptyCubemap(width, height, GL_LINEAR); //low res textures because irradiance isnt meant to have detail
            Framebuffer renderToMap = GenerateFramebuffer(width, height);
            Shader irradianceConverter = new($"{COREMain.BaseDirectory}\\shaders\\HDRCube.vert", $"{COREMain.BaseDirectory}\\shaders\\irradiance.frag");

            irradianceConverter.Use();

            irradianceConverter.SetInt("environmentMap", 1);
            irradianceConverter.SetMatrix("projection", projection);

            originalCubemap.Use(GL_TEXTURE1);
            irradianceMap.Use(GL_TEXTURE0);

            glViewport(0, 0, width, height);
            renderToMap.Bind();

            for (int i = 0; i < 6; i++)
            {
                irradianceConverter.SetMatrix("view", views[i]);
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, irradianceMap.textureID, 0);
                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                RenderCube();
            }

            irradianceConverter.Dispose();
            renderToMap.Dispose();
            return irradianceMap;
        }

        public void Render()
        {
            glDisable(GL_CULL_FACE);
            GenericShaders.Background.Use();
            GenericShaders.Background.SetInt("environmentMap", 0);

            envCubeMap.Use(GL_TEXTURE0);

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

        public Skybox(uint newHandle)
        {
            Handle = newHandle;
        }

        public void Use(int texture)
        {
            glActiveTexture(texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }

        public void Dispose()
        {
            envCubeMap.Dispose();
            irradianceMap.Dispose();
            glDeleteBuffer(VBO2D);
            glDeleteVertexArray(VAO2D);
            glDeleteTexture(Handle);
            shader.Dispose();
            testShader.Dispose();
        }
    }
}
