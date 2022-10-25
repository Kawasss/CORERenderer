using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.Loaders
{
    internal class OBJLoader
    {
        static List<int> oIndexes = new List<int>();
        static List<int> vectorIndexes = new List<int>();
        static List<int> textureIndexes = new List<int>();
        static List<int> normalIndexes = new List<int>();
        static List<int> fIndexes = new List<int>();

        static List<string> test = new List<string>();

        public static bool LoadOBJ(string path)//, out Vector3[] vertices, out Vector2[] UVs, out Vector3[] normals)
        {
            int[] vertexIndices, uvIndices, normalIndices;
            Vector3[] tempVertices, tempNormals;
            Vector2[] tempUVs;

            string file = File.ReadAllText(path);
            GetAllIndexes(file);
            int i = 1;
            int j = 0;
            int k = 1;
            
            //uses all of the indexes to seperate the strings
            foreach (int n in vectorIndexes)
            {
                //adds the last v value to the list
                if (i >= vectorIndexes.Count)
                {
                    while (normalIndexes[j] < n)
                    {
                        j++;
                    }
                    test.Add(file[n..normalIndexes[j]]);
                } else
                {
                    //adds the last value of current row of v's (before the file continues with vn)
                    while (normalIndexes[k] < n)
                    {
                        k++;
                    }
                    //if the current row of v's has ended add the last value (done by checking if the next v is further away then the next vn)
                    if (n < normalIndexes[k] && normalIndexes[k] < vectorIndexes[i])
                    {
                        test.Add(file[n..normalIndexes[k - 1]]);
                    }
                    else
                    {
                        test.Add(file[n..vectorIndexes[i]]);
                    }
                    i++;
                }
            }

            foreach (string n in test)
            {
                Console.Write(n);
            }

            return false;
        }

        public static void GetAllIndexes(string fileContent)
        {
            for (int i = fileContent.IndexOf("\no "); i > -1; i = fileContent.IndexOf("\no ", i + 1))
            {
                oIndexes.Add(i);
            }
            for (int i = fileContent.IndexOf("\nv "); i > -1; i = fileContent.IndexOf("\nv ", i + 1)) {
                vectorIndexes.Add(i);
            }
            for (int i = fileContent.IndexOf("\nvt "); i > -1; i = fileContent.IndexOf("\nvt ", i + 1))
            {
                textureIndexes.Add(i);
            }
            for (int i = fileContent.IndexOf("vn "); i > -1; i = fileContent.IndexOf("vn ", i + 1))
            {
                normalIndexes.Add(i);
            }
            for (int i = fileContent.IndexOf("f "); i > -1; i = fileContent.IndexOf("f ", i + 1))
            {
                fIndexes.Add(i);
            }
        } 
    }
}
