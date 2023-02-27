using CORERenderer.Main;
using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using COREMath;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public class TabManager
    {
        private Shader shader;
        private Shader plusShader;

        private uint VBO, VAO;

        private uint plusVBO, plusVAO;

        private string[] names;

        private int selectedDivIndex = -1;
        private int previousSelected = -1;

        private float bottomX = 0;
        private float bottomY = 0;

        private Dictionary<string, Div> tabDivBinding = new();

        private bool isAttached = false;
        private int amountAttached = 0;

        private bool FirstRender = true;

        private attachedToType type;

        public TabManager(string[] nameOfTabs)
        {
            shader = new($"{COREMain.pathRenderer}\\shaders\\Tab.vert", $"{COREMain.pathRenderer}\\shaders\\Tab.frag");
            names = nameOfTabs;
        }

        /// <summary>
        /// Tabs can be added in runtime with this variant, automatically attaches to the scene framebuffer
        /// </summary>
        /// <param name="name"></param>
        public TabManager(string name)
        {
            shader = new($"{COREMain.pathRenderer}\\shaders\\Tab.vert", $"{COREMain.pathRenderer}\\shaders\\Tab.frag");
            plusShader = new($"{COREMain.pathRenderer}\\shaders\\Tab.vert", $"{COREMain.pathRenderer}\\shaders\\Tab.frag");

            type = attachedToType.RenderWindow;

            selectedDivIndex = 0;
            tabDivBinding.Add($"{name} {1}", null);
            tabDivBinding.Add("+", null);
            names = new string[1] { name };
            amountAttached++;

            bottomX = COREMain.viewportX;
            bottomY = COREMain.viewportY + COREMain.renderHeight;

            float localX = -COREMain.monitorWidth / 2 + bottomX;
            float localY = -COREMain.monitorHeight / 2 + COREMain.viewportY;

            float[] vertices = new float[]
            {
                localX, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f,
                localX, localY + COREMain.renderHeight,
                localX + COREMain.monitorWidth * 0.034f, localY + COREMain.renderHeight,

                localX, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f,
                localX + COREMain.monitorWidth * 0.034f, localY + COREMain.renderHeight,
                localX + COREMain.monitorWidth * 0.034f, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f
            };

            GenerateFilledBuffer(out VBO, out VAO, vertices);

            glBindVertexArray(VAO);

            int vertexLocation = shader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            GenerateEmptyBuffer(out plusVBO, out plusVAO, vertices.Length * sizeof(float));

            vertexLocation = plusShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            shader.Use();
            shader.SetMatrix("projection", GetOrthograpicProjectionMatrix());

            plusShader.Use();
            plusShader.SetMatrix("projection", GetOrthograpicProjectionMatrix());

            isAttached = true;
        }

        /// <summary>
        /// Attaches the tab manager to a div, binding the div to the first tab. If used later it will bind the given div to the second tab, then the third, etc.
        /// </summary>
        /// <param name="div">all the attached divs have to be at the same x and y coordinates, else it will result in corrupted locations</param>
        public void AttachTo(Div div)
        {
            if (VBO == 0)
            {
                type = attachedToType.Div;

                selectedDivIndex = 0;
                tabDivBinding.Add(names[0], div);
                amountAttached++;

                bottomX = COREMain.monitorWidth / 2 + tabDivBinding[names[0]].bottomX;
                bottomY = COREMain.monitorHeight / 2 + tabDivBinding[names[0]].bottomY + tabDivBinding[names[0]].Height;

                float[] vertices = new float[]
                {
                    div.bottomX, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f,
                    div.bottomX, div.bottomY + div.Height,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height,

                    div.bottomX, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f
                };

                GenerateFilledBuffer(out VBO, out VAO, vertices);

                glBindVertexArray(VAO);

                int vertexLocation = shader.GetAttribLocation("aPos");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);

                shader.Use();
                shader.SetMatrix("projection", GetOrthograpicProjectionMatrix());

                isAttached = true;
                return;
            }
            tabDivBinding.Add(names[amountAttached], div);
        }

        /// <summary>
        /// Attaches the tab manager to a div, binding the div to the first tab. If used later it will bind the given div to the second tab, then the third, etc.
        /// </summary>
        /// <param name="graph">all the attached divs have to be at the same x and y coordinates, else it will result in corrupted locations</param>
        public void AttachTo(Graph graph)
        {
            Div div = graph.div;
            if (VBO == 0)
            {
                type = attachedToType.Graph;

                selectedDivIndex = 0;
                tabDivBinding.Add(names[0], div);
                amountAttached++;

                bottomX = COREMain.monitorWidth / 2 + tabDivBinding[names[0]].bottomX;
                bottomY = COREMain.monitorHeight / 2 + tabDivBinding[names[0]].bottomY + tabDivBinding[names[0]].Height;

                float[] vertices = new float[]
                {
                    div.bottomX, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f,
                    div.bottomX, div.bottomY + div.Height,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height,

                    div.bottomX, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height,
                    div.bottomX + COREMain.monitorWidth * 0.034f, div.bottomY + div.Height + COREMain.monitorHeight * 0.018f
                };

                GenerateFilledBuffer(out VBO, out VAO, vertices);

                glBindVertexArray(VAO);

                int vertexLocation = shader.GetAttribLocation("aPos");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);

                shader.Use();
                shader.SetMatrix("projection", GetOrthograpicProjectionMatrix());

                isAttached = true;
                return;
            }
            tabDivBinding.Add(names[amountAttached], div);
        }

        public void Render()
        {
            if (!Submenu.isOpen)
                CheckCollision();

            previousSelected = selectedDivIndex;

            if (type != attachedToType.RenderWindow)
            {
                Div selectedDiv = tabDivBinding[names[selectedDivIndex]];

                if ((!isAttached && COREMain.secondPassed) || (!isAttached && FirstRender))
                {
                    COREMain.console.WriteError("TabManager is not attached to a div");
                    FirstRender = false;
                    return;
                }

                selectedDiv.Render(); //renders the chosen tab

                for (int i = 0; i < names.Length; i++) //renders the little tabs on top with the divs names
                {
                    shader.Use();

                    shader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(new Vector3(i * COREMain.monitorWidth * 0.034f, 0, 0)));

                    if (i >= tabDivBinding.Count)
                        shader.SetBool("notImplemented", true);
                    else if (selectedDivIndex == i)
                        shader.SetBool("selected", true);
                    else
                        shader.SetBool("selected", false);

                    glBindVertexArray(VAO);
                    glDrawArrays(PrimitiveType.Triangles, 0, 6);

                    shader.SetBool("selected", false);
                    shader.SetBool("notImplemented", false);

                    selectedDiv.Write(names[i], (int)(i * COREMain.monitorWidth * 0.034f + 5), selectedDiv.Height + 5, 0.83f);
                }
            }
            else
            {
                if (amountAttached < COREMain.scenes.Count)
                {   //makes sure the + is always at the end whilst updating the tabs
                    tabDivBinding.Remove("+");
                    while (amountAttached < COREMain.scenes.Count)
                    {
                        tabDivBinding.Add($"{names[0]} {amountAttached + 1}", null);
                        amountAttached++;
                    }
                    tabDivBinding.Add("+", null);
                    
                }
                for (int i = 0; i <= COREMain.scenes.Count; i++)
                {
                    shader.Use();

                    if (i != COREMain.scenes.Count)
                        shader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(new Vector3(i * COREMain.monitorWidth * 0.034f, 0, 0)));
                    else
                    {
                        plusShader.Use();

                        float localX = -COREMain.monitorWidth / 2 + bottomX;
                        float localY = -COREMain.monitorHeight / 2 + COREMain.viewportY;

                        float[] vertices = new float[]
                        {
                            localX, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f,
                            localX, localY + COREMain.renderHeight,
                            localX + COREMain.monitorWidth * 0.01f, localY + COREMain.renderHeight,

                            localX, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f,
                            localX + COREMain.monitorWidth * 0.01f, localY + COREMain.renderHeight,
                            localX + COREMain.monitorWidth * 0.01f, localY + COREMain.renderHeight + COREMain.monitorHeight * 0.018f
                        };

                        glBindBuffer(GL_ARRAY_BUFFER, plusVBO);
                        glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                        glBindBuffer(GL_ARRAY_BUFFER, 0);

                        plusShader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(new Vector3(i * COREMain.monitorWidth * 0.034f, 0, 0)));//new(0.471f, 1, 1, COREMain.monitorWidth * 0.034f * i, 0, 0));// * MathC.GetScalingMatrix(0.471f, 1, 1)
                        
                        plusShader.SetBool("selected", false);
                        glBindVertexArray(plusVAO);
                        glDrawArrays(PrimitiveType.Triangles, 0, 6);

                        COREMain.debugText.RenderText($"+", -COREMain.monitorWidth * 0.5f + bottomX + (int)(i * COREMain.monitorWidth * 0.034f + 5), -COREMain.monitorHeight * 0.5f + bottomY + COREMain.debugText.characterHeight * 0.83f / 2, 0.83f, new Vector2(1, 0));
                        return;
                    }

                    if (selectedDivIndex == i)
                        shader.SetBool("selected", true);
                    else
                        shader.SetBool("selected", false);

                    glBindVertexArray(VAO);
                    glDrawArrays(PrimitiveType.Triangles, 0, 6);

                    shader.SetBool("selected", false);
                    shader.SetBool("notImplemented", false);

                    COREMain.debugText.RenderText($"{names[0]} {i + 1}", -COREMain.monitorWidth * 0.5f + bottomX + (int)(i * COREMain.monitorWidth * 0.034f + 5), -COREMain.monitorHeight * 0.5f + bottomY + 5, 0.83f, new Vector2(1, 0));

                    
                        
                    COREMain.selectedScene = selectedDivIndex;
                }
            }
        }

        public void RenderStatic() => Render();

        private void CheckCollision()
        {
            for (int i = 0; i < tabDivBinding.Count; i++)
                if (COREMain.CheckAABBCollisionWithClick((int)bottomX, (int)bottomY, (int)((i + 1) * COREMain.monitorWidth * 0.034f), (int)(COREMain.monitorHeight * 0.018f)))
                {
                    selectedDivIndex = i;
                    if (type == attachedToType.RenderWindow && selectedDivIndex == tabDivBinding.Count - 1)
                    {
                        COREMain.scenes.Add(new());
                        COREMain.scenes[^1].OnLoad(Array.Empty<string>());
                    }
                    return;
                }
        }

        private enum attachedToType
        {
            Div,
            Graph,
            List,
            RenderWindow
        }
    }
}