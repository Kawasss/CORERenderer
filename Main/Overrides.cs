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
    public class Overrides : COREMain
    {
        public static unsafe void AlwaysLoad()
        {
            //creating the window
            Glfw.WindowHint(Hint.ContextVersionMajor, 4);
            Glfw.WindowHint(Hint.ContextVersionMinor, 6);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Visible, false);
            Glfw.WindowHint(Hint.Decorated, true);

            window = Glfw.CreateWindow(monitorWidth, monitorHeight, "CORE renderer", Monitor.None, Window.None);

            Console.WriteLine("Successfully created window");

            using (FileStream stream = File.OpenRead($"{pathRenderer}\\logos\\logo4.png"))
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

            Globals.usedTextures.Add(Texture.ReadFromFile($"{pathRenderer}\\textures\\placeholder.png"));
            Globals.usedTextures.Add(Texture.ReadFromFile($"{pathRenderer}\\textures\\placeholderspecular.png"));
            Globals.usedTextures.Add(Texture.ReadFromFile($"{pathRenderer}\\textures\\white.png"));
            Globals.usedTextures.Add(Texture.ReadFromSRGBFile($"{pathRenderer}\\textures\\normal.png"));
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