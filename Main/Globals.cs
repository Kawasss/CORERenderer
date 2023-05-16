using CORERenderer.textures;
using CORERenderer.Loaders;
using static CORERenderer.OpenGL.GL;
using Console = CORERenderer.GUI.Console;
using CORERenderer.OpenGL;

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
            {".obj", RenderMode.ObjFile },
            {".fbx", RenderMode.FBXFile },
            {".exr", RenderMode.EXRFile }
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
        /// All loaded textures, 0 and 1 are always used for the default diffuse and specular texture respectively. 3 is used for solid white and 4 for the normal map, 5 is a black texture. this used for the metal map
        /// </summary>
        public static List<Texture> usedTextures = new();

        /// <summary>
        /// Gets the index of a texture in the global usedTextures, reusing textures saves on ram
        /// </summary>
        /// <param name="path"></param>
        /// <returns>returns index of a texture if its already being used, otherwise adds texture and returns its position</returns>
        public static int FindTexture(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteError($"File at {path} not found, returning default texture.");
                return 0;
            }

            for (int i = 0; i < usedTextures.Count; i++)
                if (usedTextures[i].path == path)
                {
                    if (i > 4)
                        Console.WriteLine($"Reusing texture {usedTextures[i].name} ({i})");
                    else
                        Console.WriteLine($"Using default texture {usedTextures[i].name} ({i})");
                    return i;
                }
            
            usedTextures.Add(Texture.ReadFromFile(path));
            Console.WriteLine($"Allocated {FormatSize(usedTextures[^1].width * usedTextures[^1].height * 4)} of VRAM for texture {usedTextures[^1].name} ({usedTextures.Count - 1})");
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
    }
}