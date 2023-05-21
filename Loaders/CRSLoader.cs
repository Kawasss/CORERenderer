using System.Text;
using CORERenderer.OpenGL;
using COREMath;
using CORERenderer.Main;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public const string CURRENT_VERSION = "v1.2";

        public static Error LoadCRS(string path, ref Scene scene, out string header)
        {
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    header = GetString(fs, 100);

                    List<Model> models = new();

                    if (!header.Contains(CURRENT_VERSION))
                        return Error.Outdated;

                    Rendering.Camera.position = GetVector3(fs);
                    Rendering.Camera.Pitch = GetFloat(fs);
                    Rendering.Camera.Yaw = GetFloat(fs);
                    scene.lights.Add(new() { position = GetVector3(fs) });

                    int modelCount = GetInt(fs);
                    System.Console.WriteLine(modelCount);
                    for (int i = 0; i < modelCount; i++)
                    {
                        RetrieveModelNode(fs, out string modelName, out Vector3 translation, out Vector3 scaling, out Vector3 rotation, out int submodelCount);

                        models.Add(new());
                        models[^1].Name = modelName;
                        models[^1].Transform.translation = translation;
                        models[^1].Transform.scale = scaling;
                        models[^1].Transform.rotation = rotation;

                        //getting the submodels
                        Vector3 min = Vector3.Zero, max = Vector3.Zero;
                        for (int j = 0; j < submodelCount; j++)
                        {
                            RetrieveSubmodelNode(fs, out string submodelName, out Vector3 submodelTranslation, out Vector3 submodelScaling, out Vector3 submodelRotation, out bool hasMaterial, out bool hasBones, out int amountPolygons);

                            int amountVertices = amountPolygons * 3; //a polygon consists of 3 vertices
                            List<Vertex> vertices = RetrieveVertices(fs, amountVertices, hasBones);
                            DetermineMinMax(vertices, translation, ref min, ref max);

                            if (hasMaterial)
                            {
                                PBRMaterial material = RetrieveMaterialNode(fs);

                                models[^1].type = RenderMode.ObjFile;
                                models[^1].submodels.Add(new(submodelName, vertices, submodelTranslation, submodelScaling, submodelRotation, models[^1], material));
                            }
                            else
                            {
                                models[^1].type = RenderMode.STLFile;
                                models[^1].submodels.Add(new(submodelName, vertices, submodelTranslation, submodelScaling, models[^1]));
                            }
                        }
                        models[^1].Transform.BoundingBox = new(min, max);
                    }
                    bool hasSkybox = RetrieveSkyboxNode(fs, out scene.skybox);

                    scene.models = models;
                }
            }
            catch (Exception)
            {
                scene = new();
                header = "";
                return Error.InvalidContents;
            }
            return Error.None;
        }

        private static bool RetrieveSkyboxNode(FileStream fs, out HDRTexture skybox)
        {
            bool exists = GetBool(fs);

            if (!exists)
            {
                skybox = null;
                return false;
            }

            int length = GetInt(fs);
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)fs.ReadByte();

            string dir = Path.GetTempPath();
            using (FileStream fs2 = File.Create($"{dir}skybox.hdr"))
            using (StreamWriter sw = new(fs2))
            {
                sw.BaseStream.Write(data);
            }
            skybox = HDRTexture.ReadFromFile($"{dir}skybox.hdr", Rendering.TextureQuality);
            File.Delete($"{dir}skybox.hdr");
            return true;
        }

        private static void DetermineMinMax(List<Vertex> vertices, Vector3 translation, ref Vector3 min, ref Vector3 max)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                max.x = vertices[i].x > max.x ? vertices[i].x : max.x;
                max.y = vertices[i].y > max.y ? vertices[i].y : max.y;
                max.z = vertices[i].z > max.z ? vertices[i].z : max.z;

                min.x = vertices[i].x < min.x ? vertices[i].x : min.x;
                min.y = vertices[i].y < min.y ? vertices[i].y : min.y;
                min.z = vertices[i].z < min.z ? vertices[i].z : min.z;
            }
        }

        private static PBRMaterial RetrieveMaterialNode(FileStream fs)
        {
            //retrieve all materials
            Texture difTex = Globals.usedTextures[1];
            if (!RetrieveTextureNode(fs, out byte[] diffusePNGData))
            {
                difTex = GenerateTextureFromData(diffusePNGData);
                Globals.usedTextures.Add(difTex);
            }

            Texture normIndex = Globals.usedTextures[3];
            if (!RetrieveTextureNode(fs, out byte[] normalPNGData))
            {
                normIndex = GenerateTextureFromData(normalPNGData);
                Globals.usedTextures.Add(normIndex);
            }

            Texture metallicIndex = Globals.usedTextures[4];
            if (!RetrieveTextureNode(fs, out byte[] specularPNGData))
            {
                metallicIndex = GenerateTextureFromData(specularPNGData);
                Globals.usedTextures.Add(metallicIndex);
            }

            Texture roughnessIndex = Globals.usedTextures[1];
            if (!RetrieveTextureNode(fs, out byte[] roughnessPNGData))
            {
                roughnessIndex = GenerateTextureFromData(roughnessPNGData);
                Globals.usedTextures.Add(roughnessIndex);
            }

            Texture AOIndex = Globals.usedTextures[1];
            if (!RetrieveTextureNode(fs, out byte[] aoPNGData))
            {
                AOIndex = GenerateTextureFromData(aoPNGData);
                Globals.usedTextures.Add(AOIndex);
            }

            Texture heightIndex = Globals.usedTextures[4];
            if (!RetrieveTextureNode(fs, out byte[] heightPNGData))
            {
                heightIndex = GenerateTextureFromData(heightPNGData);
                Globals.usedTextures.Add(heightIndex);
            }

            return new() { albedo = difTex, normal = normIndex, metallic = metallicIndex, roughness = roughnessIndex, AO = AOIndex, height = heightIndex };
        }

        private static int amountOfTexturesCreated = 0;

        private static Texture GenerateTextureFromData(byte[] imageData) //its incredible slow to create and delete a file
        {
            amountOfTexturesCreated++;
            string dir = $"{COREMain.BaseDirectory}\\TextureCache\\";
            using (FileStream fs = File.Create($"{dir}diffuseHolder{amountOfTexturesCreated}.png"))
            using (StreamWriter sw = new(fs))
            {
                sw.BaseStream.Write(imageData);
            }
            Texture tex = Globals.FindTexture($"{dir}diffuseHolder{amountOfTexturesCreated}.png");
            File.Delete($"{dir}diffuseHolder{amountOfTexturesCreated}.png");
            return tex;
        }

        private static bool RetrieveTextureNode(FileStream fs, out byte[] imageData)
        {
            if (GetBool(fs)) //is default material{
            {
                imageData = Array.Empty<byte>();
                return true;
            }
            int length = GetInt(fs);
            imageData = new byte[length];

            for (int i = 0; i < length; i++)
                imageData[i] = (byte)fs.ReadByte();
            return false;
        }

        private static List<Vertex> RetrieveVertices(FileStream fs, int amountVertices, bool hasBones)
        {
            List<Vertex> vertices = new();
            for (int k = 0; k < amountVertices; k++)
            {
                vertices.Add(new());
                vertices[k].x = GetFloat(fs);
                vertices[k].y = GetFloat(fs);
                vertices[k].z = GetFloat(fs);

                vertices[k].uvX = GetFloat(fs);
                vertices[k].uvY = GetFloat(fs);

                vertices[k].normalX = GetFloat(fs);
                vertices[k].normalY = GetFloat(fs);
                vertices[k].normalZ = GetFloat(fs);

                if (hasBones)
                {
                    for (int i = 0; i < 8; i++)
                        vertices[k].boneIDs[i] = GetInt(fs);
                    for (int i = 0; i < 8; i++)
                        vertices[k].boneWeights[i] = GetFloat(fs);
                }
            }

            return vertices;
        }

        private static void RetrieveSubmodelNode(FileStream fs, out string name, out Vector3 translation, out Vector3 scaling, out Vector3 rotation, out bool hasMaterial, out bool hasBones, out int amountPolygons)
        {
            name = GetString(fs, 10);
            translation = GetVector3(fs);
            scaling = GetVector3(fs);
            rotation = GetVector3(fs);

            hasMaterial = GetBool(fs);
            hasBones = GetBool(fs);

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