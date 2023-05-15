using CORERenderer.OpenGL;
using System.Text;

namespace CORERenderer.Loaders
{
    public partial class Writers
    {
        /// <summary>
        /// Generates an .stl file in the given directory
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="header"></param>
        /// <param name="model"></param>
        public static void GenerateSTL(string directoryPath, string header, Model model) //generates an .stl file according to the official format (80 bytes header, 4 bytes for triangle amount, 50 bytes for each face (12 for normal, 36 for vertices and 2 for attributes)
        {
            List<List<float>> vertices = new();
            List<List<Vertex>> v = model.Vertices;
            for (int i = 0; i < v.Count; i++)
            {
                vertices.Add(new());
                for (int j = 0; j < v[i].Count; j++)
                {
                    vertices[i].Add(v[i][j].x); vertices[i].Add(v[i][j].y); vertices[i].Add(v[i][j].z);
                    vertices[i].Add(v[i][j].uvX); vertices[i].Add(v[i][j].uvY);
                    vertices[i].Add(v[i][j].normalX); vertices[i].Add(v[i][j].normalY); vertices[i].Add(v[i][j].normalZ);
                }
            }
                
            if (model.type == Main.RenderMode.ObjFile)
                new Job(() => GenerateSTL(directoryPath, model.Name, header, vertices, model.Offsets)).Start();
            else if (model.type == Main.RenderMode.STLFile)
                new Job(() => GenerateSTL(directoryPath, model.Name, header, vertices)).Start();
        }

        /// <summary>
        /// This method assumes that the model doesn't use indices
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="name"></param>
        /// <param name="header"></param>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static void GenerateSTL(string directoryPath, string name, string header, List<float> vertices)
        {
            if (!Directory.Exists(directoryPath) || File.Exists($"{directoryPath}\\{name}.stl"))
                return;

            byte[] headerBytes = GenerateHeader(header, 80);
            byte[] triangleAmount = BitConverter.GetBytes(vertices.Count / 24);
            
            using (FileStream fs = File.Create($"{directoryPath}\\{name}.stl"))
            using (BufferedStream bs = new(fs))
            using (StreamWriter sw = new(bs))
            {
                sw.BaseStream.Write(headerBytes);
                sw.BaseStream.Write(triangleAmount);

                for (int i = 0; i < vertices.Count; i += 24)
                {
                    byte[] normalX = BitConverter.GetBytes(vertices[i + 5]); //the normal starts at the sixth float, an entire vertex looks like this: COOR COOR COOR UV UV NORM NORM NORM                                                   
                    byte[] normalY = BitConverter.GetBytes(vertices[i + 6]);
                    byte[] normalZ = BitConverter.GetBytes(vertices[i + 7]);

                    //sw.BaseStream.Write(normalInBytes);
                    sw.BaseStream.Write(normalX);
                    sw.BaseStream.Write(normalY);
                    sw.BaseStream.Write(normalZ);

                    for (int j = 0; j < 3; j++)
                    {
                        byte[] vertexX = BitConverter.GetBytes(vertices[i + j * 8]);
                        byte[] vertexY = BitConverter.GetBytes(vertices[i + j * 8 + 1]);
                        byte[] vertexZ = BitConverter.GetBytes(vertices[i + j * 8 + 2]);

                        sw.BaseStream.Write(vertexX);
                        sw.BaseStream.Write(vertexY);
                        sw.BaseStream.Write(vertexZ);
                    }

                    byte[] attributeBytes = new byte[2] { 0x0, 0x0 }; //attribute bytes are optional, so theyre left empty. Could be used to save the transparency of an objects
                    sw.BaseStream.Write(attributeBytes);
                }
            }
        }

        public static void GenerateSTL(string directoryPath, string name, string header, List<List<float>> vertices)
        {
            List<float> allVertices = new();
            foreach(List<float> list in vertices) //simplest way of adding all vertices together
                foreach (float vertex in list)
                    allVertices.Add(vertex);

            GenerateSTL(directoryPath, name, header, allVertices);
        }

        public static void GenerateSTL(string directoryPath, string name, string header, List<List<float>> vertices, List<COREMath.Vector3> offsets)
        {
            List<float> newVertices = new();
            for (int j = 0; j < vertices.Count; j++)
            {
                for (int i = 0; i < vertices[j].Count; i += 8) //adds the offset to the vertices 
                {
                    newVertices.Add(vertices[j][i] + offsets[0].x);
                    newVertices.Add(vertices[j][i + 1] + offsets[0].y);
                    newVertices.Add(vertices[j][i + 2] + offsets[0].z);
                    for (int k = 0; k < 5; k++)
                        newVertices.Add(vertices[j][i + 3 + k]);
                }
            }
            GenerateSTL(directoryPath, name, header, newVertices);
        }

        public static void GenerateSTL(string directoryPath, string name, string header, List<List<float>> vertices, List<List<uint>> indices, List<COREMath.Vector3> offsets)
        { //mess of a code below, dont question it so long as it works
            List<List<List<float>>> oneVertex = new(); //seperates all of the vertex values in this hierarchy: all vertice groups -> all vertices in vertice group -> vertex
            for (int i = 0; i < vertices.Count; i++)
            {
                oneVertex.Add(new());
                for (int j = 0; j < vertices[i].Count; j++) //divides all of the vertices into one of each, so that they can be combined with the indices
                {
                    if (j % 8 == 0)
                        oneVertex[^1].Add(new());
                    oneVertex[^1][^1].Add(vertices[i][j]);
                }
            }

            List<float> allVertices = new();

            for (int i = 0; i < oneVertex.Count; i++) //inefficient since its not together with the for loops below but its easier to work with
                for (int j = 0; j < oneVertex[i].Count; j++)
                {
                    oneVertex[i][j][0] += offsets[0].x;
                    oneVertex[i][j][1] += offsets[0].y;
                    oneVertex[i][j][2] += offsets[0].z;
                }

            for (int i = 0; i < indices.Count; i++)
                for (int j = 0; j < indices[i].Count; j++)
                    for (int k = 0; k < 8; k++) //gets the vertex thats bound to the an indice
                        allVertices.Add(oneVertex[i][(int)indices[i][j]][k]);

            GenerateSTL(directoryPath, name, header, allVertices);
        }

        /// <summary>
        /// generates a header with a given length, based on the given string
        /// </summary>
        /// <param name="headerText"></param>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        private static byte[] GenerateHeader(string headerText, int sizeInBytes)
        {
            byte[] fullHeader = Encoding.UTF8.GetBytes(headerText);

            if (fullHeader.Length > sizeInBytes) //stl headers cant be longer than 80 bytes
                fullHeader = fullHeader[..sizeInBytes];
            else
            {   //adds empty space until the entire header is filled
                byte[] returnHeader = new byte[sizeInBytes];
                for (int i = 0; i < fullHeader.Length; i++)
                    returnHeader[i] = fullHeader[i];
                for (int i = fullHeader.Length; i < sizeInBytes; i++)
                    returnHeader[i] = 0x0;
                fullHeader = returnHeader;
            }
            return fullHeader;
        }
    }
}