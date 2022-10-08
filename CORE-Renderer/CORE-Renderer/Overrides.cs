using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace openGLToturial
{
    abstract class Overrides
    {
        public unsafe abstract void OnLoad();

        public unsafe abstract void RenderEveryFrame();
    }
}
