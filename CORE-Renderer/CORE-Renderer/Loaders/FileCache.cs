using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.Loaders
{
    public class FileCache
    {
        public FileCache(string path)
        {
            this.path = path;
        }

        private readonly string path;

        /// <summary>
        /// Gets the line thats at the place of the given int
        /// </summary>
        /// <param name="lineNumber">index that starts at 0</param>
        /// <returns>string at the given index</returns>
        public string GetLine(int lineNumber)
        {
            using StreamReader sr = new(path);
            return File.ReadLines(path).Skip(lineNumber).Take(1).First();
            /*for (int i = 0; i < lineNumber; i++)
                sr.ReadLine();
            return sr.ReadLine();*/
        }
    }
}
