using CORERenderer.Main;
using CORERenderer.OpenGL;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.shaders;
using COREMath;

namespace CORERenderer.GUI
{
    public class Graph
    {
        private uint lineVBO, lineVAO;

        private float bottomX, bottomY;

        private float lastValue = 0;

        private int Width, Height;

        public int MaxValue;

        private float[] pointLocations, pointValues, actualValues;

        public Div div;

        public Vector3 color = new(1f, 0f, 1f);

        public bool showValues = true;

        private Shader shader = GenericShaders.Quad;

        public Graph(int maxYValue, int width, int height, int x, int y)
        {
            Width = width;
            Height = height;
            MaxValue = maxYValue > 0 ? maxYValue : 10;

            bottomX = -(Main.COREMain.monitorWidth / 2) + x;
            bottomY = -(Main.COREMain.monitorHeight / 2) + y;

            div = new(width, height, x, y);
            div.SetRenderCallBack(Render); //causes the graph to be rendered when the div is rendered
            div.onlyUpdateEverySecond = true;

            //more points = more data / accuracy but requires more raw power
            List<float> temp = new();
            List<float> temp2 = new();
            List<float> temp3 = new();
            for (float i = 0.05f; i <= 0.95f; i += 0.0125f)
            {
                temp.Add(bottomX + width * i);
                temp2.Add(bottomY + Height * 0.05f);
                temp3.Add(0);
            }
            pointLocations = temp.ToArray();
            pointValues = temp2.ToArray();
            actualValues = temp3.ToArray();

            GenerateEmptyBuffer(out lineVBO, out lineVAO, sizeof(float) * 2 * 2 * pointLocations.Length);

            shader.ActivateAttributes();
        }

        public void Update(float value)
        {
            if (COREMain.secondPassed) //makes the graph update only once every second to make it readable
                UpdateConditionless(value);
        }

        public void UpdateConditionless(float value)
        {
            if (value > MaxValue)
                MaxValue = (int)(value * 1.2f);

            for (int i = 0; i < actualValues.Length - 1; i++)
            {
                actualValues[i] = actualValues[i + 1];
                float dividend = actualValues[i] / MaxValue;
                dividend = dividend > 1 ? 1 : dividend;
                pointValues[i] = dividend * Height + bottomY;
            }
            actualValues[^1] = value;
            pointValues[^1] = value / MaxValue * Height + bottomY;

            lastValue = value;
        }

        public void Render()
        {
            if (Main.COREMain.secondPassed)
                RenderConditionless();
        }

        public void RenderConditionless()
        {
            List<float> vertices = new();
            //slower but way more pleasant to write
            for (int i = 0; i < pointLocations.Length - 1; i++)
            {
                vertices.Add(pointLocations[i]); vertices.Add(pointValues[i] + 1);
                vertices.Add(pointLocations[i + 1]); vertices.Add(pointValues[i + 1] + 1);
            }

            glBindBuffer(BufferTarget.ArrayBuffer, lineVBO);
            glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Count * sizeof(float), vertices.ToArray());
            glBindBuffer(BufferTarget.ArrayBuffer, 0);

            glBindVertexArray(lineVAO);

            shader.SetVector3("color", color);
            glDrawArrays(OpenGL.PrimitiveType.Lines, 0, pointLocations.Length * 2);
            shader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            if (showValues)
            {
                Main.COREMain.debugText.RenderText($"{MaxValue}", bottomX, bottomY + Height - Main.COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
                Main.COREMain.debugText.RenderText($"{(int)(MaxValue * 0.75f)}", bottomX, bottomY + Height * 0.75f - Main.COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
                Main.COREMain.debugText.RenderText($"{(int)(MaxValue * 0.5f)}", bottomX, bottomY + Height * 0.5f - Main.COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
                Main.COREMain.debugText.RenderText($"{(int)(MaxValue * 0.25f)}", bottomX, bottomY + Height * 0.25f - Main.COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));

                Main.COREMain.debugText.RenderText($"{MathF.Round(lastValue, 1)}", bottomX + Width * 0.96f, pointValues[^1] - Height * 0.01f, 0.8f, new COREMath.Vector2(1, 0));
            }
            glBindVertexArray(0);
        }

        public void RenderStatic() => div.Render();
    }
}