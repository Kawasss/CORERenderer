using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using COREMath;

namespace CORERenderer.GUI
{
    public partial class Div
    {
        private Shader shader = GenericShaders.Image2D;

        public void RenderModelInformation()
        {
            if (COREMain.GetCurrentObjFromScene == -1)
                return;

            Model model = COREMain.CurrentModel;

            this.Write($"Translation: {Math.Round(model.translation.x, 2)} {Math.Round(model.translation.y, 2)} {Math.Round(model.translation.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f), 0.7f);
            this.Write($"Scaling:     {Math.Round(model.Scaling.x, 2)} {Math.Round(model.Scaling.y, 2)} {Math.Round(model.Scaling.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f * 2), 0.7f);
            this.Write($"Rotation:    {Math.Round(model.rotation.x, 2)} {Math.Round(model.rotation.y, 2)} {Math.Round(model.rotation.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f * 3), 0.7f);
        }

        public void RenderModelList(List<Model> models)
        {
            for (int i = 0; i < models.Count; i++)
            {
                float offset = this.Height - COREMain.debugText.characterHeight * 1.1f * (i + 1);

                if (offset <= 0) //return when the list goes outside the bounds of the div
                    return;

                //if the model is selected it gets a different color to reflect that
                Vector3 color = models[i].highlighted ? new(1, 0, 1) : new(1, 1, 1);
                this.Write($"[{models[i].type}] {models[i].Name}", (int)(this.Width * 0.03f), (int)offset, 0.85f, color);
            }
        }
    }
}