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

            shader ??= new($"{Main.COREMain.BaseDirectory}\\shaders\\Line3D.vert", $"{Main.COREMain.BaseDirectory}\\shaders\\SolidColor.frag");
            shader?.ActivateAttributes();
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
