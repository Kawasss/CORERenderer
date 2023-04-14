using System.Text;
using COREMath;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static void LoadCRS(string path, out Model[] models, out string header)
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                header = GetString(fs, 100);

                models = new Model[GetInt(fs)];

                for (int i = 0; i < models.Length; i++)
                {
                    //getting the model information
                    string modelName = GetString(fs, 10);
                    Vector3 translation = GetVector3(fs);
                    Vector3 scaling = GetVector3(fs);

                    int submodelCount = GetInt(fs);

                    //getting the submodels
                    for (int j = 0; j < submodelCount; j++)
                    {
                        string submodelName = GetString(fs, 10);
                        Vector3 submodelTranslation = GetVector3(fs);
                        Vector3 submodelScaling = GetVector3(fs);

                        bool hasMaterials = GetBool(fs);
                        bool useGlDrawElements = GetBool(fs);
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

        private static bool GetBool(FileStream fs) => BitConverter.ToBoolean(Get4Bytes(fs));

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
