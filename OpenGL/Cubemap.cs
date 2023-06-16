using COREMath;
using CORERenderer.Main;
using CORERenderer.shaders;
using StbiSharp;

namespace CORERenderer.OpenGL
{
    public struct Cubemap
    {
        public static Matrix[] ViewMatrices
        {
            get => new Matrix[] 
            {
                    MathC.LookAt(new(0, 0, 0), new(1, 0, 0), new(0, -1, 0)),
                    MathC.LookAt(new(0, 0, 0), new(-1, 0, 0), new(0, -1, 0)),
                    MathC.LookAt(new(0, 0, 0), new(0, 1, 0), new(0, 0, 1)),
                    MathC.LookAt(new(0, 0, 0), new(0, -1, 0), new(0, 0, -1)),
                    MathC.LookAt(new(0, 0, 0), new(0, 0, 1), new(0, -1, 0)),
                    MathC.LookAt(new(0, 0, 0), new(0, 0, -1), new(0, -1, 0)),
                };
        }

        public uint VAO = 0;
        public uint textureID = 0;
        public Shader shader = GenericShaders.Background;

        public void Use(int texture)
        {
            GL.glActiveTexture(texture);
            GL.glBindTexture(GL.GL_TEXTURE_CUBE_MAP, textureID);
        }

        public void Render()
        {
            GL.glDisable(GL.GL_CULL_FACE);
            GenericShaders.Background.Use();
            GenericShaders.Background.SetInt("environmentMap", 0);

            GL.glBindVertexArray(VAO);
            Use(GL.GL_TEXTURE0);

            Rendering.glDrawArrays(PrimitiveType.Triangles, 0, 36);

            GL.glBindVertexArray(0);
            GL.glEnable(GL.GL_CULL_FACE);
        }

        public void Dispose()
        {
            GL.glDeleteVertexArray(VAO);
            GL.glDeleteTexture(textureID);
        }

        public Cubemap() { }
    }

    public partial class Rendering
    {
        public static Cubemap GenerateEmptyCubemap(int width, int height, int textureMinFilter)
        {
            uint vao = GenerateBufferlessVAO();
            uint texture = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, texture);

            for (int i = 0; i < 6; i++)
            {
                glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);//reused from HDRTexture.cs, dont know if itll work well
            }
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, textureMinFilter);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glGenerateMipmap(GL_TEXTURE_CUBE_MAP);

            return new() { VAO = vao, textureID = texture };
        }

        public static Cubemap GenerateEmptyCubemap(int width, int height) => GenerateEmptyCubemap(width, height, GL_LINEAR_MIPMAP_LINEAR);

        public static unsafe Cubemap GenerateDepthCubemap(int width, int height)
        {
            uint vao = GenerateBufferlessVAO();
            uint handle = glGenTexture();
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, handle);

            for (int i = 0; i < 6; i++)
                glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, null);

            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);

            return new() { textureID = handle, VAO = vao };
        }

        public static unsafe Cubemap GenerateCubemap(string[] faces)
        {
            uint cubemapID = glGenTexture();
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, cubemapID);

            Stbi.SetFlipVerticallyOnLoad(true);

            for (int i = 0; i < faces.Length; i++)
            {
                if (!File.Exists(faces[i]))
                    throw new Exception($"cubemap failed to load at: {faces[i]}");

                using (FileStream stream = File.OpenRead(faces[i]))
                using (MemoryStream memoryStream = new())
                {
                    StbiImage image = Stbi.LoadFromMemory(memoryStream, 4);
                    glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB, image.Width, image.Height, 0, GL_RGB, GL_UNSIGNED_BYTE, image.Data);
                }
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);
            }
            Shader cubemapShader = GenericShaders.Cubemap;
            cubemapShader.SetInt("cubemap", 0);

            uint cubemapVAO = glGenVertexArray();
            glBindVertexArray(cubemapVAO);

            return new Cubemap { textureID = cubemapID, VAO = cubemapVAO };
        }

        public static unsafe Cubemap GenerateCubemap(bool addShader, uint cubemapID)
        {
            uint cubemapVAO = glGenVertexArray();
            glBindVertexArray(cubemapVAO);

            if (addShader)
            {
                Shader cubemapShader = GenericShaders.Cubemap;
                cubemapShader.SetInt("cubemap", 0);

                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO, shader = cubemapShader };
            }

            else
                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO };
        }

        public static unsafe Cubemap GenerateSkybox(string[] faces)
        {
            Cubemap skybox = GenerateCubemap(faces);
            skybox.shader = GenericShaders.Skybox;

            return skybox;
        }

        public static unsafe Cubemap GenerateSkybox(uint cubemapID)
        {
            Cubemap skybox = GenerateCubemap(false, cubemapID);
            skybox.shader = GenericShaders.Skybox;
            skybox.shader.SetInt("cubemap", 0);

            return skybox;
        }
    }
}
