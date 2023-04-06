using CORERenderer.textures;
using CORERenderer.shaders;
using CORERenderer.Loaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using StbiSharp;
using CORERenderer.OpenGL;
using System.Runtime.CompilerServices;

namespace CORERenderer.Main
{
    /// <summary>
    /// Class for global variables and general methods
    /// </summary>
    public static class Globals
    {
        public readonly static Dictionary<int, char> keyCharBinding = new() 
        { 
            { 32, ' ' }, { 39, '\'' }, { 44, ',' }, { 45, '-' }, { 46, '.' }, 
            { 47, '/' }, { 59, ':' }, { 61, '=' }, { 65, 'a' }, { 66, 'b' }, 
            { 67, 'c' }, { 68, 'd' }, { 69, 'e' }, { 70, 'f' }, { 71, 'g' }, 
            { 72, 'h' }, { 73, 'i' }, { 74, 'j' }, { 75, 'k' }, { 76, 'l' },
            { 77, 'm' }, { 78, 'n' }, { 79, 'o' }, { 80, 'p' }, { 81, 'q' },
            { 82, 'r' }, { 83, 's' }, { 84, 't' }, { 85, 'u' }, { 86, 'v' },
            { 87, 'w' }, { 88, 'x' }, { 89, 'y' }, { 90, 'z' }, { 48, '0' },
            { 49, '1' }, { 50, '2' }, { 51, '3' }, { 52, '4' }, { 53, '5' },
            { 54, '6' }, { 55, '7' }, { 56, '8' }, { 57, '9' }, { 91, '[' },
            { 93, ']' }, { 92, '\\' }
        };

        /// <summary>
        /// Look up table for all supported render modes, needs to . at the beginning to work
        /// </summary>
        public readonly static Dictionary<string, RenderMode> RenderModeLookUpTable = new()
    {
        {".crs", RenderMode.CRSFile },
        {".png", RenderMode.PNGImage},
        {".jpg", RenderMode.JPGImage},
        {".hdr", RenderMode.HDRFile },
        {".stl", RenderMode.STLFile },
        {".obj", RenderMode.ObjFile }
    };

        /// <summary>
        /// All loaded textures, 0 and 1 are always used for the default diffuse and specular texture respectively. The third is used for solid white and the fourth for the normal map
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

        public static int FindSRGBTexture(string path)
        {
            for (int i = 0; i < usedTextures.Count; i++)
                if (usedTextures[i].path == path)
                    return i;

            usedTextures.Add(Texture.ReadFromSRGBFile(path));
            return usedTextures.Count - 1;
        }

        /// <summary>
        /// Creates a basic framebuffer with the default resolution
        /// </summary>
        /// <returns></returns>
        /*public unsafe static Framebuffer GenerateFramebuffer()
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

            fb.shader = new($"{COREMain.pathRenderer}\\shaders\\FrameBuffer.vert", $"{COREMain.pathRenderer}\\shaders\\FrameBuffer.frag");

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
        }*/
        public static Framebuffer GenerateFramebuffer(int width, int height) => GenerateFramebuffer(0, 0, width, height);

        public static Framebuffer GenerateFramebuffer(int x, int y, int width, int height)
        {
            float[] FrameBufferVertices = new float[]
            {
                (-COREMain.monitorWidth / 2f + x) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y + height) / COREMain.monitorHeight, 0, 1,
                (-COREMain.monitorWidth / 2f + x) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y) / COREMain.monitorHeight,           0, 0,
                (-COREMain.monitorWidth / 2f + x + width) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y) / COREMain.monitorHeight,           1, 0,

