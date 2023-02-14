using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.GUI
{
    public class Mouse
    {
        public float  x;
        public float y;

        public int ID;

        private float depth;

        private int sx = 0;
        private int sy = 0;

        public unsafe void Pick(int x, int y)
        {
            this.x = x;
            this.y = y;//sy - 1 - 

            if (x == 0)
                this.x += 1;
            if (y == 0)
                this.y += 1;

            float pixels;
            glReadPixels((int)this.x, (int)this.y, 1, 1, GL_DEPTH_COMPONENT, GL_FLOAT, &pixels);

            //camera near = 0.01f, camera far = 1000
            this.depth = pixels;
            this.depth = 2 * this.depth - 1; //NDC
            this.depth = (0.2f * 0.01f) / (1000f + 0.01f - (this.depth * (1000f - 0.01f))); //linear 0 - 1
            this.depth = 0.01f + this.depth * (1000f - 0.01f); //linear near - far

            int id;
            glReadPixels((int)this.x, (int)this.y, 1, 1, GL_STENCIL_INDEX, GL_INT, &id);
            this.ID = id;

            //this.x = (2 * (this.x - 0) / x) - 1; //???? supposed to be NDC -1 - 1
            //this.y = 1 - (2 * (this.y - 0) / y);
        }
    }
}
