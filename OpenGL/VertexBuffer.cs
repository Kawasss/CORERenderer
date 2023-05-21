using CORERenderer.OpenGL;

namespace CORERenderer.OpenGL
{
    public class VertexBuffer
    {
        private int size = 0;
        private Usage usage;

        /// <summary>
        /// Gets and sets the size of the buffer, its expected to be in bytes
        /// </summary>
        public int Size { get => size; set => size = value; }
        public bool IsBound { get => VAO == Rendering.CurrentBoundVAO; }
        public Usage Usage { get => usage; }

        private uint VBO, VAO;

        public VertexBuffer(Usage usage, int size)
        {
            Rendering.GenerateEmptyBuffer(usage, out VBO, out VAO, size);
            this.usage = usage;
        }

        public VertexBuffer(int size)
        {
            Rendering.GenerateEmptyBuffer(out VBO, out VAO, size);
            this.usage = Usage.DynamicDraw;
        }

        public VertexBuffer(List<Vertex> vertices)
        {
            Rendering.GenerateFilledBuffer(out VBO, out VAO, vertices.ToArray());
            size = Vertex.GetFloatList(vertices).Count * sizeof(float);
            this.usage = Usage.StaticDraw;
        }

        public VertexBuffer(List<Vertex> vertices, Usage usage)
        {
            Rendering.GenerateFilledBuffer(usage, out VBO, out VAO, vertices.ToArray());
            size = Vertex.GetFloatList(vertices).Count * sizeof(float);
            this.usage = usage;
        }

        public VertexBuffer(float[] vertices)
        {
            Rendering.GenerateFilledBuffer(out VBO, out VAO, vertices);
            size = vertices.Length * sizeof(float);
            this.usage = Usage.StaticDraw;
        }
        public VertexBuffer(Usage usage, float[] vertices)
        {
            Rendering.GenerateFilledBuffer(usage, out VBO, out VAO, vertices);
            size = vertices.Length * sizeof(float);
            this.usage = usage;
        }

        public void SetSubData(int offset, List<Vertex> data)
        {
            GL.GlBindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            int size = Vertex.GetFloatList(data).Count * sizeof(float);
            Rendering.glBufferSubData(GL.GL_ARRAY_BUFFER, offset, size, Vertex.GetFloatList(data).ToArray());
        }
        public void SetSubData(int offset, List<float> data)
        {
            GL.GlBindBuffer(GL.GL_ARRAY_BUFFER, VBO);
            int size = data.Count * sizeof(float);
            Rendering.glBufferSubData(GL.GL_ARRAY_BUFFER, offset, size, data.ToArray());
        }

        /// <summary>
        /// Binds the VAO of the buffer
        /// </summary>
        public void Bind()
        {
            Rendering.glBindVertexArray(VAO);
        }

        public void Draw(PrimitiveType type, int first, int amount)
        {
            Bind();
            Rendering.glDrawArrays(type, first, amount);
        }
    }
}
