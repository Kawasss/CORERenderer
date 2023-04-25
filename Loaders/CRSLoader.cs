using System.Text;
using COREMath;
using CORERenderer.Main;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public const string CURRENT_VERSION = "v1.0";

        public static Error LoadCRS(string path, out List<Model> models, out string header)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    header = GetString(fs, 100);
                    
                    models = new();

                    if (!header.Contains(CURRENT_VERSION))
                        return Error.Outdated;

                    int modelCount = GetInt(fs);

                    for (int i = 0; i < modelCount; i++)
                    {
                        RetrieveModelNode(fs, out string modelName, out Vector3 translation, out Vector3 scaling, out Vector3 rotation, out int submodelCount);

                        models.Add(new());
                        models[^1].Name = modelName;
                        models[^1].translation = translation;
                        models[^1].Scaling = scaling;
                        models[^1].rotation = rotation;

                        //getting the submodels
                        for (int j = 0; j < submodelCount; j++)
                        {
                            RetrieveSubmodelNode(fs, out string submodelName, out Vector3 submodelTranslation, out Vector3 submodelScaling, out Vector3 submodelRotation, out bool hasMaterial, out int amountPolygons);

                            int amountVertices = amountPolygons * 3 * 8; //each polygon has 3 vertices, each vertex has 8 components (xyz, uv xy, normal xyz)
                            List<float> vertices = RetrieveVertices(fs, amountVertices);

                            //add the segment for retreiving material values if given
                            if (hasMaterial)
                            {
                                Material material = RetrieveMaterialNode(fs);

                                models[^1].type = RenderMode.ObjFile;
                                models[^1].submodels.Add(new(submodelName, vertices, submodelTranslation, submodelScaling, submodelRotation, models[^1], material));
                            }
                            else
                            {
                                models[^1].type = RenderMode.STLFile;
                                models[^1].submodels.Add(new(submodelName, vertices, submodelTranslation, submodelScaling, models[^1]));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                models = new();
                header = "";
                return Error.InvalidContents;
            }
            return Error.None;
        }

        private static Material RetrieveMaterialNode(FileStream fs)
        {
            string materialName = GetString(fs, 10);
            float shininess = GetFloat(fs);
            float transparency = GetFloat(fs);
            Vector3 ambient = GetVector3(fs);

            //retrieve all materials
            RetrieveTextureNode(fs, out Vector3 diffuseStrength, out byte[] diffusePNGData);
            Texture difTex = GenerateTextureFromData(diffusePNGData);
            Globals.usedTextures.Add(difTex);
            int difIndex = Globals.usedTextures.Count - 1;

            RetrieveTextureNode(fs, out Vector3 specularStrength, out byte[] specularPNGData);
            Texture specTex = GenerateTextureFromData(specularPNGData);
            Globals.usedTextures.Add(specTex);
            int specIndex = Globals.usedTextures.Count - 1;

            RetrieveTextureNode(fs, out byte[] normalPNGData);
            Texture normTex = GenerateTextureFromData(normalPNGData);
            Globals.usedTextures.Add(normTex);
            int normIndex = Globals.usedTextures.Count - 1;

            return new() { Name = materialName, Shininess = shininess, Ambient = ambient, Diffuse = diffuseStrength, Specular = specularStrength, Transparency = transparency, Texture = difIndex, DiffuseMap = difIndex, SpecularMap = specIndex, NormalMap = normIndex };
        }

        private static Texture GenerateTextureFromData(byte[] imageData) //its incredible slow to create and delete a file
        {
            string dir = Path.GetTempPath();
            using (FileStream fs = File.Create($"{dir}diffuseHolder.png"))
            using (StreamWriter sw = new(fs))
            {
                sw.BaseStream.Write(imageData);
            }
            Texture tex = Texture.ReadFromFile($"{dir}diffuseHolder.png");
            File.Delete($"{dir}diffuseHolder.png");
            return tex;
        }

        private static void RetrieveTextureNode(FileStream fs, out Vector3 textureStrength, out byte[] imageData)
        {
            textureStrength = GetVector3(fs);
            int length = GetInt(fs);
            imageData = new byte[length];

            for (int i = 0; i < length; i++)
                imageData[i] = (byte)fs.ReadByte();
        }

        private static void RetrieveTextureNode(FileStream fs, out byte[] imageData)
        {
            int length = GetInt(fs);
            imageData = new byte[length];

            for (int i = 0; i < length; i++)
                imageData[i] = (byte)fs.ReadByte();
        }

        private static List<float> RetrieveVertices(FileStream fs, int amountVertices)
        {
            List<float> vertices = new();
            for (int k = 0; k < amountVertices; k++)
                vertices.Add(GetFloat(fs));

            return vertices;
        }

        private static void RetrieveSubmodelNode(FileStream fs, out string name, out Vector3 translation, out Vector3 scaling, out Vector3 rotation, out bool hasMaterial, out int amountPolygons)
        {
            name = GetString(fs, 10);
            translation = GetVector3(fs);
            scaling = GetVector3(fs);
            rotation = GetVector3(fs);

            hasMaterial = GetBool(fs);

            amountPolygons = GetInt(fs);
        }

        private static void RetrieveModelNode(FileStream fs, out string name, out Vector3 translation, out Vector3 scaling, out Vector3 rotation, out int submodelCount)
        {
            name = GetString(fs, 10);

            translation = GetVector3(fs);
            scaling = GetVector3(fs);
            rotation = GetVector3(fs);

            submodelCount = GetInt(fs);
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