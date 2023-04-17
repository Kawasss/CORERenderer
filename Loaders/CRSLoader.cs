using System.Text;
using COREMath;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static void LoadCRS(string path, out List<Model> models, out string header)
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                header = GetString(fs, 100);

                models = new();

                int modelCount = GetInt(fs);
                Console.WriteLine(modelCount);
                for (int i = 0; i < modelCount; i++)
                {
                    //getting the model information
                    string modelName = GetString(fs, 10);

                    Vector3 translation = GetVector3(fs);
                    Vector3 scaling = GetVector3(fs);

                    int submodelCount = GetInt(fs);
                    Console.WriteLine(submodelCount);
                    //getting the submodels
                    for (int j = 0; j < submodelCount; j++)
                    {
                        string submodelName = GetString(fs, 10);
                        Vector3 submodelTranslation = GetVector3(fs);
                        Vector3 submodelScaling = GetVector3(fs);

                        bool hasMaterials = GetBool(fs);

                        int amountPolygons = GetInt(fs);
                        int amountVertices = amountPolygons * 3 * 8; //each polygon has 3 vertices, each vertex has 8 components (xyz, uv xy, normal xyz)
                        Console.WriteLine(amountVertices);
                        List<float> vertices = new();
                        for (int k = 0; k < amountVertices; k++)
                            vertices.Add(GetFloat(fs));

                        models.Add(new());
                        models[^1].type = Main.RenderMode.STLFile;
                        models[^1].submodels.Add(new(submodelName, vertices, translation, submodelScaling, models[^1]));
                        //add the segment for retreiving material values if given
                    }
                }
            }
        }

        private static Vector3 GetVector3(FileStream fs)
        {
            Vector3 vector = Vector3.Zero;

            vector.x = GetFloat(fs);
            vector.y = GetFloat(fs);
            vector.z = GetFloat(fs);

            return vector;
        }

        private static bool GetBool(FileStream fs) => Convert.ToBoolean((byte)fs.ReadByte());

        private static float GetFloat(FileStream fs) => BitConverter.ToSingle(Get4Bytes(fs));

        private static int GetInt(FileStream fs) => BitConverter.ToInt32(Get4Bytes(fs));

        private static string GetString(FileStream fs, int stringLength)
        {
            byte[] headerBytes = new byte[stringLength];
            for (int i = 0; i < stringLength; i++)
                headerBytes[i] = (byte)fs.ReadByte();
        
            return Encoding.UTF8.GetString(headerBytes);
        }

        private static byte[] Get4Bytes(FileStream fs)
        {
            byte[] bytes = new byte[4];

            for (int i = 0; i < 4; i++)
                bytes[i] = (byte)fs.ReadByte();

            return bytes;
        }
    }
}
