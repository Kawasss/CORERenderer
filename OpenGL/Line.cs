using COREMath;
using CORERenderer.shaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.OpenGL
{
    public class Line
    {
        private static Shader shader = null;
        public static float lineWidth = 1;

        private uint VBO, VAO;
        private Vector3 color;

        public Matrix model = Matrix.IdentityMatrix;

        public Line(Vector3 origin, Vector3 end, Vector3 color)
        {
            this.color = color;

            float[] buffer = new float[6] { origin.x, origin.y, origin.z, end.x, end.y, end.z };
            GenerateFilledBuffer(out VBO, out VAO, buffer);

            if (shader == null)
            {
                shader = new($"{COREMain.pathRenderer}\\shaders\\Line3D.vert", $"{COREMain.pathRenderer}\\shaders\\SolidColor.frag");
                int vertexLocation = shader.GetAttribLocation("aPos");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 3 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);
            }
        }

        public void Render()
        {
            glLineWidth(lineWidth);
            shader.SetMatrix("model", model);
            shader.SetVector3("color", color);
            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Lines, 0, 2);
        }
    }
}
