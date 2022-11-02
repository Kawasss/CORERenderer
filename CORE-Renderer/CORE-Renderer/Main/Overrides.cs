using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monitor = CORERenderer.GLFW.Structs.Monitor;
using Image = CORERenderer.GLFW.Structs.Image;
using static CORERenderer.GL;
using COREMath;
using CORERenderer.shaders;
using CORERenderer.Main;

namespace CORERenderer
{
    public class Rendering : COREMain
    {
        public unsafe static void AlwaysLoad()
        {
            Stream stream = File.OpenRead($"{CORERenderContent.pathRenderer}\\logos\\logo4.png");

            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            Image[] images = new Image[1];
            fixed (byte* temp = &image.Data[0])
            {
                IntPtr ptr = new(temp);
                images[0] = new Image(image.Width, image.Height, ptr);
            }
            Glfw.SetWindowIcon(window, 1, images);

            Glfw.SetScrollCallback(window, CORERenderContent.ScrollCallback);

            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);
            Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);
        }

        public unsafe static void AlwaysRender()
        {
            //render stuff that is constant and doesnt change, tried to put grid rendering here but didnt work?
        }

        static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
            Width = width;
            Height = height;
        }

        public unsafe virtual void OnLoad()
        {
            throw new NotImplementedException("OnLoad() has not been implemented");
        }

        public unsafe virtual void RenderEveryFrame()
        {
            throw new NotImplementedException("RenderEveryFrame() has not been implemented");
        }

        public unsafe virtual void EveryFrame(Window window, float delta)
        {
            throw new NotImplementedException("EveryFrame() has not been implemented");
        }
    }
}
