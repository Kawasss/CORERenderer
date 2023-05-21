using CORERenderer.Main;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static PBRMaterial LoadCPBR(string path)
        {
            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                Texture albedo = GetTexture(fs);
                Texture normal = GetTexture(fs);
                Texture metallic = GetTexture(fs);
                Texture roughness = GetTexture(fs);
                Texture AO = GetTexture(fs);
                Texture height = GetTexture(fs);

                return new(albedo, normal, metallic, roughness, AO, height);
            }
        }

        private static int holder = 0;

        private static Texture GetTexture(FileStream fs)
        {
            int length = GetInt(fs);
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
                data[i] = (byte)fs.ReadByte();
            holder++;
            return GenerateTextureFromData($"texture{holder}", data);
        }

        private static Texture GenerateTextureFromData(string name, byte[] imageData) //its incredible slow to create and delete a file
        {
            string dir = $"{COREMain.BaseDirectory}\\TextureCache\\";//Path.GetTempPath();
            using (FileStream fs = File.Create($"{dir}{name}.png"))
            using (StreamWriter sw = new(fs))
            {
                sw.BaseStream.Write(imageData);
            }
            Texture tex = Globals.FindTexture($"{dir}{name}.png");
            File.Delete($"{dir}{name}.png");
            return tex;
        }
    }
}
