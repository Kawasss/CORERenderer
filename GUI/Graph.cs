using CORERenderer.Main;
using System.Data;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.GUI
{
    public class Graph
    {
        private uint lineVBO;
        private uint lineVAO;

        private float bottomX;
        private float bottomY;

        private float lastValue = 0;

        private int Width;
        private int Height;

        private int MaxValue;

        private float[] pointLocations;
        private float[] pointValues;

        public Div div;

        public Graph(int maxYValue, int width, int height, int x, int y)
        {
            Width = width;
            Height = height;
            MaxValue = maxYValue;

            bottomX = -(COREMain.monitorWidth / 2) + x;
            bottomY = -(COREMain.monitorHeight / 2) + y;

            div = new(width, height, x, y);
            div.SetRenderCallBack(Render); //causes the graph to be rendered when the div is rendered

            //more points = more data / accuracy but requires more raw power, right now 5 per row
            pointLocations = new float[] 
            {
                bottomX + width * 0.05f, bottomX + width * 0.075f, bottomX + width * 0.1f, bottomX + width * 0.125f, bottomX + width * 0.15f, 
                bottomX + width * 0.175f, bottomX + width * 0.2f, bottomX + width * 0.225f, bottomX + width * 0.25f, bottomX + width * 0.275f, 
                bottomX + width * 0.3f, bottomX + width * 0.325f, bottomX + width * 0.35f, bottomX + width * 0.375f, bottomX + width * 0.4f, 
                bottomX + width * 0.425f, bottomX + width * 0.45f, bottomX + width * 0.475f, bottomX + width * 0.5f, bottomX + width * 0.525f, 
                bottomX + width * 0.55f, bottomX + width * 0.575f, bottomX + width * 0.6f, bottomX + width * 0.625f, bottomX + width * 0.65f, 
                bottomX + width * 0.675f, bottomX + width * 0.7f, bottomX + width * 0.725f, bottomX + width * 0.75f, bottomX + width * 0.775f, 
                bottomX + width * 0.8f, bottomX + width * 0.825f, bottomX + width * 0.85f, bottomX + width * 0.875f, bottomX + width * 0.9f, 
                bottomX + width * 0.925f, bottomX + width * 0.95f
            };
            pointValues = new float[] 
            { 
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5, bottomY + 5,
                bottomY + 5, bottomY + 5
            };

            GenerateEmptyBuffer(out lineVBO, out lineVAO, sizeof(float) * 2 * 2 * pointLocations.Length);

            int vertexLocation = GenericShaders.solidColorQuadShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);
        }

        public void Update(float value)
        {
            if (COREMain.secondPassed) //makes the graph update only once every second to make it readable
            {
                if (value > MaxValue - 1)
                {
                    for (int i = 0; i < pointValues.Length; i++)
                    {
                        float oldValue = (pointValues[i] - bottomY) / Height * MaxValue; //reverse the calculations done at float position = ...
                        pointValues[i] = oldValue / value * Height + bottomY;
                    }
                    MaxValue = (int)(value + 10);
                }

                float position = value / MaxValue * Height + bottomY; //normalizes the given value to get a percentage of where the point is
                if (value > MaxValue)
                    position = Height;

                //all values get pushed back once so that the values gets passed down as time goes on
                for (int i = 0; i < pointValues.Length - 1; i++)
                    pointValues[i] = pointValues[i + 1];
                pointValues[^1] = position;

                lastValue = value;
            }
        }

        public void Render()
        {
            if (COREMain.secondPassed)
            {
                List<float> vertices = new();
                //slower but way more pleasant to write
                for (int i = 0; i < pointLocations.Length - 1; i++)
                {
                    vertices.Add(pointLocations[i]); vertices.Add(pointValues[i] + 1);
                    vertices.Add(pointLocations[i + 1]); vertices.Add(pointValues[i + 1] + 1);
                }

                glBindBuffer(GL_ARRAY_BUFFER, lineVBO);
                glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Count * sizeof(float), vertices.ToArray());
                glBindBuffer(GL_ARRAY_BUFFER, 0);
            }
            glBindVertexArray(lineVAO);

            GenericShaders.solidColorQuadShader.SetVector3("color", 1f, 0f, 1f);
            glDrawArrays(OpenGL.PrimitiveType.Lines, 0, 74);
            GenericShaders.solidColorQuadShader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            COREMain.debugText.RenderText($"{MaxValue}", bottomX, bottomY + Height - COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
            COREMain.debugText.RenderText($"{(int)(MaxValue * 0.75f)}", bottomX, bottomY + Height * 0.75f - COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
            COREMain.debugText.RenderText($"{(int)(MaxValue * 0.5f)}", bottomX, bottomY + Height * 0.5f - COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));
            COREMain.debugText.RenderText($"{(int)(MaxValue * 0.25f)}", bottomX, bottomY + Height * 0.25f - COREMain.debugText.characterHeight, 1, new COREMath.Vector2(1, 0));

            glBindVertexArray(0);
        }

        public void RenderStatic() => div.Render();
    }
}
