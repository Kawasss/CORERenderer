using CORERenderer.textures;
using CORERenderer.shaders;
using COREMath;
namespace CORERenderer.Main
{
    public struct Character
    {
        public uint textureID;
        public Vector2 size;
        public Vector2 bearing;
        public int advance;
    }

    public struct Light
    {
        public Vector3 position;
        public Vector3 color;
    }

    public struct Cubemap
    {
        public uint VAO;
        public uint textureID;
        public Shader shader;
    }

    /// <summary>
    /// contains all the metadata for one object in the CRS directory
    /// </summary>
    public struct ObjectInstance 
    {
        public ObjectInstance(string objP, int amountVerticeGroups, int amountIndiceGroups)
        {
            objPath = objP;
            amountOfVerticeGroups = amountVerticeGroups;
            amountOfIndiceGroups = amountIndiceGroups;
        }
        public string objPath;
        public int amountOfVerticeGroups;
        public int amountOfIndiceGroups;
    }

    public struct PBRMaterial
    {
        public string Name;
        public Texture albedoMap;
        public Texture normalMap;
        public Texture metallicMap;
        public Texture roughnessMap;
        public Texture AOMap;
    }

    /// <summary>
    /// holds all the information for an OpenGL material
    /// </summary>
    public struct Material
    {
        public string Name;
        public float Shininess;
        public Vector3 Ambient;
        public Vector3 Diffuse;
        public Vector3 Specular;
        public Vector3 EmissiveCoefficient;
        public float OpticalDensity;
        public int Illum;
        public float Transparency;
        public int Texture;
        public int DiffuseMap;
        public int SpecularMap;
        public int NormalMap;

        public Material()
        {
            Name = "placeholder";
            Texture = 0;
            DiffuseMap = 0;
            SpecularMap = 1;
            NormalMap = 3;

            OpticalDensity = 1;
            Transparency = 1;

            Ambient = new(0.2f, 0.2f, 0.2f);
            Diffuse = new(0.5f, 0.5f, 0.5f);
            Specular = new(1, 1, 1);
            EmissiveCoefficient = Vector3.Zero;
            Illum = 2;
            Shininess = 32;

        }
    }
}