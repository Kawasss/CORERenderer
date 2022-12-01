using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using COREMath;
using CORERenderer.Loaders;

namespace CORERenderer.CRS
{
    public partial class CRS
    {
        public static CRS ReadCRS(string path)
        {
            //finds the name of the file from the given path
            List<int> temp = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);

            string name = path[(temp[^1] + 1)..^4];

            FileStream fileStream = File.OpenRead($"{path}\\{name}.cst");
            fileStream.Close();
            CRS newCRS = new(name, path, fileStream);

            return newCRS;
        }
    }
}
