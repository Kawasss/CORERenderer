using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using COREMath;
using CORERenderer.textures;
using System.Diagnostics;

namespace CORERenderer.GUI
{
    public partial class Div
    {
        private Shader shader = GenericShaders.Image2D;

        public void RenderModelInformation()
        {
            if (Main.COREMain.GetCurrentObjFromScene == -1)
                return;

            int totalOffset = 0;
            Model model = Main.COREMain.CurrentModel;

            if (model.hdr != null)
                return;

            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Translation: {Math.Round(model.Transform.translation.x, 2)} {Math.Round(model.Transform.translation.y, 2)} {Math.Round(model.Transform.translation.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Scaling:     {Math.Round(model.Transform.scale.x, 2)} {Math.Round(model.Transform.scale.y, 2)} {Math.Round(model.Transform.scale.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write($"Rotation:    {Math.Round(model.Transform.rotation.x, 2)} {Math.Round(model.Transform.rotation.y, 2)} {Math.Round(model.Transform.rotation.z, 2)}", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            //shitty
            Texture textureToDraw = Globals.usedTextures[model.submodels[0].material.Texture];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Texture:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth  / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = Globals.usedTextures[model.submodels[0].material.DiffuseMap];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Diffuse map:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = Globals.usedTextures[model.submodels[0].material.SpecularMap];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Specular map:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = Globals.usedTextures[model.submodels[0].material.NormalMap];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("Normal map:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = Globals.usedTextures[model.submodels[0].material.MetalMap];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("metal map:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);

            textureToDraw = Globals.usedTextures[model.submodels[0].material.aoMap];
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
            this.Write("AO map:", (int)(this.Width * 0.05f), this.Height - totalOffset, 0.7f);
            totalOffset += 150;
            textureToDraw.RenderAs2DImage((int)(Main.COREMain.monitorWidth / 2 * 0.996f - this.Width * 0.95f), (int)(-Main.COREMain.monitorHeight / 2 + this.Height - totalOffset));
            totalOffset += (int)(Main.COREMain.debugText.characterHeight * 1.1f);
        }

        public void RenderModelList(List<Model> models)
        {
            for (int i = 0; i < models.Count; i++)
            {
                float offset = this.Height - Main.COREMain.debugText.characterHeight * 0.8f * (i + 1);

                if (offset <= 0) //return when the list goes outside the bounds of the div
                    return;

                //if the model is selected it gets a different color to reflect that
                Vector3 color = models[i].highlighted ? new(1, 0, 1) : new(1, 1, 1);
                this.Write($"[{models[i].type}] {models[i].Name}", (int)(this.Width * 0.03f), (int)offset, 0.7f, color);
            }
        }
    }
}