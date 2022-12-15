using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using StbImageSharp;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using Image = CORERenderer.GLFW.Structs.Image;
using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.shaders;
using CORERenderer.Main;
using CORERenderer.textures;

namespace CORERenderer
{
    public class Overrides : COREMain, EngineProperties
    {
        public unsafe void AlwaysLoad()
        {
            //creating the window
            Glfw.Init();
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

            window = Glfw.CreateWindow(Width, Height, "CORE renderer", Glfw.PrimaryMonitor, Window.None);
            if (window == null)
                Console.WriteLine("Failed to create a window");

            Console.WriteLine("Successfully created window");

            Stream stream = File.OpenRead($"{CORERenderContent.pathRenderer}\\logos\\logo4.png");

            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            Image[] images = new Image[1];
            fixed (byte* temp = &image.Data[0])
            {
                IntPtr ptr = new(temp);
                images[0] = new Image(image.Width, image.Height, ptr);
            }
            Glfw.SetWindowIcon(window, 1, images);

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);

            Globals.usedTextures.Add(Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png"));
            Globals.usedTextures.Add(Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholderspecular.png"));
        }

        public unsafe virtual void OnLoad()
        {
            throw new NotImplementedException("OnLoad() has not been implemented");
        }

        public unsafe virtual void RenderEveryFrame()
        {
            throw new NotImplementedException("RenderEveryFrame() has not been implemented");
        }

        public unsafe virtual void EveryFrame(Window window, float delta)
        {
            throw new NotImplementedException("EveryFrame() has not been implemented");
        }
    }
}
