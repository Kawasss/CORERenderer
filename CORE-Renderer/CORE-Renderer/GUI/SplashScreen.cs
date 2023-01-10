using static CORERenderer.OpenGL.GL;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using CORERenderer.textures;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using StbImageSharp;
using CORERenderer;
using COREMath;
using CORERenderer.Main;

namespace CORERenderer.GUI
{
    public class SplashScreen
    {
        public readonly int width;
        public readonly int height;

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

            width = 700;//(int)(VM.Width * 0.3f);
            height = 400;//(int)(VM.Height * 0.3f);

            window = Glfw.CreateWindow(width, height, "", Monitor.None, Window.None);

            Glfw.GetWindowPosition(window, out int x, out int y);
            Glfw.SetWindowPosition(window, x + (VM.Width - width) / 2, y + (VM.Height - height) / 2);

            Stream stream = File.OpenRead($"{CORERenderContent.pathRenderer}\\logos\\logo4.png");

            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            Image[] images = new Image[1];
            fixed (byte* temp = &image.Data[0])
            {
                IntPtr ptr = new(temp);
                images[0] = new Image(image.Width, image.Height, ptr);
            }
            Glfw.SetWindowIcon(window, 1, images);

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);

            glEnable(GL_TEXTURE_2D);
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glViewport(0, 0, width, height);

            shader = new($"{CORERenderContent.pathRenderer}\\shaders\\SplashScreen.vert", $"{CORERenderContent.pathRenderer}\\shaders\\SplashScreen.frag");

            splashScreenTexture = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\splashscreen.png");

            font = new(32, $"{CORERenderContent.pathRenderer}\\Fonts\\baseFont.ttf");

            vao = glGenVertexArray();
            glBindVertexArray(vao);

            shader.Use();
            shader.SetInt("Texture", GL_TEXTURE0);

            splashScreenTexture.Use(GL_TEXTURE0);

            glDrawArrays(GL_TRIANGLES, 0, 6);
            Glfw.SwapBuffers(window);
        }

        public void WriteLine(string text) => WriteLine(text, new Vector3(1, 1, 1));

        public void WriteLine(string text, Vector3 color)
        {
            Glfw.MakeContextCurrent(window);

            glBindVertexArray(vao);

            shader.Use();

            splashScreenTexture.Use(GL_TEXTURE0);

            glDrawArrays(GL_TRIANGLES, 0, 6);

            font.RenderText(text, -1, -0.96f, 0.00165f, new Vector2(1, 0), color);

            Glfw.SwapBuffers(window);

            Glfw.MakeContextCurrent(COREMain.window);
        }

        public void Dispose()
        {
            glDeleteTexture(splashScreenTexture.Handle);
            Glfw.DestroyWindow(window);
            Glfw.RestoreWindow(COREMain.window);
            GC.Collect();
        }
    }
}