using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Loaders;
using COREMath;
using CORERenderer.textures;
using CORERenderer.OpenGL;
using CORERenderer.shaders;

namespace CORERenderer.GUI
{
    public partial class Div
    {
        private uint VBO;
        private uint VAO;

        public float bottomX;
        public float bottomY;

        public int Width;
        public int Height;

        static private Texture objIcon = null;
        static private Texture imageIcon = null;
        static private Texture hdrIcon = null;

        static private uint iconVBO;
        static private uint iconVAO;

        //maybe make line rendering abstract and put in rendering.cs
        static private uint lineVBO;
        static private uint lineVAO;

        private List<bool> changedValue = new();

        private List<List<bool>> changedsubValue = new();

        Action renderCallBackMethod = null;

        public List<Model> modelList;

        public bool onlyUpdateEverySecond = false;

        private Shader shaderQ = GenericShaders.Quad;

        public Div(int width, int height, int x, int y)
        {
            Width = width;
            Height = height;

            bottomX = -(Main.COREMain.monitorWidth / 2) + x;
            bottomY = -(Main.COREMain.monitorHeight / 2) + y;

            float[] vertices = new float[]
            {
                bottomX,         bottomY + height,
                bottomX,         bottomY,
                bottomX + width, bottomY,

                bottomX,         bottomY + height,
                bottomX + width, bottomY,
                bottomX + width, bottomY + height
            };

            GenerateFilledBuffer(out VBO, out VAO, vertices);

            shaderQ.ActivateAttributes();

            objIcon ??= Texture.ReadFromFile(false, $"{Main.COREMain.pathRenderer}\\GUI\\objIcon.png");
            imageIcon ??= Texture.ReadFromFile(false, $"{Main.COREMain.pathRenderer}\\GUI\\imageIcon.png");
            hdrIcon ??= Texture.ReadFromFile(false, $"{Main.COREMain.pathRenderer}\\GUI\\hdrIcon.png");

            if (iconVBO == 0)
            {
                iconVBO = glGenBuffer();
                iconVAO = glGenVertexArray();

                glBindBuffer(BufferTarget.ArrayBuffer, iconVBO);
                glBindVertexArray(iconVAO);

                //tell the gpu how large the buffer has to be
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length * 2, (IntPtr)null, GL_DYNAMIC_DRAW);//4 * 6

                shader.ActivateAttributes();

                shader.Use();
                shader.SetInt("Texture", 0);
                shader.SetMatrix("projection", GetOrthograpicProjectionMatrix(Main.COREMain.Width, Main.COREMain.Height));
            }

            lineVBO = glGenBuffer();
            lineVAO = glGenVertexArray();

            glBindBuffer(BufferTarget.ArrayBuffer, lineVBO);
            glBindVertexArray(lineVAO);

            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 2 * 2, (IntPtr)null, GL_DYNAMIC_DRAW);

            shader.ActivateAttributes();
        }

        /// <summary>
        /// Sets a method that will be called after rendering the div
        /// </summary>
        /// <param name="renderCallBack">method</param>
        public void SetRenderCallBack(Action renderCallBack)
        {
            renderCallBackMethod = renderCallBack;
        }

        public void Render()
        {
            if (onlyUpdateEverySecond && !Main.COREMain.secondPassed)
                return;

            shaderQ.Use();

            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);

            renderCallBackMethod?.Invoke(); //only activates if it isnt null
        }

        public void RenderStatic() => Render();

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y, Vector3 color) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0), color);

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y, float scale, Vector3 color) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, scale, new Vector2(1, 0), color);

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a value to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(Vector3 vector, int x, int y) => Main.COREMain.debugText.RenderText($"{vector.x}  {vector.y}  {vector.z}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a value to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(Vector2 vector, int x, int y) => Main.COREMain.debugText.RenderText($"{vector.x}  {vector.y}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(float value, int x, int y) => Main.COREMain.debugText.RenderText($"{value}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(int value, int x, int y) => Main.COREMain.debugText.RenderText($"{value}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale">standard: 1</param>
        public void Write(string text, int x, int y, float scale) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, scale, new Vector2(1, 0));

        public void WriteError(string text, int x, int y) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0), new Vector3(1, 0, 0));

        public void WriteError(string text, int x, int y, float scale) => Main.COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, scale, new Vector2(1, 0), new Vector3(1, 0, 0));

        public void WriteError(Exception err, int x, int y) => Main.COREMain.debugText.RenderText($"{err}", bottomX + x, bottomY + y, 1, new Vector2(1, 0), new Vector3(1, 0, 0));
    }
}