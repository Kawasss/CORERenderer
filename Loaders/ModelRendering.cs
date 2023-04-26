using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.Loaders
{
    public partial class Model
    {
        public void Render()
        {
            highlighted = COREMain.selectedID == ID;

            if (type == RenderMode.ObjFile || type == RenderMode.STLFile)
                RenderModel();

            else if (type == RenderMode.JPGImage)
                RenderImage();

            else if (type == RenderMode.PNGImage)
                RenderImage();
            else if (type == RenderMode.HDRFile)
                Rendering.RenderBackground(hdr);
        }

        public void RenderBackground() => Rendering.RenderBackground(hdr);

        private unsafe void RenderModel()
        {
            for (int i = 0; i < submodels.Count; i++)
            {
                submodels[i].renderLines = renderLines;
                submodels[i].parentModel = Matrix.IdentityMatrix * new Matrix(scaling, translation) * (MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z));

                if (submodels[i].highlighted)
                {
                    this.highlighted = true;
                    selectedSubmodel = i;
                }


                if (!submodels[i].isTranslucent)
                    submodels[i].Render();
                else
                    translucentSubmodels.Add(submodels[i]);
            }
        }

        private void RenderImage()
        {
            Matrix model = Matrix.IdentityMatrix * MathC.GetScalingMatrix(scaling) * MathC.GetTranslationMatrix(translation) * MathC.GetRotationXMatrix(rotation.x) * MathC.GetRotationYMatrix(rotation.y) * MathC.GetRotationZMatrix(rotation.z);
            shader.SetMatrix("model", model);
            shader.SetInt("material.diffuse", GL_TEXTURE0);
            usedTextures[Materials[0].Texture].Use(GL_TEXTURE0);

            glBindVertexArray(VAO);
            glDrawArrays(PrimitiveType.Triangles, 0, Vertices[0].Count / 8);
            if (renderLines)
            {
                GL.glLineWidth(1.5f);
                shader.SetVector3("overrideColor", new(1, 0, 1));
                glDrawArrays(PrimitiveType.Lines, 0, Vertices[0].Count / 8);
                shader.SetVector3("overrideColor", new(0, 0, 0));
            }
        }

    }
}
