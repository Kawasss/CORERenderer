using CORERenderer.textures;
using CORERenderer.shaders;
using CORERenderer.Loaders;
using COREMath;
using CORERenderer;
using static CORERenderer.OpenGL.GL;
using System.Runtime.Serialization.Formatters;
using StbImageSharp;
using System.Runtime.CompilerServices;

namespace CORERenderer.Main
{
    /// <summary>
    /// Class for global variables and general methods
    /// </summary>
    public static class Globals
    {
        /// <summary>
        /// All loaded textures, 0 and 1 are always used for the default diffuse and specular texture respectively
        /// </summary>
        public static List<Texture> usedTextures = new();

        /// <summary>
        /// Gets the index of a texture in the global usedTextures, reusing textures saves on ram
        /// </summary>
        /// <param name="path"></param>
        /// <returns>returns index of a texture if its already being used, otherwise adds texture and returns its position</returns>
        public static int FindTexture(string path)
        {
            for (int i = 0; i < usedTextures.Count; i++)
                if (usedTextures[i].path == path)
                    return i;
            
            usedTextures.Add(Texture.ReadFromFile(path));
            return usedTextures.Count - 1;
        }

        /// <summary>
        /// Creates a basic framebuffer with the default resolution
        /// </summary>
        /// <returns></returns>
        public unsafe static Framebuffer GenerateFramebuffer()
        {
            float[] FrameBufferVertices = new float[]
            {
                -1,  1,  0,  1,
                -1, -1,  0,  0,
                 1, -1,  1,  0,

                -1,  1,  0,  1,
                 1, -1,  1,  0,
                 1,  1,  1,  1
            };

            Framebuffer fb = new();

            fb.shader = new($"{CORERenderContent.pathRenderer}\\shaders\\FrameBuffer.vert", $"{CORERenderContent.pathRenderer}\\shaders\\FrameBuffer.frag");

            fb.FBO = glGenFramebuffer();
            glBindFramebuffer(GL_FRAMEBUFFER, fb.FBO);

            fb.Texture = glGenTexture();
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, fb.Texture);

            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, COREMain.Width, COREMain.Height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);

            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
            glBindTexture(GL_TEXTURE_2D, 0);

            fb.RBO = glGenRenderbuffer();
            glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

            glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, COREMain.Width, COREMain.Height);
            glBindRenderbuffer(GL_RENDERBUFFER, 0);

            glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);

            {
                fb.VBO = glGenBuffer();
                glBindBuffer(GL_ARRAY_BUFFER, fb.VBO);

                fixed (float* temp = &FrameBufferVertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, FrameBufferVertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                }

                fb.VAO = glGenVertexArray();
                glBindVertexArray(fb.VAO);

                int vertexLocation = fb.shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                vertexLocation = fb.shader.GetAttribLocation("aTexCoords");
                glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation);

                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);
            }

            return fb;
        }

        /// <summary>
        /// Deletes all unused textures from a given object
        /// </summary>
        /// <param name="Object"></param>
        public static void DeleteUnusedTextures(Obj Object)
        {
            bool delete;
            for (int i = 0; i < Object.Materials.Count; i++)
            {
                delete = true;
                for (int j = 0; j < CORERenderContent.givenCRS.allOBJs.Count; j++)
                    if (Object.Materials[i].Texture == CORERenderContent.givenCRS.allOBJs[j].Materials[i].Texture)
                        delete = false;
                if (delete)
                {
                    File.Delete(usedTextures[Object.Materials[i].Texture].path);
                    glDeleteTexture(usedTextures[Object.Materials[i].Texture].Handle);
                }

                delete = true;
                for (int j = 0; j < CORERenderContent.givenCRS.allOBJs.Count; j++)
                    if (Object.Materials[i].DiffuseMap == CORERenderContent.givenCRS.allOBJs[j].Materials[i].DiffuseMap)
                        delete = false;
                if (delete)
                {
                    File.Delete(usedTextures[Object.Materials[i].DiffuseMap].path);
                    glDeleteTexture(usedTextures[Object.Materials[i].DiffuseMap].Handle);
                }

                delete = true;
                for (int j = 0; j < CORERenderContent.givenCRS.allOBJs.Count; j++)
                    if (Object.Materials[i].SpecularMap == CORERenderContent.givenCRS.allOBJs[j].Materials[i].SpecularMap)
                        delete = false;
                if (delete)
                {
                    File.Delete(usedTextures[Object.Materials[i].SpecularMap].path);
                    glDeleteTexture(usedTextures[Object.Materials[i].SpecularMap].Handle);
                }
            }
        }

        

        public static unsafe Cubemap GenerateCubemap(string[] faces)
        {
            uint cubemapID = glGenTexture();
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, cubemapID);

            StbImage.stbi_set_flip_vertically_on_load(1);

            for (int i = 0; i < faces.Length; i++)
            {
                if (!File.Exists(faces[i]))
                    throw new Exception($"cubemap failed to load at: {faces[i]}"); 

                ImageResult image = ImageResult.FromStream(File.OpenRead(faces[i]), ColorComponents.RedGreenBlue);
                fixed (byte* temp = &image.Data[0])
                {
                    IntPtr ptr = new(temp);
                    glTexImage2D(GL_TEXTURE_CUBE_MAP_POSITIVE_X + i, 0, GL_RGB, image.Width, image.Height, 0, GL_RGB, GL_UNSIGNED_BYTE, ptr);
                }
            }
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_CUBE_MAP, GL_TEXTURE_WRAP_R, GL_CLAMP_TO_EDGE);

            Shader cubemapShader = new($"{CORERenderContent.pathRenderer}\\shaders\\cubemap.vert", $"{CORERenderContent.pathRenderer}\\shaders\\cubemap.frag");
            cubemapShader.SetInt("cubemap", GL_TEXTURE0);

            uint cubemapVAO = glGenVertexArray();
            glBindVertexArray(cubemapVAO);

            return new Cubemap { textureID = cubemapID, VAO = cubemapVAO, shader = cubemapShader };
        }

        public static unsafe Cubemap GenerateCubemap(bool addShader, uint cubemapID)
        {
            uint cubemapVAO = glGenVertexArray();
            glBindVertexArray(cubemapVAO);

            if (addShader)
            {
                Shader cubemapShader = new($"{CORERenderContent.pathRenderer}\\shaders\\cubemap.vert", $"{CORERenderContent.pathRenderer}\\shaders\\cubemap.frag");
                cubemapShader.SetInt("cubemap", GL_TEXTURE0);

                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO, shader = cubemapShader };
            }

            else
                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO };
        }

        public static unsafe Cubemap GenerateSkybox(string[] faces)
        {
            Cubemap skybox = GenerateCubemap(faces);
            skybox.shader = new($"{CORERenderContent.pathRenderer}\\shaders\\skybox.vert", $"{CORERenderContent.pathRenderer}\\shaders\\skybox.frag");

            return skybox;
        }

        public static unsafe Cubemap GenerateSkybox(uint cubemapID)
        {
            Cubemap skybox = GenerateCubemap(false, cubemapID);
            skybox.shader = new($"{CORERenderContent.pathRenderer}\\shaders\\skybox.vert", $"{CORERenderContent.pathRenderer}\\shaders\\skybox.frag");
            skybox.shader.SetInt("cubemap", GL_TEXTURE0);

            return skybox;
        }
    }
}
