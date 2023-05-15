using CORERenderer.OpenGL;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static Error LoadFBX(string path, out string name, out List<List<Vertex>> vertices)
        {
            if (!File.Exists(path))
            {
                vertices = new();
                name = "PLACEHOLDER";
                return Error.InvalidPath;
            }

            Assimp.Scene s = GetAssimpScene(path);
            name = Path.GetFileNameWithoutExtension(path);
            vertices = GetBasicSceneData(path, s, out _, out _, out _);

            return Error.None;
        }
    }
}
