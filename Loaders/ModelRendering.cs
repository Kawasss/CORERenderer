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
            RenderModel();
        }

        public void RenderShadow()
        {
            foreach (Submodel submodel in submodels)
                submodel.RenderShadowVersion();
        }

        public void RenderID()
        {
            foreach (Submodel submodel in submodels)
                submodel.RenderIDVersion();
        }

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
                submodels[i].Render();
            }
        }
    }
}
