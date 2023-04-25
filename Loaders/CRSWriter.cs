using CORERenderer.Main;
using CORERenderer.textures;
using COREMath;

namespace CORERenderer.Loaders
{
    public partial class Writers
    {
        public static void GenerateCRS(string path, string name, string header, Model[] models)
        {
            using (FileStream fs = File.Create($"{path}\\{name}.crs"))
            using (BufferedStream bs = new(fs))
            using (StreamWriter sw = new(bs))
            {
                WriteGeneralInfo(sw, header, models.Length);

                foreach (Model model in models)
                {
                    WriteModelInfo(sw, model);

                    foreach (Submodel submodel in model.submodels)
                    {
                        WriteSubmodelNode(sw, submodel);
                    }
                }
            }
        }

        private static void WriteSubmodelNode(StreamWriter sw, Submodel submodel)
        {
            WriteSubmodelInfo(sw, submodel);

            foreach (float value in submodel.vertices)
                sw.BaseStream.Write(BitConverter.GetBytes(value));

            if (submodel.hasMaterials)
                WriteMaterialNode(sw, submodel.material);
        }

        private static void WriteMaterialNode(StreamWriter sw, Material material)
        {
            WriteMaterialInfo(sw, material);
            
            WriteTextureNode(sw, material.Diffuse, Globals.usedTextures[material.Texture]); //writes the diffuse map
            WriteTextureNode(sw, material.Specular, Globals.usedTextures[material.SpecularMap]); //writes the specular map
            WriteTextureNode(sw, Globals.usedTextures[material.NormalMap]); //writes the normal map
        }

        private static void WriteTextureNode(StreamWriter sw, Vector3 strength, Texture texture)
        {
            byte[] data = texture.FileContent;
            byte[] length = BitConverter.GetBytes(data.Length);

            sw.BaseStream.Write(strength.Bytes);
            sw.BaseStream.Write(length);
            sw.BaseStream.Write(data);
        }

        private static void WriteTextureNode(StreamWriter sw, Texture texture)
        {
            byte[] data = texture.FileContent;
            byte[] length = BitConverter.GetBytes(data.Length);

            sw.BaseStream.Write(length);
            sw.BaseStream.Write(data);
        }

        private static void WriteMaterialInfo(StreamWriter sw, Material material)
        {
            byte[] materialName = GenerateHeader(material.Name, 10);

            byte[] shininess = BitConverter.GetBytes(material.Shininess);
            byte[] transparency = BitConverter.GetBytes(material.Transparency);
            byte[] ambient = material.Ambient.Bytes;

            sw.BaseStream.Write(materialName);
            sw.BaseStream.Write(shininess);
            sw.BaseStream.Write(transparency);
            sw.BaseStream.Write(ambient);
        }

        private static void WriteGeneralInfo(StreamWriter sw, string header, int modelCount)
        {
            byte[] headerBytes = GenerateHeader(header, 100);

            sw.BaseStream.Write(headerBytes);
            sw.BaseStream.Write(BitConverter.GetBytes(modelCount));
        }

        private static void WriteModelInfo(StreamWriter sw, Model model)
        {
            byte[] modelName = GenerateHeader(model.Name, 10);

            byte[] position = model.translation.Bytes;
            byte[] scaling = model.Scaling.Bytes;
            byte[] rotation = model.rotation.Bytes;

            byte[] amountSubmodels = BitConverter.GetBytes(model.submodels.Count);

            sw.BaseStream.Write(modelName);

            sw.BaseStream.Write(position);
            sw.BaseStream.Write(scaling);
            sw.BaseStream.Write(rotation);

            sw.BaseStream.Write(amountSubmodels);
        }

        private static void WriteSubmodelInfo(StreamWriter sw, Submodel submodel)
        {
            byte[] smName = GenerateHeader(submodel.Name, 10);
            byte[] position = submodel.translation.Bytes;
            byte[] scaling = submodel.scaling.Bytes;
            byte[] rotation = submodel.rotation.Bytes;

            byte[] hasMaterials = BitConverter.GetBytes(submodel.hasMaterials);

            byte[] amountPolygons = BitConverter.GetBytes(submodel.NumberOfVertices / 3);

            sw.BaseStream.Write(smName);
            sw.BaseStream.Write(position);
            sw.BaseStream.Write(scaling);
            sw.BaseStream.Write(rotation);

            sw.BaseStream.Write(hasMaterials);

            sw.BaseStream.Write(amountPolygons);
        }
    }
}