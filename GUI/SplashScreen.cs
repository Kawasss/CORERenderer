using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using CORERenderer.textures;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using COREMath;
using CORERenderer.Main;
using StbiSharp;
using CORERenderer.Fonts;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public class SplashScreen
    {
        public readonly int width, height;

        public readonly int refreshRate = 0;

        public readonly Window window;

        public readonly uint vao;

        private readonly Font font;

        private readonly Shader shader;

        private readonly Texture splashScreenTexture;

        public unsafe SplashScreen()
        {
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Decorated, false);

            VideoMode VM = Glfw.GetVideoMode(Glfw.PrimaryMonitor);

            refreshRate = VM.RefreshRate;
            width = (int)(VM.Width * 0.27f);
            height = (int)(VM.Height * 0.27f);

            COREMain.monitorWidth = VM.Width;
            COREMain.monitorHeight = VM.Height;

            window = Glfw.CreateWindow(width, height, "", Monitor.None, Window.None);

            Glfw.GetWindowPosition(window, out int x, out int y);
            Glfw.SetWindowPosition(window, x + (VM.Width - width) / 2, y + (VM.Height - height) / 2);

            using (FileStream stream = File.OpenRead($"{COREMain.pathRenderer}\\logos\\logo4.png"))
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

            glEnable(GL_TEXTURE_2D);
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glViewport(0, 0, width, height);

            shader = new($"{COREMain.pathRenderer}\\shaders\\SplashScreen.vert", $"{COREMain.pathRenderer}\\shaders\\SplashScreen.frag");

            splashScreenTexture = Texture.ReadFromFile($"{COREMain.pathRenderer}\\textures\\splashscreen.png");

            font = new(32, $"{COREMain.pathRenderer}\\Fonts\\Orbitron.ttf");

            vao = glGenVertexArray();
            glBindVertexArray(vao);

            shader.Use();
            shader.SetInt("Texture", GL_TEXTURE0);

            splashScreenTexture.Use(GL_TEXTURE0);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            Glfw.SwapBuffers(window);
        }


        public void WriteLine(string text) => WriteLine(text, new Vector3(1, 1, 1));

        public void WriteLine(string text, Vector3 color)
        {
            Glfw.MakeContextCurrent(window);

            glBindVertexArray(vao);

            shader.Use();

            splashScreenTexture.Use(GL_TEXTURE0);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);

            font.RenderText(text, 0, -84, 0.75f, new Vector2(1, 0), color);

            Glfw.SwapBuffers(window);

            Glfw.MakeContextCurrent(COREMain.window);
        }

        public void Refresh() => Glfw.SwapBuffers(window);

        public void Dispose()
        {
            //deletes the window and the vram it used
            glDeleteTexture(splashScreenTexture.Handle);
            glDeleteVertexArray(vao);
            Glfw.DestroyWindow(window);
            Glfw.RestoreWindow(COREMain.window);
            Glfw.SetWindowMonitor(COREMain.window, Glfw.PrimaryMonitor, 0, 0, COREMain.monitorWidth, COREMain.monitorHeight, refreshRate);
            GC.Collect();
        }
    }
}