using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using static CORERenderer.GL;

namespace CORERenderer.Main
{
    public class COREMain
    {
        [NotNull]
        public static int Width = 800;
        public static int Height = 600;
        public static Window window;

        public static int fps = 0;

        private static double time = 0;
        private static double time2 = 0;

        public unsafe static void Main(string[] args)
        {
            CORERenderContent render = new();
            Rendering basic = new();

            basic.AlwaysLoad();
            render.OnLoad();

            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                time2 = Glfw.Time;
                Console.Write($"\rfps: {fps}         \n");

                render.EveryFrame(window, (float)(time / 1000));

                //basic.AlwaysRender();
                //render.RenderEveryFrame();
                render.Render();
                time = Glfw.Time - time2;
                fps = (int)(1 / time);

                Console.CursorTop = 0;
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();
            }
            Console.WriteLine("shutting down");
            Glfw.Terminate();
        }

        static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
            Width = width;
            Height = height;
        }

    }
}
