using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CORERenderer;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.Main
{
    public class GlobalMethods
    {
        public unsafe static Framebuffer GenerateFramebuffer()
        {
            float[] FrameBufferVertices = new float[]
            {
                -1,  1,  0,  1,
                -1, -1,  0,  0,
                 1, -1,  1,  0,

                -1,  1,  0,  1,
                 1, -1,  1,  0,
                 1,  1,  1,  1
            };

            Framebuffer fb = new();

            fb.shader = new($"{CORERenderContent.pathRenderer}\\shaders\\FrameBuffer.vert", $"{CORERenderContent.pathRenderer}\\shaders\\FrameBuffer.frag");

            fb.FBO = glGenFramebuffer();
            glBindFramebuffer(GL_FRAMEBUFFER, fb.FBO);

            fb.Texture = glGenTexture();
            glBindTexture(GL_TEXTURE_2D, fb.Texture);

            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, COREMain.Width, COREMain.Height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);

            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

            glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
            glBindTexture(GL_TEXTURE_2D, 0);

            fb.RBO = glGenRenderbuffer();
            glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

            glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, COREMain.Width, COREMain.Height);
            glBindRenderbuffer(GL_RENDERBUFFER, 0);

            glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);

            {
                fb.VBO = glGenBuffer();
                glBindBuffer(GL_ARRAY_BUFFER, fb.VBO);

                fixed (float* temp = &FrameBufferVertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, FrameBufferVertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                }

                fb.VAO = glGenVertexArray();
                glBindVertexArray(fb.VAO);

                int vertexLocation = fb.shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                vertexLocation = fb.shader.GetAttribLocation("aTexCoords");
                glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation);

                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);
            }

            return fb;
        }
    }
}
