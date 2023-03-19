using CORERenderer.textures;
using CORERenderer.shaders;
using COREMath;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using System.Runtime.CompilerServices;
using CORERenderer.OpenGL;

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

    public struct Framebuffer
    {
        public uint FBO; //FrameBufferObject
        public uint VAO; //VertexArrayObject
        public uint Texture;
        public uint RBO; //RenderBufferObject
        public Shader shader;

        public uint VBO; //VBO isnt really needed, but just in case

        public void Bind() => glBindFramebuffer(this);

        public void RenderFramebuffer()
        {
            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE_2D, 0);

            glBindFramebuffer(GL_FRAMEBUFFER, 0);
            glClear(GL_DEPTH_BUFFER_BIT);
            glDisable(GL_DEPTH_TEST);

            glClearColor(1, 1, 1, 1);

            this.shader.Use();

            glBindVertexArray(this.VAO);
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, this.Texture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
        }
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