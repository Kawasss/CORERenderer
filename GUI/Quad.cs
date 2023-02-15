using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Loaders;
using COREMath;
using CORERenderer.textures;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public class Div
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

        public Div(int width, int height, int x, int y)
        {
            Width = width;
            Height = height;

            bottomX = -(COREMain.monitorWidth / 2) + x;
            bottomY = -(COREMain.monitorHeight / 2) + y;

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

            int vertexLocation = GenericShaders.solidColorQuadShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            objIcon ??= Texture.ReadFromFile(false, $"{COREMain.pathRenderer}\\GUI\\objIcon.png");
            imageIcon ??= Texture.ReadFromFile(false, $"{COREMain.pathRenderer}\\GUI\\imageIcon.png");
            hdrIcon ??= Texture.ReadFromFile(false, $"{COREMain.pathRenderer}\\GUI\\hdrIcon.png");

            if (iconVBO == 0)
            {
                iconVBO = glGenBuffer();
                iconVAO = glGenVertexArray();

                glBindBuffer(GL_ARRAY_BUFFER, iconVBO);
                glBindVertexArray(iconVAO);

                //tell the gpu how large the buffer has to be
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length * 2, (IntPtr)null, GL_DYNAMIC_DRAW);//4 * 6

                vertexLocation = GenericShaders.image2DShader.GetAttribLocation("vertex");
                unsafe { glVertexAttribPointer((uint)vertexLocation, 4, GL_FLOAT, false, 4 * sizeof(float), (void*)0); }
                glEnableVertexAttribArray((uint)vertexLocation);

                GenericShaders.image2DShader.Use();
                GenericShaders.image2DShader.SetInt("Texture", GL_TEXTURE0);
                GenericShaders.image2DShader.SetMatrix("projection", GetOrthograpicProjectionMatrix());
            }

            lineVBO = glGenBuffer();
            lineVAO = glGenVertexArray();

            glBindBuffer(GL_ARRAY_BUFFER, lineVBO);
            glBindVertexArray(lineVAO);

            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 2 * 2, (IntPtr)null, GL_DYNAMIC_DRAW);

            vertexLocation = GenericShaders.solidColorQuadShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);
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
            GenericShaders.solidColorQuadShader.Use();

            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);

            renderCallBackMethod?.Invoke(); //only activates if it isnt null
        }

        public void RenderModelList() => SetRenderCallBack(renderModelList);

        public void RenderSubmodelList() => SetRenderCallBack(renderSubmodelList);

        private void renderModelList()
        {
            modelList = COREMain.scenes[COREMain.selectedScene].allModels;

            if (changedValue.Count == 0)
            {
                for (int i = 0; i < modelList.Count; i++)
                    changedValue.Add(false);
            }
            if (changedValue.Count < modelList.Count)
            {
                for (int i = changedValue.Count - 1; i < modelList.Count; i++)
                    changedValue.Add(false);
            }

            int offset = 20;
            for (int i = 0; i < modelList.Count; i++, offset += 37)
            {
                if (!Submenu.isOpen)
                    if (COREMain.CheckAABBCollisionWithClick((int)(COREMain.monitorWidth / 2 + bottomX), (int)(COREMain.monitorHeight / 2 + Height - offset + bottomY), Width, 37))
                    {   //makes the clicked object highlighted and makes all the others not highlighted
                    
                        if (!COREMain.scenes[COREMain.selectedScene].allModels[i].highlighted && !changedValue[i])
                        {
                            COREMain.scenes[COREMain.selectedScene].allModels[i].highlighted = true;
                            //makes all other models not highlighted, because this doesnt know which object was highlighted before
                            for (int j = 0; j < modelList.Count; j++)
                                if (j != i)
                                    COREMain.scenes[COREMain.selectedScene].allModels[j].highlighted = false;
                            changedValue[i] = true;
                            COREMain.scenes[COREMain.selectedScene].currentObj = i;
                        }
                        else if (!changedValue[i])
                        {
                            COREMain.scenes[COREMain.selectedScene].allModels[i].highlighted = false;
                            changedValue[i] = true;
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1;
                        }
                    }
                    else
                        changedValue[i] = false;
                
                if (!modelList[i].highlighted)
                    Write($"{modelList[i].name}", (int)(Width * 0.1f + 30), Height - offset);
                else
                    Write($"{modelList[i].name}", (int)(Width * 0.1f + 30), Height - offset, new Vector3(1, 0, 1));
                
                GenericShaders.image2DShader.Use();

                //sets up the buffer with the coordinates for the icon
                float[] vertices = new float[]
                {
                    bottomX + Width * 0.1f,      bottomY + Height - offset + 18f,      0, 0,
                    bottomX + Width * 0.1f,      bottomY + Height - offset + 18f - 25, 0, 1,
                    bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f - 25, 1, 1,

                    bottomX + Width * 0.1f,      bottomY + Height - offset + 18f,      0, 0,
                    bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f - 25, 1, 1,
                    bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f,      1, 0
                };
                glBindBuffer(GL_ARRAY_BUFFER, iconVBO);
                glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                glBindBuffer(GL_ARRAY_BUFFER, 0);

                //determines what icon to use
                if (modelList[i].type == RenderMode.ObjFile)
                    objIcon.Use(GL_TEXTURE0);
                else if (modelList[i].type == RenderMode.JPGImage || modelList[i].type == RenderMode.PNGImage)
                    imageIcon.Use(GL_TEXTURE0);
                else if (modelList[i].type == RenderMode.HDRFile)
                    hdrIcon.Use(GL_TEXTURE0);

                glBindVertexArray(iconVAO);
                glDrawArrays(PrimitiveType.Triangles, 0, 6);

                //line between the model names
                vertices = new float[]
                {
                    bottomX + Width * 0.1f, bottomY + Height - offset - 9,
                    bottomX + Width * 0.9f, bottomY + Height - offset - 9
                };

                glBindBuffer(GL_ARRAY_BUFFER, lineVBO);
                glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                glBindBuffer(GL_ARRAY_BUFFER, 0);

                GenericShaders.solidColorQuadShader.Use();
                GenericShaders.solidColorQuadShader.SetVector3("color", 0.3f, 0.3f, 0.3f);

                glBindVertexArray(lineVAO);
                glDrawArrays(PrimitiveType.Lines, 0, 2);

                GenericShaders.solidColorQuadShader.SetVector3("color", 0.15f, 0.15f, 0.15f);
            }
        }

        private void renderSubmodelList()
        {
            int addedThisLoop = 0;
            if (changedValue.Count == 0)
                for (int i = 0; i < COREMain.scenes.Count; i++)
                {
                    while (addedThisLoop < COREMain.scenes[COREMain.selectedScene].allModels[i].submodelNames.Count)
                    {
                        changedValue.Add(false);
                        addedThisLoop++;
                    }
                    addedThisLoop = 0;
                }
            else if (changedValue.Count < COREMain.scenes.Count)
                for (int i = changedValue.Count - 1; i < COREMain.scenes.Count; i++)
                {
                    while (addedThisLoop < COREMain.scenes[COREMain.selectedScene].allModels[i].submodelNames.Count)
                    {
                        changedValue.Add(false);
                        addedThisLoop++;
                    }
                    addedThisLoop = 0;
                }

            int offset = 20;
            foreach (Model model in COREMain.scenes[COREMain.selectedScene].allModels) //(int j = 0; j < COREMain.scenes[COREMain.selectedScene].allModels.Count; j++)
                foreach (Submodel submodel in model.submodels)//(int i = 0; i < COREMain.scenes[COREMain.selectedScene].allModels[j].submodelNames.Count; i++, offset  += 37)
                {
                    //add changing selected submodel
                    Write($"{submodel.Name}", (int)(Width * 0.1f + 30), Height - offset, new Vector3(1, 0, 1));

                    GenericShaders.image2DShader.Use();

                    //sets up the buffer with the coordinates for the icon
                    float[] vertices = new float[]
                    {
                            bottomX + Width * 0.1f,      bottomY + Height - offset + 18f,      0, 0,
                            bottomX + Width * 0.1f,      bottomY + Height - offset + 18f - 25, 0, 1,
                            bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f - 25, 1, 1,

                            bottomX + Width * 0.1f,      bottomY + Height - offset + 18f,      0, 0,
                            bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f - 25, 1, 1,
                            bottomX + Width * 0.1f + 25, bottomY + Height - offset + 18f,      1, 0
                    };
                    glBindBuffer(GL_ARRAY_BUFFER, iconVBO);
                    glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                    glBindBuffer(GL_ARRAY_BUFFER, 0);

                    objIcon.Use(GL_TEXTURE0);

                    glBindVertexArray(iconVAO);
                    glDrawArrays(PrimitiveType.Triangles, 0, 6);

                    //line between the model names
                    vertices = new float[]
                    {
                            bottomX + Width * 0.1f, bottomY + Height - offset - 9,
                            bottomX + Width * 0.9f, bottomY + Height - offset - 9
                    };

                    glBindBuffer(GL_ARRAY_BUFFER, lineVBO);
                    glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                    glBindBuffer(GL_ARRAY_BUFFER, 0);

                    GenericShaders.solidColorQuadShader.Use();
                    GenericShaders.solidColorQuadShader.SetVector3("color", 0.3f, 0.3f, 0.3f);

                    glBindVertexArray(lineVAO);
                    glDrawArrays(PrimitiveType.Lines, 0, 2);

                    GenericShaders.solidColorQuadShader.SetVector3("color", 0.15f, 0.15f, 0.15f);
                    offset += 37;
                }
        }

        public void RenderStatic() => Render();

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y, Vector3 color) => COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0), color);

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y, float scale, Vector3 color) => COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, scale, new Vector2(1, 0), color);

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(string text, int x, int y) => COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a value to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(Vector3 vector, int x, int y) => COREMain.debugText.RenderText($"{vector.x}  {vector.y}  {vector.z}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a value to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(Vector2 vector, int x, int y) => COREMain.debugText.RenderText($"{vector.x}  {vector.y}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(float value, int x, int y) => COREMain.debugText.RenderText($"{value}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Write(int value, int x, int y) => COREMain.debugText.RenderText($"{value}", bottomX + x, bottomY + y, 1, new Vector2(1, 0));

        /// <summary>
        /// Writes a string to a position relative of the quads position
        /// </summary>
        /// <param name="text"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale">standard: 1</param>
        public void Write(string text, int x, int y, float scale) => COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, scale, new Vector2(1, 0));

        public void WriteError(string text, int x, int y) => COREMain.debugText.RenderText(text, bottomX + x, bottomY + y, 1, new Vector2(1, 0), new Vector3(1, 0, 0));

        public void WriteError(Exception err, int x, int y) => COREMain.debugText.RenderText($"{err}", bottomX + x, bottomY + y, 1, new Vector2(1, 0), new Vector3(1, 0, 0));
    }
}
