using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.CRS
{
    public partial class CRS
    {
        public static bool ReadCRS(string path)
        {
            if (!File.Exists(path))
                return false;

            List<int> nameFinder = new();
            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                nameFinder.Add(i);
            string filename = path[(nameFinder[^1] + 1)..];

            if (filename[^4..].ToLower() != ".crs")
            {
                Console.WriteLine($"invalid file {filename}");
                return false;
            }

            return true;
        } 
    }
}
