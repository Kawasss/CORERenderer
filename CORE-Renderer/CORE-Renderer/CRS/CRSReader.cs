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
            CRS newCRS = new(name, path, File.ReadAllLines($"{path}\\{name}.cst"), fileStream);

            //finds the amount of objects in the scene and the place of their information in the .cst file
            string[] allLines = File.ReadAllLines($"{path}\\{name}.cst");

            int amountOfObjects = 0;
            List<int> ObjectLocations = new();
            for (int i = 0; i < allLines.Length; i++)
                if (allLines[i].Length >= 4 && allLines[i][..4] == "<obj")
                {
                    ObjectLocations.Add(i);
                    amountOfObjects++;
                }

            List<string> materialNames = new();
            for (int i = 0; i < amountOfObjects; i++)
            {
                //gets all of the material names for the current obj file so that the mtl file can be read
                string[] local = File.ReadAllLines($"{path}\\{i}.cv");
                for (int j = 0; j < local.Length; j++)
                    if (local[j].Length > 2 && local[j][..2] == "<v")
                    {
                        int index = local[j].IndexOf("materialName = \"");
                        materialNames.Add(local[j][(index + 1)..local[j].IndexOf('"', index + 1)]); //maybe index + 1 if error?
                    }
                newCRS.allOBJs.Add(new($"{path}\\{i}.mtl", materialNames));

                //reads and writes all of the vertice values of one group per loop to the current Obj
                string[] group;
                if (i != ObjectLocations.Count - 1)
                    group = local[(ObjectLocations[i] + 1)..(ObjectLocations[i + 1] - 1)]; //maybe ObjectLocations[i] + 1 if error?
                else //does last object seperately because out of bounds error
                    group = local[(ObjectLocations[i] + 1)..];

                for (int j = 0; j < materialNames.Count; j++) //uses amount of materials since its material has its own vertice group
                {   //loops through all the vertices in a group / material and parses it to the current obj's vertice group
                    newCRS.allOBJs[i].vertices.Add(new());
                    for (int k = 0; k < group.Length; k++)
                        if (group[k][0] != '<')
                            newCRS.allOBJs[i].vertices[j].Add(float.Parse(group[k]));
                        
                }
                //same as above, just with the indice groups
                local = File.ReadAllLines($"{path}\\{i}.ci");

                if (i != ObjectLocations.Count - 1)
                    group = local[(ObjectLocations[i] + 1)..(ObjectLocations[i + 1] - 1)]; //maybe ObjectLocations[i] + 1 if error?
                else
                    group = local[(ObjectLocations[i] + 1)..];

                for (int j = 0; j < materialNames.Count; j++)
                {
                    newCRS.allOBJs[i].indices.Add(new());
                    for (int k = 0; k < group.Length; k++)
                        if (group[k][0] != '<')
                            newCRS.allOBJs[i].indices[j].Add(uint.Parse(group[k]));
                }
            }
            for (int i = 0; i < newCRS.allOBJs.Count; i++)
                newCRS.allOBJs[i].GenerateBuffers();
            newCRS.nextUnusedID = amountOfObjects; //sets the next unused id correctly given the amount of objects

            return newCRS;
        }

        /// <summary>
        /// gets the float value of the variable on the given line 
        /// </summary>
        /// <param name="linePlace">place of the variable</param>
        /// <returns>float value assigned to the variable</returns>
        private float GetObjFloat(int linePlace)
        {   //gets a float by using the syntax of the .cst
            int local2 = this.cstLines[linePlace].IndexOf('=');
            int local3 = this.cstLines[linePlace].IndexOf(';');

            return float.Parse(this.cstLines[linePlace][(local2 + 1)..local3], CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// gets the vector3 value of the variable on the given line 
        /// </summary>
        /// <param name="linePlace">place of the variable</param>
        /// <returns>vector3 value assigned to the variable</returns>
        private Vector3 GetObjVector3(int linePlace)
        {   //gets a vector3 by using the syntax of the .cst
            int local2 = this.cstLines[linePlace].IndexOf('=');
            int local3 = this.cstLines[linePlace].IndexOf(';');
            int local4 = this.cstLines[linePlace].IndexOf(',');
            int local5 = this.cstLines[linePlace].IndexOf(',', local4 + 1);

            return new(this.cstLines[linePlace][(local2 + 1)..local4],
                       this.cstLines[linePlace][(local4 + 1)..local5],
                       this.cstLines[linePlace][(local5 + 1)..local3]);
        }

        public void AssignVertices()
        {
            int local3 = -1;
            int local4 = -1;
            //loops everything so that it does every single .obj in the crs file
            for (int i = 0; i < allOBJs.Count; i++)
            {
                int ID = nameIDBinder[allOBJs[i].name];
                //loops everything so that it does every vertice group of an .csv file
                for (int l = 0; l < allObjectInstances[i].amountOfVerticeGroups; l++)
                {
                    //assigns all the floats for each vertice group
                    allOBJs[i].vertices.Add(new());
                    string[] local2 = File.ReadAllLines(allObjectInstances[i].csvPath);

                    //finds all of the float values of the current vertice group
                    local3 = Array.FindIndex(local2, local3 + 1, z => z.Equals($"<vertices id = {ID}>"));
                    local4 = Array.FindIndex(local2, local4 + 1, z => z.Equals($"</vertices>"));
                    string[] local5 = local2[local3..local4]; //reads all of the float values

                    //parses and adds the current float value to the vertice group
                    for (int j = 0; j < local5.Length; j++)
                    {
                        if (local5[j][0] == '<') //breaks if the given string is "<vertices id = {ID}>" or "</vertices>"
                            break;
                        allOBJs[i].vertices[l].Add(float.Parse(local5[j]));
                    }
                }
            }
        }

        public void AssignIndices()
        {
            int local3 = -1;
            int local4 = -1;
            //loops everything so that it does every single .obj in the crs file
            for (int i = 0; i < allOBJs.Count; i++)
            {
                int ID = nameIDBinder[allOBJs[i].name];
                //loops everything so that it does every vertice group of an .csv file
                for (int l = 0; l < allObjectInstances[i].amountOfIndiceGroups; l++)
                {
                    //assigns all the floats for each vertice group
                    allOBJs[i].indices.Add(new());
                    string[] local2 = File.ReadAllLines(allObjectInstances[i].csiPath);

                    //finds all of the float values of the current vertice group
                    local3 = Array.FindIndex(local2, local3 + 1, z => z.Equals($"<indices id = {ID}>"));
                    local4 = Array.FindIndex(local2, local4 + 1, z => z.Equals($"</indices>"));
                    string[] local5 = local2[local3..local4]; //reads all of the float values

                    //parses and adds the current float value to the vertice group
                    for (int j = 0; j < local5.Length; j++)
                    {
                        if (local5[j][0] == '<') //breaks if the given string is "<vertices id = {ID}>" or "</vertices>"
                            break;
                        allOBJs[i].indices[l].Add(uint.Parse(local5[j]));
                    }
                }
            }
        }
    }
}
