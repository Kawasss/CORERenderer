using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CORERenderer.Loaders
{
    public class OBJLoader
    {
        public static bool LoadOBJ(string path, out float[] outVertices)//, out Vector3[] vertices, out Vector2[] UVs, out Vector3[] normals)
        {
            List<float> vertices = new();
            List<Vector3> vectorVertices = new();

            List<float> normals = new();
            List<Vector3> vectorNormals = new();

            List<float> UVCoordinates = new();
            List<Vector2> vectorUVCoordinates = new();

            List<int> fValues = new();
            List<Vector3> VectorfValues = new();

            List<string> usemtls = new();

            List<string> stringVertices = new();
            List<string> stringNormals = new();
            List<string> stringUV = new();
            List<string> stringF = new();
            
            List<string> oValues = new();
            List<string> sValues = new();

            List<int> oPositions = new();

            List<float> ambient;
            List<float> diffuse;
            List<float> specular;
            List<float> transparent;
            List<float> shininess;
            List<int> illum;
            List<string> texture;
            List<string> map;

            string mtllib = "Null";

            string[] tempString = File.ReadAllLines(path);

            //maybe could be done better?
            foreach (string n in tempString)
            {
                switch (n[0..2])
                {
                    case "# ": //comment
                        break;

                    case "mt": //mtllib (maybe better way?)
                        mtllib = n[7..]; //"mtllib " is 7 chars long
                        break;

                    case "o ": //texture name
                        oValues.Add(n[2..]);
                        oPositions.Add(Array.FindIndex(tempString, z => z == n));
                        break;

                    case "v ": //vector
                        stringVertices.Add(n);
                        break;

                    case "vn": //vector normal
                        stringNormals.Add(n);
                        break;

                    case "vt": //UV coordinates
                        stringUV.Add(n);
                        break;

                    case "s ": //s value
                        sValues.Add(n[2..]);
                        break;

                    case "us": //usemtl (maybe better way?)
                        usemtls.Add(n[7..]); //"usemtl " is 7 chars long
                        break;

                    case "f ": //v / vn / vt indicator
                        stringF.Add(n);
                        break;

                    default:
                        throw new Exception($"Couldn't read '{n}' (indication of type of data not recognized)");
                }
            }

            //below not compatible with o's yet
            ValueReader(stringVertices, out vertices, out vectorVertices);
            ValueReader(stringNormals, out normals, out vectorNormals);
            ValueReader(stringUV, out UVCoordinates, out vectorUVCoordinates);

            FReader(stringF, out fValues, out VectorfValues);

            outVertices = new float[VectorfValues.Count * 8];
            Console.WriteLine(VectorfValues.Count);
            int t = 0;
            for (int i = 0; i < VectorfValues.Count; i++)
            {
                outVertices[t] = vectorVertices[(int)VectorfValues[i].x - 1].x;
                outVertices[t + 1] = vectorVertices[(int)VectorfValues[i].x - 1].y;
                outVertices[t + 2] = vectorVertices[(int)VectorfValues[i].x - 1].z;

                outVertices[t + 3] = vectorNormals[(int)VectorfValues[i].z - 1].x;
                outVertices[t + 4] = vectorNormals[(int)VectorfValues[i].z - 1].y;
                outVertices[t + 5] = vectorNormals[(int)VectorfValues[i].z - 1].z;
                
                outVertices[t + 6] = vectorUVCoordinates[(int)VectorfValues[i].y - 1].x;
                outVertices[t + 7] = vectorUVCoordinates[(int)VectorfValues[i].y - 1].y;
                t += 8;
            }

            return true;
        }

        //need to make readable with commentary asap, will forget how it works in a week
        private static void FReader(List<string> stringList, out List<int> intList, out List<Vector3> vectorList)
        {
            intList = new();
            vectorList = new();

            foreach (string n in stringList)
            {
                List<int> local = new();
                List<int> local2;
                List<string> local3 = new();

                //isolates the indices into ../../.. then by / so the int value remains
                for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                {
                    local.Add(i);
                }
                
                //isolates the indices to ../../.. by using the indexes of the surrounding " "'s
                for (int i = 0; i < local.Count - 1; i++) 
                {
                    local3.Add( n[ ( local[i] + 1 ) .. local[ i + 1 ]] );
                }
                local3.Add( n[( local[ local.Count - 1 ] + 1) .. n.Length] );

                //isolates each int from local (../../..) and parses it
                foreach (string s in local3)
                {
                    local2 = new(); //new list because otherwise its .Count is too big for it too functionally handle

                    for (int i = s.IndexOf("/"); i > -1; i = s.IndexOf("/", i + 1))
                    {
                        local2.Add(i);
                    }
                    //isolates the int values by taking the space between the / indexes
                    intList.Add(int.Parse(s[..local2[0]], CultureInfo.InvariantCulture));
                    intList.Add(int.Parse(s[(local2[0] + 1)..local2[1]], CultureInfo.InvariantCulture));
                    intList.Add(int.Parse(s[( local2[local2.Count - 1] + 1 ) .. s.Length], CultureInfo.InvariantCulture));  
                }
            }
            for (int i = 0; i < intList.Count; i += 3)
            {
                vectorList.Add(new Vector3(intList[i], intList[i + 1], intList[i + 2]));
            }
        }

        //finds the places of the values, isolates them and converts them to be usable
        private static void ValueReader(List<string> stringList, out List<float> floatList, out List<Vector3> vectorList)
        {
            floatList = new();
            vectorList = new();

            foreach (string n in stringList)
            {
                int[] local = new int[3];
                int z = 0;
                for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                {
                    local[z] = i;
                    z++;
                }
                floatList.Add(float.Parse(n[local[0]..local[1]], CultureInfo.InvariantCulture));
                floatList.Add(float.Parse(n[local[1]..local[2]], CultureInfo.InvariantCulture));
                floatList.Add(float.Parse(n[local[2]..n.Length], CultureInfo.InvariantCulture)); 
            }

            //previous 3 lines compressed into one for a new vector
            for (int i = 0; i < floatList.Count; i += 3)
            {
                vectorList.Add(new Vector3(floatList[i], floatList[i + 1], floatList[i + 2]));
            }
        }

        private static void ValueReader(List<string> stringList, out List<float> floatList, out List<Vector2> vectorList)
        {
            floatList = new();
            vectorList = new();

            foreach (string n in stringList)
            {
                int[] local = new int[3];
                int z = 0;
                for (int i = n.IndexOf(" "); i > -1; i = n.IndexOf(" ", i + 1))
                {
                    local[z] = i;
                    z++;
                }
                floatList.Add(float.Parse(n[local[0]..local[1]], CultureInfo.InvariantCulture));
                floatList.Add(float.Parse(n[local[1]..n.Length], CultureInfo.InvariantCulture));
            }

            //previous 3 lines compressed into one for a new vector
            for (int i = 0; i < floatList.Count; i += 2)
            {
                vectorList.Add(new Vector2(floatList[i], floatList[i + 1]));
            }
        }
    }
}
 