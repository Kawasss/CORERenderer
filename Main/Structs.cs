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

    public struct PBRMaterial
    {
        public Texture albedo;
        public Texture normal;
        public Texture metallic;
        public Texture roughness;
        public Texture AO;
        public Texture height;

        public PBRMaterial(Texture albedo, Texture normal, Texture metallic, Texture roughness, Texture AO, Texture height)
        {
            this.albedo = albedo;
            this.normal = normal;
            this.metallic = metallic;
            this.roughness = roughness;
            this.AO = AO;
            this.height = height;
        }
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
        public Texture Texture;
        public Texture DiffuseMap;
        public Texture SpecularMap;
        public Texture NormalMap;
        public Texture MetalMap;
        public Texture aoMap;
        public Texture displacementMap;

        public Vector3 overrideColor = Vector3.Zero;

        public Material()
        {
            Name = "placeholder";
            Texture = Globals.usedTextures[0];
            DiffuseMap = Globals.usedTextures[2];
            SpecularMap = Globals.usedTextures[1];
            NormalMap = Globals.usedTextures[3];
            MetalMap = Globals.usedTextures[4];
            aoMap = Globals.usedTextures[2];
            displacementMap = Globals.usedTextures[4];

            OpticalDensity = 1;
            Transparency = 1;

            Ambient = new(0.2f, 0.2f, 0.2f);
            Diffuse = new(0.5f, 0.5f, 0.5f);
            Specular = new(1, 1, 1);
            EmissiveCoefficient = Vector3.Zero;
            Illum = 2;
            Shininess = 32;
            overrideColor = Vector3.Zero;
        }
    }
}