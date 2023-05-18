﻿using CORERenderer.Main;
using CORERenderer.textures;
using COREMath;
using CORERenderer.OpenGL;

namespace CORERenderer.Loaders
{
    public partial class Writers
    {
        public static void GenerateCRS(string path, string name, string header, Scene scene)
        {
            Model[] models = scene.models.ToArray();

            using (FileStream fs = File.Create($"{path}\\{name}.crs"))
            using (BufferedStream bs = new(fs))
            using (StreamWriter sw = new(bs))
            {
                WriteGeneralInfo(sw, header, scene.camera, scene.lights[0], models.Length);

                foreach (Model model in models)
                {
                    WriteModelInfo(sw, model);

                    foreach (Submodel submodel in model.submodels)
                    {
                        WriteSubmodelNode(sw, submodel);
                    }
                }
                WriteSkyboxNode(sw, scene.skybox);
            }
        }

        private static void WriteSkyboxNode(StreamWriter sw, HDRTexture skybox)
        {
            byte[] exists = BitConverter.GetBytes(skybox != null);
            sw.BaseStream.Write(exists);

            if (skybox != null)
            {
                sw.BaseStream.Write(BitConverter.GetBytes(skybox.data.Length));
                sw.BaseStream.Write(skybox.data);
            }
        }

        private static void WriteCameraNode(StreamWriter sw, Camera camera)
        {
            byte[] position = camera.position.Bytes;

            byte[] pitch = BitConverter.GetBytes(camera.Pitch);
            byte[] yaw = BitConverter.GetBytes(camera.Yaw);

            sw.BaseStream.Write(position);
            sw.BaseStream.Write(pitch);
            sw.BaseStream.Write(yaw);
        }

        private static void WriteLightNode(StreamWriter sw, Light light)
        {
            byte[] position = light.position.Bytes;
            sw.BaseStream.Write(position);
        }

        private static void WriteSubmodelNode(StreamWriter sw, Submodel submodel)
        {
            WriteSubmodelInfo(sw, submodel);

            foreach (Vertex value in submodel.Vertices)
            {
                sw.BaseStream.Write(BitConverter.GetBytes(value.x));
                sw.BaseStream.Write(BitConverter.GetBytes(value.y));
                sw.BaseStream.Write(BitConverter.GetBytes(value.z));

                sw.BaseStream.Write(BitConverter.GetBytes(value.uvX));
                sw.BaseStream.Write(BitConverter.GetBytes(value.uvY));

                sw.BaseStream.Write(BitConverter.GetBytes(value.normalX));
                sw.BaseStream.Write(BitConverter.GetBytes(value.normalY));
                sw.BaseStream.Write(BitConverter.GetBytes(value.normalZ));

                if (value.hasBones)
                {
                    for (int i = 0; i < 8; i++)
                        sw.BaseStream.Write(BitConverter.GetBytes(value.boneIDs[i]));
                    for (int i = 0; i < 8; i++)
                        sw.BaseStream.Write(BitConverter.GetBytes(value.boneWeights[i]));
                }
            }
                

            if (submodel.hasMaterials)
                WriteMaterialNode(sw, submodel.material);
        }

        private static void WriteMaterialNode(StreamWriter sw, Material material)
        {
            WriteMaterialInfo(sw, material);
            
            WriteTextureNode(sw, material.Diffuse, Globals.usedTextures[material.Texture], material.Texture == 0); //writes the diffuse map
            WriteTextureNode(sw, material.Specular, Globals.usedTextures[material.SpecularMap], material.SpecularMap == 1); //writes the specular map
            WriteTextureNode(sw, Globals.usedTextures[material.NormalMap], material.NormalMap == 3); //writes the normal map
        }

        private static void WriteTextureNode(StreamWriter sw, Vector3 strength, Texture texture, bool isDefault)
        {
            byte[] data = texture.FileContent;
            byte[] length = BitConverter.GetBytes(data.Length);

            sw.BaseStream.Write(BitConverter.GetBytes(isDefault));
            sw.BaseStream.Write(strength.Bytes);
            sw.BaseStream.Write(length);
            sw.BaseStream.Write(data);
        }

        private static void WriteTextureNode(StreamWriter sw, Texture texture, bool isDefault)
        {
            byte[] data = texture.FileContent;
            byte[] length = BitConverter.GetBytes(data.Length);

            sw.BaseStream.Write(BitConverter.GetBytes(isDefault));
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

        private static void WriteGeneralInfo(StreamWriter sw, string header, Camera camera, Light light, int modelCount)
        {
            byte[] headerBytes = GenerateHeader(header, 100);

            sw.BaseStream.Write(headerBytes);

            WriteCameraNode(sw, camera);
            WriteLightNode(sw, light);

            sw.BaseStream.Write(BitConverter.GetBytes(modelCount));
        }

        private static void WriteModelInfo(StreamWriter sw, Model model)
        {
            byte[] modelName = GenerateHeader(model.Name, 10);

            byte[] position = model.Transform.translation.Bytes;
            byte[] scaling = model.Transform.scale.Bytes;
            byte[] rotation = model.Transform.rotation.Bytes;

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
            byte[] hasBones = BitConverter.GetBytes(submodel.HasBones);

            byte[] amountPolygons = BitConverter.GetBytes(submodel.NumberOfVertices / 3);

            sw.BaseStream.Write(smName);
            sw.BaseStream.Write(position);
            sw.BaseStream.Write(scaling);
            sw.BaseStream.Write(rotation);

            sw.BaseStream.Write(hasMaterials);
            sw.BaseStream.Write(hasBones);

            sw.BaseStream.Write(amountPolygons);
        }
    }
}