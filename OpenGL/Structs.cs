using COREMath;
using CORERenderer.Loaders;
using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.OpenGL
{
    public struct Plane
    {
        Vector3 normal = Vector3.UnitVectorY;
        float distance = 0;

        public Plane(Vector3 normal, float distance)
        {
            this.normal = normal;
            this.distance = distance;
        }
    }

    public struct Frustrum
    {
        public Plane topFace;
        public Plane bottomFace;
        public Plane rightFace;
        public Plane leftFace;
        public Plane farFace;
        public Plane nearFace;
    }

    public struct Framebuffer
    {
        public uint FBO; //FrameBufferObject
        public uint VAO; //VertexArrayObject
        public uint Texture;
        public uint RBO; //RenderBufferObject
        public Shader shader;

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
}