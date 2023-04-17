using System.Text;
using COREMath;
using CORERenderer.Main;
using CORERenderer.textures;

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
                    RetrieveModelNode(fs, out string modelName, out Vector3 translation, out Vector3 scaling, out int submodelCount);

                    models.Add(new());
                    models[^1].name = modelName;
                    models[^1].translation = translation;
                    models[^1].Scaling = scaling;
                    Console.WriteLine($"model: {translation}");
                    Console.WriteLine(submodelCount);
                    //getting the submodels
                    for (int j = 0; j < submodelCount; j++)
                    {
                        RetrieveSubmodelNode(fs, out string submodelName, out Vector3 submodelTranslation, out Vector3 submodelScaling, out bool hasMaterial, out int amountPolygons);

                        int amountVertices = amountPolygons * 3 * 8; //each polygon has 3 vertices, each vertex has 8 components (xyz, uv xy, normal xyz)
                        List<float> vertices = RetrieveVertices(fs, amountVertices);

                        //add the segment for retreiving material values if given
                        if (hasMaterial)
                        {
                            string materialName = GetString(fs, 10);
                            float shininess = GetFloat(fs);
                            float transparency = GetFloat(fs);
                            Vector3 ambient = GetVector3(fs);

                            //retrieve the diffuse texture
                            RetrieveTextureNode(fs, out Vector3 diffuseStrength, out int textureWidth, out int textureHeight, out byte[] imageData);
                            Texture difTex = new("diffuse", textureWidth, textureHeight, imageData);
                            Globals.usedTextures.Add(difTex);
                            int difIndex = Globals.usedTextures.IndexOf(difTex);

                            //retrieve the specular texture
                            RetrieveTextureNode(fs, out Vector3 specularStrength, out textureWidth, out textureHeight, out imageData);
                            Texture specTex = new("specular", textureWidth, textureHeight, imageData);
                            Globals.usedTextures.Add(specTex);
                            int specIndex = Globals.usedTextures.IndexOf(specTex);

                            //retrieve the normal texture
                            RetrieveTextureNode(fs, out textureWidth, out textureHeight, out imageData);
                            Texture normTex = new("normal", textureWidth, textureHeight, imageData);
                            Globals.usedTextures.Add(normTex);
                            int normIndex = Globals.usedTextures.IndexOf(normTex);

                            Material material = new();
                            material.Name = materialName;
                            material.Shininess = shininess;
                            material.Ambient = ambient;
                            material.Diffuse = diffuseStrength;
                            material.Specular = specularStrength;
                            material.Transparency = transparency;
                            material.Texture = difIndex;
                            material.DiffuseMap = difIndex;
                            material.SpecularMap = specIndex;
                            material.NormalMap = normIndex;
                            Console.WriteLine($"submodel: {submodelTranslation}");
                            models[^1].type = RenderMode.ObjFile;
                            models[^1].submodels.Add(new(submodelName, vertices, submodelTranslation, submodelScaling, models[^1], material));
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

        private static void RetrieveTextureNode(FileStream fs, out Vector3 textureStrength, out int textureWidth, out int textureHeight, out byte[] imageData)
        {
            textureStrength = GetVector3(fs);
            textureWidth = GetInt(fs);
            textureHeight = GetInt(fs);

            imageData = new byte[textureWidth * textureHeight * 4];
            for (int k = 0; k < imageData.Length; k++)
                imageData[k] = (byte)fs.ReadByte();
        }

        private static void RetrieveTextureNode(FileStream fs, out int textureWidth, out int textureHeight, out byte[] imageData)
        {
            textureWidth = GetInt(fs);
            textureHeight = GetInt(fs);

            imageData = new byte[textureWidth * textureHeight * 4];
            for (int k = 0; k < imageData.Length; k++)
                imageData[k] = (byte)fs.ReadByte();
        }

        private static List<float> RetrieveVertices(FileStream fs, int amountVertices)
        {
            List<float> vertices = new();
            for (int k = 0; k < amountVertices; k++)
                vertices.Add(GetFloat(fs));

            return vertices;
        }

        private static void RetrieveSubmodelNode(FileStream fs, out string name, out Vector3 translation, out Vector3 scaling, out bool hasMaterial, out int amountPolygons)
        {
            name = GetString(fs, 10);
            translation = GetVector3(fs);
            scaling = GetVector3(fs);

            hasMaterial = GetBool(fs);

            amountPolygons = GetInt(fs);
        }

        private static void RetrieveModelNode(FileStream fs, out string name, out Vector3 translation, out Vector3 scaling, out int submodelCount)
        {
            name = GetString(fs, 10);

            translation = GetVector3(fs);
            scaling = GetVector3(fs);

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