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
using System.Drawing;
using System.Reflection.Metadata;

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

        private readonly uint splashScreenTexture;

        public unsafe SplashScreen()
        {
            Glfw.WindowHint(Hint.ContextVersionMajor, 4);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Decorated, false);

            VideoMode VM = Glfw.GetVideoMode(Glfw.PrimaryMonitor);

            refreshRate = VM.RefreshRate;
            width = (int)(VM.Width * 0.27f);
            height = (int)(VM.Height * 0.27f);

            Main.COREMain.monitorWidth = VM.Width;
            Main.COREMain.monitorHeight = VM.Height;

            window = Glfw.CreateWindow(width, height, "", Monitor.None, Window.None);

            Glfw.GetWindowPosition(window, out int x, out int y);
            Glfw.SetWindowPosition(window, x + (VM.Width - width) / 2, y + (VM.Height - height) / 2);

            using (FileStream stream = File.OpenRead($"{Main.COREMain.BaseDirectory}\\logos\\logo4.png"))
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

            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glViewport(0, 0, width, height);

            shader = new($"{Main.COREMain.BaseDirectory}\\shaders\\SplashScreen.vert", $"{Main.COREMain.BaseDirectory}\\shaders\\SplashScreen.frag");

            splashScreenTexture = GenerateTexture();

            font = new(32, $"{Main.COREMain.BaseDirectory}\\Fonts\\Orbitron.ttf");

            vao = glGenVertexArray();
            glBindVertexArray(vao);

            shader.Use();
            shader.SetInt("Texture", 0);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, splashScreenTexture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            
            Glfw.SwapBuffers(window);
        }

        private static uint GenerateTexture()
        {
            Stbi.SetFlipVerticallyOnLoad(true);

            uint handle = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);

            StbiImage image = Stbi.LoadFromMemory(File.ReadAllBytes($"{COREMain.BaseDirectory}\\textures\\splashscreen.png"), 4);

            glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGBA, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, image.Data);

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

            glGenerateMipmap(GL_TEXTURE_2D);

            return handle;
        }

        public void WriteLine(string text) => WriteLine(text, new Vector3(1, 1, 1));

        public void WriteLine(string text, Vector3 color)
        {
            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);
            
            glBindVertexArray(vao);

            shader.Use();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, splashScreenTexture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);

            font.RenderText(text, 0, height - font.characterHeight, 1f, new Vector2(1, 0), color);
            
            Glfw.SwapBuffers(window);

            Glfw.MakeContextCurrent(Main.COREMain.window);
        }

        public void Refresh() => Glfw.SwapBuffers(window);

        public void Dispose()
        {
            //deletes the window and the vram it used
            glDeleteTexture(splashScreenTexture);
            glDeleteVertexArray(vao);
            Glfw.DestroyWindow(window);
            Glfw.RestoreWindow(Main.COREMain.window);
            Glfw.SetWindowMonitor(Main.COREMain.window, Glfw.PrimaryMonitor, 0, 0, Main.COREMain.monitorWidth, Main.COREMain.monitorHeight, refreshRate);
            GC.Collect();
        }
    }
}