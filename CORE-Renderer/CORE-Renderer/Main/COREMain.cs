using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using static CORERenderer.OpenGL.GL;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using COREMath;
using CORERenderer.textures;
using CORERenderer.GLFW.Enums;
using CORERenderer.shaders;
using StbImageSharp;
using CORERenderer.GUI;

namespace CORERenderer.Main
{
    public class COREMain : EngineProperties
    {
        [NotNull]
        public static int Width = 1000;
        public static int Height = 800;
        public static Window window;

        public static SplashScreen splashScreen;

        public static int fps = 0;

        public static CORERenderContent render;
        public static Overrides overrides;

        public static RenderMode LoadFile = RenderMode.CRSFile;
        public static string LoadFilePath;

        private static double time = 0;
        private static double time2 = 0;

        private static Font debugText;

        private static bool destroyWindow = false;

        public unsafe static int Main(string[] args)
        {
            Glfw.Init();

            splashScreen = new();

            render = new();
            overrides = new();

            EnginePresets.SetPresets();
            
            if (args.Length > 0 && args[0][^4..].ToLower() == ".obj")
            {
                LoadFile = RenderMode.GivenFile;
                LoadFilePath = args[0];
            }

            overrides.AlwaysLoad();
            render.OnLoad();

            double minimumFrameTime = EPL.RunEngineLogic();
            float currentFrameTime = 0;

            Glfw.SetScrollCallback(window, render.ScrollCallback);
            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);

            debugText = new(32, $"{CORERenderContent.pathRenderer}\\Fonts\\baseFont.ttf");

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

                if (!destroyWindow)
                {
                    destroyWindow = true;
                    splashScreen.Dispose();
                }
            }
            Console.WriteLine();

            DeleteAllBuffers();

            Console.WriteLine("shutting down");
            Glfw.Terminate();

            return 0;
        }

        static private void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
            Width = width;
            Height = height;
        }

        private static void DeleteAllBuffers()
        {
            if (LoadFile == RenderMode.CRSFile)
            {
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
            }
            else if (LoadFile == RenderMode.GivenFile)
            {
                glDeleteBuffers(CORERenderContent.GivenObj.GeneratedBuffers.ToArray());
                glDeleteBuffers(CORERenderContent.GivenObj.elementBufferObject.ToArray());
                glDeleteVertexArrays(CORERenderContent.GivenObj.GeneratedVAOs.ToArray());
                glDeleteShader(CORERenderContent.GivenObj.shader.Handle);
                Console.Write($"..0");
            }
            
            Console.WriteLine();
        }
    }
}
