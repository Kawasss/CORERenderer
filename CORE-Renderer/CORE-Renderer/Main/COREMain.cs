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

        private static Font debugText;

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

            debugText = new(32);

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                time2 = Glfw.Time;
                
                render.EveryFrame(window, currentFrameTime);

                render.RenderEveryFrame();
                fps = (int)(1 / currentFrameTime);
                time = Glfw.Time - time2;

                if (EngineProperties.showFPS && !EngineProperties.showFrameTime)
                    debugText.RenderText($"fps: {fps}", -1f, 0.96f, 0.001f, new Vector2(1, 0));
                if (EngineProperties.showFrameTime && !EngineProperties.showFPS)
                    debugText.RenderText($"frametime: {string.Format("{0:0.00}", currentFrameTime * 1000)} ms", -1f, 0.96f, 0.001f, new Vector2(1, 0));
                if (EngineProperties.showFPS && EngineProperties.showFrameTime)
                    debugText.RenderText($"fps: {fps}   frametime: {string.Format("{0:0.00}", currentFrameTime * 1000)} ms", -1f, 0.96f, 0.001f, new Vector2(1, 0));

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
            glViewport(0, 0, width, height / 5);
            Width = width;
            Height = height;
        }
    }
}