                (-COREMain.monitorWidth / 2f + x) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y + height) / COREMain.monitorHeight, 0, 1,
                (-COREMain.monitorWidth / 2f + x + width) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y) / COREMain.monitorHeight,           1, 0,
                (-COREMain.monitorWidth / 2f + x + width) / COREMain.monitorWidth, (-COREMain.monitorHeight / 2f + y + height) / COREMain.monitorHeight, 1, 1
            };
            FrameBufferVertices[0] *= 2;
            FrameBufferVertices[1] *= 2;
            FrameBufferVertices[4] *= 2;
            FrameBufferVertices[5] *= 2;
            FrameBufferVertices[8] *= 2;
            FrameBufferVertices[9] *= 2;

            FrameBufferVertices[12] *= 2;
            FrameBufferVertices[13] *= 2;
            FrameBufferVertices[16] *= 2;
            FrameBufferVertices[17] *= 2;
            FrameBufferVertices[20] *= 2;
            FrameBufferVertices[21] *= 2;

            Framebuffer fb = new();
            unsafe
            {
                fb.width = width;
                fb.height = height;

                fb.shader = GenericShaders.Framebuffer;//new($"{COREMain.pathRenderer}\\shaders\\FrameBuffer.vert", $"{COREMain.pathRenderer}\\shaders\\FrameBuffer.frag");
            
                fb.FBO = glGenFramebuffer();
                glBindFramebuffer(GL_FRAMEBUFFER, fb.FBO);

                fb.Texture = glGenTexture();
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, fb.Texture);

            
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);

                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
                glBindTexture(GL_TEXTURE_2D, 0);

                fb.RBO = glGenRenderbuffer();
                glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

                glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
                glBindRenderbuffer(GL_RENDERBUFFER, 0);

                glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);

            
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
            }
            glBindBuffer(GL_ARRAY_BUFFER, 0);
            glBindVertexArray(0);

            return fb;
        }

        /// <summary>
        /// Deletes all unused textures from a given object
        /// </summary>
        /// <param name="Object"></param>
        public static void DeleteUnusedTextures(Model Object)
        {
            bool delete;
            for (int i = 0; i < Object.Materials.Count; i++)
            {
                delete = true;
                for (int j = 0; j < COREMain.scenes[COREMain.selectedScene].allModels.Count; j++)
                    if (Object.Materials[i].Texture == COREMain.scenes[COREMain.selectedScene].allModels[i].Materials[i].Texture)
                        delete = false;
                if (delete)
                    glDeleteTexture(usedTextures[Object.Materials[i].Texture].Handle);

                delete = true;
                for (int j = 0; j < COREMain.scenes[COREMain.selectedScene].allModels.Count; j++)
                    if (Object.Materials[i].DiffuseMap == COREMain.scenes[COREMain.selectedScene].allModels[j].Materials[i].DiffuseMap)
                        delete = false;
                if (delete)
                    glDeleteTexture(usedTextures[Object.Materials[i].DiffuseMap].Handle);

                delete = true;
                for (int j = 0; j < COREMain.scenes[COREMain.selectedScene].allModels.Count; j++)
                    if (Object.Materials[i].SpecularMap == COREMain.scenes[COREMain.selectedScene].allModels[j].Materials[i].SpecularMap)
                        delete = false;
                if (delete)
                    glDeleteTexture(usedTextures[Object.Materials[i].SpecularMap].Handle);
            }
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
            Shader cubemapShader = new($"{COREMain.pathRenderer}\\shaders\\cubemap.vert", $"{COREMain.pathRenderer}\\shaders\\cubemap.frag");
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
                Shader cubemapShader = new($"{COREMain.pathRenderer}\\shaders\\cubemap.vert", $"{COREMain.pathRenderer}\\shaders\\cubemap.frag");
                cubemapShader.SetInt("cubemap", GL_TEXTURE0);

                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO, shader = cubemapShader };
            }

            else
                return new Cubemap { textureID = cubemapID, VAO = cubemapVAO };
        }

        public static unsafe Cubemap GenerateSkybox(string[] faces)
        {
            Cubemap skybox = GenerateCubemap(faces);
            skybox.shader = new($"{COREMain.pathRenderer}\\shaders\\skybox.vert", $"{COREMain.pathRenderer}\\shaders\\skybox.frag");

            return skybox;
        }

        public static unsafe Cubemap GenerateSkybox(uint cubemapID)
        {
            Cubemap skybox = GenerateCubemap(false, cubemapID);
            skybox.shader = new($"{COREMain.pathRenderer}\\shaders\\skybox.vert", $"{COREMain.pathRenderer}\\shaders\\skybox.frag");
            skybox.shader.SetInt("cubemap", GL_TEXTURE0);

            return skybox;
        }
    }
}