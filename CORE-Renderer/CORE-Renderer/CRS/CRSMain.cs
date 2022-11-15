using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CORERenderer.Loaders;
using CORERenderer;

namespace CORERenderer.CRS
{
    public partial class CRS
    {
        public string name = null;
        public string path = null;
        public int nextUnusedID;
        public FileStream cstFile;
        
        private string[] cstLines;

        public List<Obj> allOBJs = new();
        private Dictionary<string, int> nameIDBinder = new();

        public struct ObjectInstance
        {
            public ObjectInstance(FileStream csv, FileStream csi, string csvP, string csiP, int amountVerticeGroups, int amountIndiceGroups)
            {
                csvFile = csv;
                csiFile = csi;
                csvPath = csvP;
                csiPath = csiP;
                amountOfVerticeGroups = amountVerticeGroups;
                amountOfIndiceGroups = amountIndiceGroups;
            }
            public FileStream csvFile;
            public FileStream csiFile;
            public string csvPath;
            public string csiPath;
            public int amountOfVerticeGroups;
            public int amountOfIndiceGroups;
        }
        public List<ObjectInstance> allObjectInstances = new();

        CRS(string name, string path, string[] cstLines, FileStream cstFile)
        {
            this.name = name;
            this.path = path;
            this.cstFile = cstFile;
            this.cstLines = cstLines;
            nextUnusedID = 0;
        }

        private void UpdateIDs()
        {
            if (nameIDBinder.Count != 0)
            {
                for (int i = 0; i < allOBJs.Count; i++)
                {
                    if (nameIDBinder.ContainsKey(allOBJs[i].name))
                        return;
                    else
                        nameIDBinder.Add(allOBJs[i].name, nextUnusedID);
                }
            }
            else
                nameIDBinder.Add(allOBJs[0].name, nextUnusedID);   
        }
    }
}
