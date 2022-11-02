using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using COREMath;
using CORERenderer.textures;

namespace CORERenderer.Loaders
{
    public class Obj
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

        public Obj(string path)
        {
            bool loaded = new OBJLoader().LoadOBJ(path, out vertices, out indices, out string mtllib);
            _ = new OBJLoader().LoadOBJ(path, out _, out _, out _);

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
            {
                temp.Add(i);
            }

            if (!loaded)
            {
                throw new Exception($"Invalid file format for {path[(temp[^1] + 1)..]} (!.obj && !.OBJ)");
            }

            if (mtllib == null)
            {
                return;
            }

            new MTLLoader().LoadMTL
            (
                $"{path[..(temp[^1] + 1)]}{mtllib}", out List<float> shininess, 
                out List<Vector3>  ambient, out List<Vector3> diffuse,
                out List<Vector3> specular, out List<float> opticalDensity, 
                out List<int> illum, out List<float> transparency, 
                out List<Texture> texture, out List<Texture> diffuseMap, 
                out List<Texture> specularMap
            );


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
