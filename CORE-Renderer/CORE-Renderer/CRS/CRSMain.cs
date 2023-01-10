using CORERenderer.Loaders;
using CORERenderer.Main;

namespace CORERenderer.CRSFile
{
    public partial class CRS
    {
        public string name = null;
        public string path = null;
        public int nextUnusedID;
        public FileStream cstFile;

        public List<Obj> allOBJs = new();
        private Dictionary<string, int> nameIDBinder = new();

        
        public List<ObjectInstance> allObjectInstances = new();

        CRS(string name, string path, FileStream cstFile)
        {   //sets all the CRS information
            this.name = name;
            this.path = path;
            this.cstFile = cstFile;
            nextUnusedID = 0;
        }

        private void UpdateIDs()
        {   //binds the current ID to the name of the current obj
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

        public static CRS LoadCRS(string path, string name)
        {
            if (Directory.Exists(path))
            {
                //COREMain.splashScreen.WriteLine("Found file, reading..");
                Console.WriteLine("Found file, reading..");
                return ReadCRS(path);
            }
            else
            {
                //COREMain.splashScreen.WriteLine("Creating new file..");
                Console.WriteLine("Creating new file..");
                return GenerateCRS(path, name);
            }
        } 

        
    }
}