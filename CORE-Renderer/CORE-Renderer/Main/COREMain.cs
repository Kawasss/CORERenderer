using System;
using COREMath;
using static CORERenderer.GL;
using StbImageSharp;
using CORERenderer;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using Image = CORERenderer.GLFW.Structs.Image;

namespace CORERenderer.Main
{
    public class COREMain
    {
        [NotNull]
        public static int Width = 800;
        public static int Height = 600;
        public static Window window;

        private static double time = 0;

        public unsafe static void Main(string[] args)
        {
            //creating the window
            Glfw.Init();
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

            window = Glfw.CreateWindow(Width, Height, "CORE renderer", Monitor.None, Window.None);
            if (window == null)
            {
                Console.WriteLine("Failed to create a window");
            }
            Console.WriteLine("Successfully created window");

            CORERenderContent render = new();

            Rendering.AlwaysLoad();
            render.OnLoad();

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                time += Glfw.Time - time;
                render.EveryFrame(window, (float)(time / 1000));

                Rendering.AlwaysRender();
                render.RenderEveryFrame();

                Glfw.PollEvents();
            }
            Console.WriteLine("shutting down");
            Glfw.Terminate();
        }
    }
}
