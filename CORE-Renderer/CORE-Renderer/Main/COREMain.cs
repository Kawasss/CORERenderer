using System;
using GLFW;
using COREMath;
using static OpenGL.GL;

namespace openGLToturial
{
    public class COREMain
    {
        public unsafe static void Main(string[] args)
        {
            //creating the window
            Glfw.Init();
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);

            Window window = Glfw.CreateWindow(800, 600, "CORE renderer", GLFW.Monitor.None, Window.None);
            if (window == null)
            {
                Console.WriteLine("Failed to create a window");
            }
            Console.WriteLine("Successfully created window");

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);
            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);

            new CORERenderContent().OnLoad();

            CORERenderContent render = new();

            //render loop
            while (!Glfw.WindowShouldClose(window))
            {
                ProcessInput(window);

                render.RenderEveryFrame();

                Glfw.SwapBuffers(window);
                Glfw.PollEvents();
            }
            Console.WriteLine("shutting down");
            Glfw.Terminate();
        }

        static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
        }
        
        static void ProcessInput(Window window)
        {
            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }
        }
    }
}
