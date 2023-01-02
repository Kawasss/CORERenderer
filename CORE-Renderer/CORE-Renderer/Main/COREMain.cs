using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using static CORERenderer.OpenGL.GL;
using COREMath;

namespace CORERenderer.Main
{
    public class COREMain : EngineProperties
    {
        [NotNull]
        public static int Width = 1000;
        public static int Height = 800;
        public static Window window;

        public static int fps = 0;

        public static CORERenderContent render;
        public static Overrides overrides;

        private static double time = 0;
        private static double time2 = 0;

        public unsafe static void Main(string[] args)
        {
            render = new();
            overrides = new();

            EnginePresets.SetPresets();

            overrides.AlwaysLoad();
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

            if (CORERenderContent.givenCRS.allOBJs.Count > 0)
                Console.Write("\nDeleting buffers");

            for (int i = 0; i < CORERenderContent.givenCRS.allOBJs.Count; i++)
            {
                glDeleteBuffers(CORERenderContent.givenCRS.allOBJs[i].GeneratedBuffers.ToArray());
                glDeleteBuffers(CORERenderContent.givenCRS.allOBJs[i].elementBufferObject.ToArray());
                glDeleteVertexArrays(CORERenderContent.givenCRS.allOBJs[i].GeneratedVAOs.ToArray());
                glDeleteShader(CORERenderContent.givenCRS.allOBJs[i].shader.Handle);
                Console.Write($"..{i}");
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
