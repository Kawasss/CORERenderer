using COREMath;
using CORERenderer.Main;
using CORERenderer.shaders;
using CORERenderer.textures;
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

        public int width = 0, height = 0;
        internal int mipMapCount;

        internal uint depthTexture;

        private bool useChromAber = false, useVignette = false, useDOF = false;
        private Vector3 chromAberStrength = Vector3.Zero;
        private float vignetteStrength, DOFStrength = 1, DOFFocusPoint;

        public bool UsesChromaticAberration { get => useChromAber; set => useChromAber = value; }
        public bool UsesVignette            { get => useVignette; set => useVignette = value; }
        public bool UsesDepthOfField        { get => useDOF; set => useDOF = value; }

        public Vector3 ChromaticAberrationStrength { get => chromAberStrength; set => chromAberStrength = new(Math.Clamp(value.x, 0, 1), Math.Clamp(value.y, 0, 1), Math.Clamp(value.z, 0, 1)); }
        public float   DepthOfFieldFocusPoint      { get => DOFFocusPoint; set => DOFFocusPoint = value; }
        public float   VignetteStrength            { get => vignetteStrength; set => vignetteStrength = Math.Clamp(value, 0, 1); }
        public float   DepthOfFieldStrength        { get => DOFStrength; set  => DOFStrength = Math.Clamp(value, 0, 1); }

        public uint VBO; //VBO isnt really needed, but just in case

        public bool isComplete { get { Bind(); bool status = glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE; glBindFramebuffer(GL_FRAMEBUFFER, 0); return status; } }

        public void Bind() 
        { 
            /*if (useDOF)
            {
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, this.depthTexture);
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTexture, 0);
            } */
            glBindFramebuffer(this); 
        }

        public void RenderFramebuffer()
        {
            if (useDOF)
            {
                /*+Bind();
                depthTexture.Use(ActiveTexture.Texture1);
                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTexture.Handle, 0);*/
                
                //foreach (Loaders.Model model in COREMain.CurrentScene.models)
                //    model.Render();
                //COREMain.CurrentScene.skybox.Render();
            }
            //if (useDOF)
            //    depthTexture.WriteAsPNG($"{COREMain.BaseDirectory}\\renders\\depth.png");

            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE_2D, 0);

            glBindFramebuffer(GL_FRAMEBUFFER, 0);
            glClear(GL_DEPTH_BUFFER_BIT);
            glDisable(GL_DEPTH_TEST);

            glClearColor(1, 1, 1, 1);

            this.shader.Use();

            shader.SetBool("useChromaticAberration", useChromAber);
            shader.SetVector3("chromAberIntensities", chromAberStrength);

            shader.SetBool("useVignette", useVignette);
            shader.SetFloat("vignetteStrength", vignetteStrength);

            shader.SetBool("useDOF", useDOF);
            shader.SetFloat("DOFStrength", DOFStrength);
            shader.SetFloat("DOFFocusPoint", DOFFocusPoint);

            shader.SetInt("mipMapCount", mipMapCount);

            shader.SetInt("screenTexture", 0);
            shader.SetInt("depthTexture", 1);
            shader.SetFloat("farPlane", Rendering.Camera.FarPlane);
            shader.SetFloat("nearPlane", Rendering.Camera.NearPlane);

            glBindVertexArray(this.VAO);
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, this.Texture);
            glGenerateMipmap(GL_TEXTURE_2D);
            glActiveTexture(GL_TEXTURE1);
            glBindTexture(GL_TEXTURE_2D, this.depthTexture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        public void Dispose()
        {
            glDeleteFramebuffer(FBO);
            glDeleteVertexArray(VAO);
            glDeleteTexture(Texture);
            glDeleteRenderbuffer(RBO);
            glDeleteBuffer(VBO);
        }

        public Framebuffer(int width, int height)
        {
            this.width = width;
            this.height = height;
            chromAberStrength = Vector3.Zero;
            //depthTexture = textures.Texture.GenerateEmptyTexture(width, height);
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

            int mipMapCount = 1 + (int)Math.Floor(Math.Log2(Math.Max(width, height)));
            Framebuffer fb = new(width, height) { mipMapCount = mipMapCount };
            unsafe
            {
                fb.shader = GenericShaders.Framebuffer;

                fb.FBO = glGenFramebuffer();
                glBindFramebuffer(GL_FRAMEBUFFER, fb.FBO);

                fb.Texture = glGenTexture();
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, fb.Texture);

                glTexImage2D(Image2DTarget.Texture2D, 0, GL_RGBA16F, width, height, 0, GL_RGBA, GL_FLOAT, null);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR_MIPMAP_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
                glGenerateMipmap(GL_TEXTURE_2D);

                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, fb.Texture, 0);
                glBindTexture(GL_TEXTURE_2D, 0);

                fb.depthTexture = glGenTexture();
                glActiveTexture(GL_TEXTURE0);
                glBindTexture(GL_TEXTURE_2D, fb.depthTexture);

                glTexImage2D(Image2DTarget.Texture2D, 0, GL_DEPTH_COMPONENT, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, null);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
                glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);

                glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, fb.depthTexture, 0);

                /*fb.RBO = glGenRenderbuffer();
                glBindRenderbuffer(GL_RENDERBUFFER, fb.RBO);

                glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
                glBindRenderbuffer(GL_RENDERBUFFER, 0);

                glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, fb.RBO);*/
                
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
