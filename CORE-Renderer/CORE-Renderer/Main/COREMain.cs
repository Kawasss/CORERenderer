using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.Main
{
    public class COREMain : EngineProperties
    {
        [NotNull]
        public static int Width = 2560; //1024
        public static int Height = 1440; //576
        public static Window window;

        public static int fps = 0;

        public static CORERenderContent render;
        public static Rendering basic;

        private static double time = 0;
        private static double time2 = 0;

        public unsafe static void Main(string[] args)
        {
            render = new();
            basic = new();

            EnginePresets.SetPresets();

            basic.AlwaysLoad();
            render.OnLoad();

            double minimumFrameTime = EPL.RunEngineLogic();
            float currentFrameTime = 0;

            Glfw.SetScrollCallback(window, render.ScrollCallback);
            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                time2 = Glfw.Time;
                if (EngineProperties.showFPS && !EngineProperties.showFrameTime)
                    Console.Write($"\rfps: {fps}         ");
                if (EngineProperties.showFrameTime && !EngineProperties.showFPS)
                    Console.Write($"\rframetime: {Math.Round(currentFrameTime * 1000, 3)}");
                if (EngineProperties.showFPS && EngineProperties.showFrameTime)
                    Console.Write($"\rfps: {fps}   frametime: {Math.Round(currentFrameTime * 1000, 3)}                  ");

                render.EveryFrame(window, currentFrameTime);

                render.RenderEveryFrame();
                render.AlwaysRender();

                fps = (int)(1 / currentFrameTime);
                time = Glfw.Time - time2;
                
                currentFrameTime = 0;
                while (currentFrameTime < minimumFrameTime)
                    currentFrameTime += (float)time;

                //Console.CursorTop = 0;
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();
            }
            Console.WriteLine(); 
            Console.WriteLine("shutting down");
            Glfw.Terminate();
        }

        static private void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
            Width = width;
            Height = height;
        }
    }
}
