using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.OpenGL
{
    public struct Framebuffer
    {
        public uint FBO; //FrameBufferObject
        public uint VAO; //VertexArrayObject
        public uint Texture;
        public uint RBO; //RenderBufferObject
        public Shader shader;

        public int width, height;

        public uint VBO; //VBO isnt really needed, but just in case

        public void Bind() => glBindFramebuffer(this);

        public void RenderFramebuffer()
        {
            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE_2D, 0);

            glBindFramebuffer(GL_FRAMEBUFFER, 0);
            glClear(GL_DEPTH_BUFFER_BIT);
            glDisable(GL_DEPTH_TEST);

            glClearColor(1, 1, 1, 1);

            this.shader.Use();

            glBindVertexArray(this.VAO);
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, this.Texture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }

    public partial class Rendering
    {
        public static Framebuffer GenerateFramebuffer(int width, int height) => GenerateFramebuffer(0, 0, width, height);

        public static Framebuffer GenerateFramebuffer(int x, int y, int width, int height)
        {
            float[] FrameBufferVertices = new float[]
            {
                (-width / 2f + x) / width, (-height / 2f + y + height) / height, 0, 1,
                (-width / 2f + x) / width, (-height / 2f + y) / height,           0, 0,
                (-width / 2f + x + width) / width, (-height / 2f + y) / height,           1, 0,

                (-width / 2f + x) / width, (-height / 2f + y + height) / height, 0, 1,
                (-width / 2f + x + width) / width, (-height / 2f + y) / height,           1, 0,
                (-width / 2f + x + width) / width, (-height / 2f + y + height) / height, 1, 1
            };
            for (int i = 0; i < FrameBufferVertices.Length; i += 4)
            {
                FrameBufferVertices[i] *= 2;
                FrameBufferVertices[i + 1] *= 2;
            }

            Framebuffer fb = new();
            unsafe
            {
                fb.width = width;
                fb.height = height;

                fb.shader = GenericShaders.Framebuffer;

                fb.FBO = glGenFramebuffer();
                glBindFramebuffer(GL_FRAMEBUFFER, fb.FBO);

                fb.Texture = glGenTexture();
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, fb.Texture);


                glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, null);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
                glBindTexture(GL_TEXTURE_2D, 0);

                fb.RBO = glGenRenderbuffer();
                glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

                glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
                glBindRenderbuffer(GL_RENDERBUFFER, 0);

                glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);


                fb.VBO = glGenBuffer();
                glBindBuffer(BufferTarget.ArrayBuffer, fb.VBO);

                fixed (float* temp = &FrameBufferVertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferData(GL_ARRAY_BUFFER, FrameBufferVertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
                }

                fb.VAO = glGenVertexArray();
                glBindVertexArray(fb.VAO);

                fb.shader.ActivateAttributes();
            }
            glBindBuffer(BufferTarget.ArrayBuffer, 0);
            glBindVertexArray(0);

            return fb;
        }
    }
}
