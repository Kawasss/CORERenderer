using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using Image = CORERenderer.GLFW.Structs.Image;
using static CORERenderer.OpenGL.GL;
using StbiSharp;
using CORERenderer.Main;
using CORERenderer.textures;

namespace CORERenderer
{
    public class Overrides : Main.COREMain
    {
        public static unsafe void AlwaysLoad()
        {
            //creating the window
            if (!SetOpenGLVersion(4, 3))
            {
                GUI.Console.WriteError("Couldn't find OpenGL version 4.3, trying to find an older version now. This software will be unstable and can crash unexpectedly.");
                GUI.Console.WriteError("Trying to use OpenGL version 4.0");
                Console.WriteLine("Couldn't find OpenGL version 4.3, trying to find an older version now. This software will be unstable and can crash unexpectedly.");
                Console.WriteLine("Trying to use OpenGL version 4.0");
                if (!SetOpenGLVersion(4, 0))
                {
                    GUI.Console.WriteError("Trying to use OpenGL version 3.3");
                    Console.WriteLine("Trying to use OpenGL version 3.3");
                    if (!SetOpenGLVersion(3, 3))
                    {
                        GUI.Console.WriteError("No valid version of OpenGL can be found, this software can't be used and will crash");
                        Console.WriteLine("No valid version of OpenGL can be found, this software can't be used and will crash");
                    }
                }
            }
            
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Visible, false);
            Glfw.WindowHint(Hint.Decorated, true);

            window = Glfw.CreateWindow(monitorWidth, monitorHeight, "CORE renderer", Monitor.None, Window.None);

            using (FileStream stream = File.OpenRead($"{BaseDirectory}\\logos\\logo4.png"))
            using (MemoryStream memoryStream = new())
            {
                StbiImage image;
                stream.CopyTo(memoryStream);

                image = Stbi.LoadFromMemory(memoryStream, 4);

                Image[] images = new Image[1];
                fixed (byte* temp = &image.Data[0])
                {
                    IntPtr ptr = new(temp);
                    images[0] = new Image(image.Width, image.Height, ptr);
                }

                Glfw.SetWindowIcon(window, 1, images);
            }

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);
            Globals.usedTextures.Clear();
            Globals.usedTextures.Add(Texture.ReadFromFile($"{BaseDirectory}\\textures\\placeholder.png"));
            Globals.usedTextures.Add(Texture.ReadFromFile($"{BaseDirectory}\\textures\\white.png"));//placeholderspecular
            Globals.usedTextures.Add(Texture.ReadFromFile($"{BaseDirectory}\\textures\\white.png"));
            Globals.usedTextures.Add(Texture.ReadFromFile($"{BaseDirectory}\\textures\\normal2_1.png"));//$"{pathRenderer}\\textures\\normal2_1.png"
            Globals.usedTextures.Add(Texture.ReadFromFile($"{BaseDirectory}\\textures\\black.png"));//$"{pathRenderer}\\textures\\black.png"
        }

        private static bool SetOpenGLVersion(int major, int minor)
        {
            try
            {
                Glfw.WindowHint(Hint.ContextVersionMajor, major);
                Glfw.WindowHint(Hint.ContextVersionMinor, minor);
                return true;
            }
            catch (GLFW.Exception)
            {
                GUI.Console.WriteError($"Couldn't get OpenGL version {major}.{minor}, returning without setting an OpenGL version");
                return false;
            }
        }

        /// <summary>
        /// Load in everything for RenderEveryFrame to work, called when the renderer is initializing
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void OnLoad(string[] args)
        {
            throw new NotImplementedException("OnLoad() has not been implemented");
        }

        /// <summary>
        /// Override for anything rendering related, called every frame
        /// </summary>
        /// <param name="delta"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void RenderEveryFrame(float delta)
        {
            throw new NotImplementedException("RenderEveryFrame() has not been implemented");
        }

        /// <summary>
        /// Override for updating anything not related to rendering, called every frame before RenderEveryFrame()
        /// </summary>
        /// <param name="window"></param>
        /// <param name="delta"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void EveryFrame(Window window, float delta)
        {
            throw new NotImplementedException("EveryFrame() has not been implemented");
        }
    }
}