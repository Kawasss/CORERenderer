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
            { 47, '/' }, { 59, ';' }, { 61, '=' }, { 65, 'a' }, { 66, 'b' }, 
            { 67, 'c' }, { 68, 'd' }, { 69, 'e' }, { 70, 'f' }, { 71, 'g' }, 
            { 72, 'h' }, { 73, 'i' }, { 74, 'j' }, { 75, 'k' }, { 76, 'l' },
            { 77, 'm' }, { 78, 'n' }, { 79, 'o' }, { 80, 'p' }, { 81, 'q' },
            { 82, 'r' }, { 83, 's' }, { 84, 't' }, { 85, 'u' }, { 86, 'v' },
            { 87, 'w' }, { 88, 'x' }, { 89, 'y' }, { 90, 'z' }, { 48, '0' },
            { 49, '1' }, { 50, '2' }, { 51, '3' }, { 52, '4' }, { 53, '5' },
            { 54, '6' }, { 55, '7' }, { 56, '8' }, { 57, '9' }, { 91, '[' },
            { 93, ']' }, { 92, '\\' }
        };

        public readonly static Dictionary<int, char> keyShiftCharBinding = new()
        {
            { 32, ' ' }, { 39, '"' }, { 44, '<' }, { 45, '_' }, { 46, '>' },
            { 47, '?' }, { 59, ':' }, { 61, '+' }, { 65, 'A' }, { 66, 'B' },
            { 67, 'C' }, { 68, 'D' }, { 69, 'E' }, { 70, 'F' }, { 71, 'G' },
            { 72, 'H' }, { 73, 'I' }, { 74, 'J' }, { 75, 'K' }, { 76, 'L' },
            { 77, 'M' }, { 78, 'N' }, { 79, 'O' }, { 80, 'P' }, { 81, 'Q' },
            { 82, 'R' }, { 83, 'S' }, { 84, 'T' }, { 85, 'U' }, { 86, 'V' },
            { 87, 'W' }, { 88, 'X' }, { 89, 'Y' }, { 90, 'Z' }, { 48, ')' },
            { 49, '!' }, { 50, '@' }, { 51, '#' }, { 52, '$' }, { 53, '%' },
            { 54, '^' }, { 55, '&' }, { 56, '*' }, { 57, '(' }, { 91, '{' },
            { 93, '}' }, { 92, '|' }
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
        /// Formats a given amount of bytes in KB, MB or GB
        /// </summary>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        public static string FormatSize(int sizeInBytes)
        {
            const int kilobyte = 1024;
            const int megabyte = kilobyte * 1024;
            const int gigabyte = megabyte * 1024;
            string unit;
            double size;

            if (sizeInBytes >= gigabyte)
            {
                size = (double)sizeInBytes / gigabyte;
                unit = "GB";
            }
            else if (sizeInBytes >= megabyte)
            {
                size = (double)sizeInBytes / megabyte;
                unit = "MB";
            }
            else if (sizeInBytes >= kilobyte)
            {
                size = (double)sizeInBytes / kilobyte;
                unit = "KB";
            }
            else
            {
                size = sizeInBytes;
                unit = "bytes";
            }

            return $"{size:0.#} {unit}";
        }

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
                {
                    if (i > 3)
                        COREMain.console.WriteLine($"Reusing texture {usedTextures[i].name} ({i})");
                    else
                        COREMain.console.WriteLine($"Using default texture {usedTextures[i].name} ({i})");
                    return i;
                }
            
            usedTextures.Add(Texture.ReadFromFile(path));
            COREMain.console.WriteLine($"Allocated {FormatSize(usedTextures[^1].width * usedTextures[^1].height * 4)} of VRAM for texture {usedTextures[^1].name} ({usedTextures.Count - 1})");
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

        public static Framebuffer GenerateFramebuffer(int width, int height) => GenerateFramebuffer(0, 0, width, height);

        public static Framebuffer GenerateFramebuffer(int x, int y, int width, int height)
        {
            float[] FrameBufferVertices = new float[]
            {
                (-width / 2f + x) / width, (-height / 2f + y + height) / height, 0, 1,
                (-width / 2f + x) / width, (-height / 2f + y) / height,           0, 0,
                (-width / 2f + x + width) / width, (-height / 2f + y) / height,           1, 0,

                (-width / 2f + x) / width, (-height / 2f + y + height) / height, 0, 1,
                (-width / 2f + x + width) / width, (-height / 2f + y) / height,           1, 0,
                (-width / 2f + x + width) / width, (-height / 2f + y + height) / height, 1, 1
            };
            FrameBufferVertices[0] *= 2; //ugly
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

            
                glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
                glBindTexture(GL_TEXTURE_2D, 0);

                fb.RBO = glGenRenderbuffer();
                glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

                glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
                glBindRenderbuffer(GL_RENDERBUFFER, 0);

                glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);

            
                fb.VBO = glGenBuffer();
                glBindBuffer(BufferTarget.ArrayBuffer, fb.VBO);

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
            glBindBuffer(BufferTarget.ArrayBuffer, 0);
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
                for (int j = 0; j < COREMain.CurrentScene.models.Count; j++)
                    if (Object.Materials[i].Texture == COREMain.CurrentScene.models[i].Materials[i].Texture)
                        delete = false;
                if (delete)
                    glDeleteTexture(usedTextures[Object.Materials[i].Texture].Handle);

                delete = true;
                for (int j = 0; j < COREMain.CurrentScene.models.Count; j++)
                    if (Object.Materials[i].DiffuseMap == COREMain.CurrentScene.models[j].Materials[i].DiffuseMap)
                        delete = false;
                if (delete)
                    glDeleteTexture(usedTextures[Object.Materials[i].DiffuseMap].Handle);

                delete = true;
                for (int j = 0; j < COREMain.CurrentScene.models.Count; j++)
                    if (Object.Materials[i].SpecularMap == COREMain.CurrentScene.models[j].Materials[i].SpecularMap)
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