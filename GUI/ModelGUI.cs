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
        private Shader shader = GenericShaders.Image2D;

        public void RenderModelInformation()
        {
            if (COREMain.GetCurrentObjFromScene == -1)
                return;

            Model model = COREMain.CurrentModel;

            this.Write($"{Math.Round(model.translation.x, 2)} {Math.Round(model.translation.y, 2)} {Math.Round(model.translation.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f), 0.7f);
            this.Write($"{Math.Round(model.Scaling.x, 2)} {Math.Round(model.Scaling.y, 2)} {Math.Round(model.Scaling.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f * 2), 0.7f);
            this.Write($"{Math.Round(model.rotation.x, 2)} {Math.Round(model.rotation.y, 2)} {Math.Round(model.rotation.z, 2)}", (int)(this.Width * 0.05f), (int)(this.Height - COREMain.debugText.characterHeight * 1.1f * 3), 0.7f);
        }
    }
}