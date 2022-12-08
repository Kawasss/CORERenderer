using System;
using System.Net;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Text;
using System.Drawing.Drawing2D;
using static CORERenderer.OpenGL.GL;
using GLFW;
using StbImageSharp;
using System.Drawing.Imaging;
using CORERenderer.GLFW;

namespace CORERenderer.textures
{
    public class Texture
    {
        public readonly uint Handle;
        public string path;
        public string name;

        public static unsafe Texture ReadFromFile(string imagePath)
        {
            uint handle = glGenTexture();

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, handle);

            StbImage.stbi_set_flip_vertically_on_load(1);

            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Couldnt find given texture at {imagePath}, using default texture");
                imagePath = $"{CORERenderContent.pathRenderer}\\textures\\placeholder.png";
            }

            ImageResult image = ImageResult.FromStream(File.OpenRead(imagePath), ColorComponents.RedGreenBlueAlpha);
            fixed (byte* temp = &image.Data[0])
            {
                IntPtr ptr = new(temp);
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, image.Width, image.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
            }

            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

            glGenerateMipmap(GL_TEXTURE_2D);

            List<int> local = new();
            for (int i = imagePath.IndexOf("\\"); i > -1; i = imagePath.IndexOf("\\", i + 1))
                local.Add(i);

            return new Texture(handle) { path = imagePath, name = imagePath[local[^1]..]};
        }

        public Texture(uint newHandle)
        {
            Handle = newHandle;
        }

        public void Use(int texture)
        {
            glActiveTexture(texture);
            glBindTexture(GL_TEXTURE_2D, Handle);
        }
    }
} 