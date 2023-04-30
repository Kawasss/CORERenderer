using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.Loaders
{
    public partial class Model
    {
        public void Render()
        {
            highlighted = COREMain.selectedID == ID;

            if (type == RenderMode.ObjFile || type == RenderMode.STLFile || type == RenderMode.JPGImage || type == RenderMode.PNGImage)
                RenderModel();

            else if (type == RenderMode.HDRFile)
                Rendering.RenderBackground(hdr);
        }

        public void RenderBackground() => Rendering.RenderBackground(hdr);

        private unsafe void RenderModel()
        {
            for (int i = 0; i < submodels.Count; i++)
            {
                submodels[i].renderLines = renderLines;

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
    }
}
