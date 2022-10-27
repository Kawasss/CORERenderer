using CORERenderer.GLFW.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer
{
    abstract class Overrides
    {
        public unsafe abstract void OnLoad();

        public unsafe abstract void RenderEveryFrame();

        public unsafe abstract void EveryFrame(Window window, float delta);
    }
}
