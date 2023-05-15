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
            if (type == RenderMode.ObjFile || type == RenderMode.STLFile || type == RenderMode.JPGImage || type == RenderMode.PNGImage)
                RenderModel();

            else if (type == RenderMode.HDRFile)
                hdr.Render();
        }

        public void RenderBackground() => hdr.Render();

        private unsafe void RenderModel()
        {
            for (int i = 0; i < submodels.Count; i++)
            {
                submodels[i].renderLines = renderLines;

                if (submodels[i].highlighted)
                    selectedSubmodel = i;

                if (submodels[i].isTranslucent)
                {
                    translucentSubmodels.Add(submodels[i]);
                    continue;
                }
                if (MathC.Distance(Main.COREMain.CurrentScene.camera.position, transform.translation) <= 10)
                {
                    translucentSubmodels.Add(submodels[i]);
                    continue;
                }
                submodels[i].Render();
            }
        }
    }
}
