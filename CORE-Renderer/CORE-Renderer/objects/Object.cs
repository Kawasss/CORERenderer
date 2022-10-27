using CORERenderer.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.Bodies
{
    public class Body
    {
        public Body(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception($"file {path} not found (invalid path)");
            }
            //OBJLoader.LoadOBJ(path);

            int z = 0;
            List<int> local = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                local.Add(i);
                z++;
            }
            string rootOBJ = path[..local[z - 1]];

            /*
             check if mtl file is valid here
            */

        }
    }
}
