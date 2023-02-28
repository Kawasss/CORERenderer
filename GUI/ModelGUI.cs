using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Loaders;
using COREMath;
using CORERenderer.textures;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public partial class Div
    {
        private void renderModelList()
        {
            if (Submenu.isOpen && !COREMain.clearedGUI)
                return;

            int offset = 20;
            int aa = 0;
            foreach (Model model in COREMain.scenes[COREMain.selectedScene].allModels)
            {
                if (offset >= Height)
                {
                    return;
                }

                if (!Submenu.isOpen)
                    if (COREMain.CheckAABBCollisionWithClick((int)(COREMain.monitorWidth / 2 + bottomX), (int)(COREMain.monitorHeight / 2 + Height - offset + bottomY), Width, 37))
                    {   //makes the clicked object highlighted and makes all the others not highlighted
                        if (!model.highlighted)
                        {
                            model.highlighted = true;
                            if (COREMain.scenes[COREMain.selectedScene].currentObj != -1)
                            COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].highlighted = false;
                            COREMain.scenes[COREMain.selectedScene].currentObj = aa;
                        }
                        else
                        {
                            model.highlighted = false;
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1;
                        }  
                    }

                string name;
                if (model.name.Length > 16)
                    name = $"{model.name[..16]}...";
                else
                    name = model.name;

                //add changing selected submodel
                if (model.highlighted)
                    Write($"{name}", (int)(Width * 0.1f + 30), Height - offset, 0.9f, new Vector3(1, 0, 1));
                else
                    Write($"{name}", (int)(Width * 0.1f + 30), Height - offset, 0.9f);

                GenericShaders.image2DShader.Use();

                //sets up the buffer with the coordinates for the icon
                float[] vertices = new float[]
                {
                    (int)(bottomX + Width * 0.1f),                  (int)(bottomY + Height - offset + 18f),                   0, 0,
                    (int)(bottomX + Width * 0.1f),                  (int)(bottomY + Height - offset + 18f - Height * 0.024f), 0, 1,
                    (int)(bottomX + Width * 0.1f + Width * 0.083f), (int)(bottomY + Height - offset + 18f - Height * 0.024f), 1, 1,

                    (int)(bottomX + Width * 0.1f),                  (int)(bottomY + Height - offset + 18f),                   0, 0,
                    (int)(bottomX + Width * 0.1f + Width * 0.083f), (int)(bottomY + Height - offset + 18f - Height * 0.024f), 1, 1,
                    (int)(bottomX + Width * 0.1f + Width * 0.083f), (int)(bottomY + Height - offset + 18f),                   1, 0
                };
                glBindBuffer(GL_ARRAY_BUFFER, iconVBO);
                glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), vertices);
                glBindBuffer(GL_ARRAY_BUFFER, 0);

                //determines what icon to use
                if (model.type == RenderMode.ObjFile)
                    objIcon.Use(GL_TEXTURE0);
                else if (model.type == RenderMode.JPGImage || model.type == RenderMode.PNGImage)
                    imageIcon.Use(GL_TEXTURE0);
                else if (model.type == RenderMode.HDRFile)
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
                offset += 37;
                aa++;
            }
        }

        private void renderSubmodelList()
        {
            int offset = 20;
            foreach (Model model in COREMain.scenes[COREMain.selectedScene].allModels) //(int j = 0; j < COREMain.scenes[COREMain.selectedScene].allModels.Count; j++)
            {
                foreach (Submodel submodel in model.submodels)//(int i = 0; i < COREMain.scenes[COREMain.selectedScene].allModels[j].submodelNames.Count; i++, offset  += 37)
                {
                    if (!Submenu.isOpen)
                        if (COREMain.CheckAABBCollisionWithClick((int)(COREMain.monitorWidth / 2 + bottomX), (int)(COREMain.monitorHeight / 2 + Height - offset + bottomY), Width, 37))
                        {   //makes the clicked object highlighted and makes all the others not highlighted
                            if (!submodel.highlighted)
                                submodel.highlighted = true;
                            else
                                submodel.highlighted = false;
                        }

                    string name;
                    if (submodel.Name.Length > 16)
                        name = $"{submodel.Name[..16]}...";
                    else
                        name = submodel.Name;

                    //add changing selected submodel
                    if (submodel.highlighted)
                        Write($"{name}", (int)(Width * 0.1f + 30), Height - offset, 0.9f, new Vector3(1, 0, 1));
                    else
                        Write($"{name}", (int)(Width * 0.1f + 30), Height - offset, 0.9f);

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
        }

        public static void renderModelInformation(Div div, Model model)
        {
            div.Write($"Translation:  {model.translation.x}  {model.translation.y}  {model.translation.z}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight), 0.8f);
            div.Write($"Scale:        {model.Scaling.x}  {model.Scaling.y}  {model.Scaling.z}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 2), 0.8f);
            div.Write($"Render wireframe: {model.renderLines}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 4), 0.8f);
            div.Write($"Submodels: {model.submodels.Count}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 5), 0.8f);

            div.Write($"Selected submodel:", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 7), 0.8f);
            div.Write($"Translation:  {MathF.Round(model.submodels[model.selectedSubmodel].translation.x, 2)} {MathF.Round(model.submodels[model.selectedSubmodel].translation.y,2 )} {MathF.Round(model.submodels[model.selectedSubmodel].translation.z, 2)}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 8), 0.8f);
            div.Write($"Scale:        {MathF.Round(model.submodels[model.selectedSubmodel].scaling.x, 2)} {MathF.Round(model.submodels[model.selectedSubmodel].scaling.y, 2)} {MathF.Round(model.submodels[model.selectedSubmodel].scaling.z, 2)}", (int)(div.Width * 0.01f), (int)(div.Height * 0.99f - COREMain.debugText.characterHeight * 9), 0.8f);
        }
    }
}
