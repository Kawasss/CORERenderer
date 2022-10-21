using System;
using System.Windows;
using System.Drawing;
using GLFW;
using COREMath;
using static OpenGL.GL;
using JetBrains.Annotations;
using StbImageSharp;
using Image = System.Drawing.Image;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using CORE_Renderer;

namespace openGLToturial
{
    public class COREMain
    {
        public const int WIDTH = 800;
        public const int HEIGHT = 600;
        public static Window window;

        private static double time = 0;

        public unsafe static void Main(string[] args)
        {
            //creating the window
            Glfw.Init();
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

            window = Glfw.CreateWindow(WIDTH, HEIGHT, "CORE renderer", GLFW.Monitor.None, Window.None);
            if (window == null)
            {
                Console.WriteLine("Failed to create a window");
            }
            Console.WriteLine("Successfully created window");

            Stream stream = File.OpenRead($"{CORERenderContent.pathRenderer}\\logos\\logo4.png");

            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            GLFW.Image[] images = new GLFW.Image[1];
            fixed (byte* temp = &image.Data[0])
            {
                IntPtr ptr = new(temp);
                images[0] = new GLFW.Image(image.Width, image.Height, ptr);
            }
            Glfw.SetWindowIcon(window, 1, images);

            Glfw.SetScrollCallback(window, CORERenderContent.ScrollCallback);

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);
            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);

            new CORERenderContent().OnLoad();

            CORERenderContent render = new();

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                time += Glfw.Time - time;
                render.EveryFrame(window, (float)(time / 1000));
                
                render.RenderEveryFrame();
                
                Glfw.PollEvents();
            }
            Console.WriteLine("shutting down");
            Glfw.Terminate();
        }

        static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
        }
    }
}
