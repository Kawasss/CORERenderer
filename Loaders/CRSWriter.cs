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
                        WriteSubmodelInfo(sw, submodel);
                        foreach (float value in submodel.vertices)
                            sw.Write(BitConverter.GetBytes(value));
                    }
                }
            }
        }

        private static void WriteGeneralInfo(StreamWriter sw, string header, int modelCount)
        {
            byte[] headerBytes = GenerateHeader(header, 100);

            sw.BaseStream.Write(headerBytes);
            sw.BaseStream.Write(BitConverter.GetBytes(modelCount));
        }

        private static void WriteModelInfo(StreamWriter sw, Model model)
        {
            byte[] modelName = GenerateHeader(model.name, 10);

            byte[] position = model.translation.Bytes;
            byte[] scaling = model.Scaling.Bytes;

            byte[] amountSubmodels = BitConverter.GetBytes(model.submodels.Count);

            sw.BaseStream.Write(modelName);

            sw.BaseStream.Write(position);
            sw.BaseStream.Write(scaling);

            sw.BaseStream.Write(amountSubmodels);
        }

        private static void WriteSubmodelInfo(StreamWriter sw, Submodel submodel)
        {
            byte[] smName = GenerateHeader(submodel.Name, 10);
            byte[] position = submodel.translation.Bytes;
            byte[] scaling = submodel.scaling.Bytes;

            byte[] hasMaterials = BitConverter.GetBytes(submodel.hasMaterials);
            byte[] useGlDrawElements = BitConverter.GetBytes(submodel.useGlDrawElements);

            byte[] amountPolygons = BitConverter.GetBytes(submodel.numberOfVertices / 3);

            sw.BaseStream.Write(smName);
            sw.BaseStream.Write(position);
            sw.BaseStream.Write(scaling);

            sw.BaseStream.Write(hasMaterials);
            sw.BaseStream.Write(useGlDrawElements);

            sw.BaseStream.Write(amountPolygons);
        }
    }
}
