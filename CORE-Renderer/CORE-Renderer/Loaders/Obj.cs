using COREMath;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public class Obj : Readers
    {
        public readonly float[] vertices;
        public readonly uint[] indices;

        public float[] Shininess;
        public float[] OpticalDensity;
        public int[] Illum;
        public float[] Transparency;

        public readonly Vector3[] Ambient;
        public readonly Vector3[] Diffuse;
        public readonly Vector3[] Specular;

        public readonly Texture[] Texture;
        public readonly Texture[] DiffuseMap;
        public readonly Texture[] SpecularMap;

        public readonly string name = null;

        public Obj(string path)
        {
            bool loaded = LoadOBJ(path, out vertices, out indices, out string mtllib);
            _ = LoadOBJ(path, out _, out _, out _);

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);

            name = path[(temp[^1] + 1)..];

            if (!loaded)
                throw new Exception($"Invalid file format for {name} (!.obj && !.OBJ)");

            if (mtllib == null)
                return;

            loaded = LoadMTL
            (
                $"{path[..(temp[^1] + 1)]}{mtllib}", out List<float> shininess, 
                out List<Vector3>  ambient, out List<Vector3> diffuse,
                out List<Vector3> specular, out List<float> opticalDensity, 
                out List<int> illum, out List<float> transparency, 
                out List<Texture> texture, out List<Texture> diffuseMap, 
                out List<Texture> specularMap
            );

            if (!loaded)
                throw new Exception($"Invalid file format for {name} (!.mtl && !.MTL)");

            //puts all of the data into arrays
            Shininess = shininess.ToArray();
            OpticalDensity = opticalDensity.ToArray();
            Illum = illum.ToArray();
            Transparency = transparency.ToArray();

            Ambient = ambient.ToArray();
            Diffuse = diffuse.ToArray();
            Specular = specular.ToArray();

            Texture = texture.ToArray();
            DiffuseMap = diffuseMap.ToArray();
            SpecularMap = specularMap.ToArray();
        }
    }  
}
