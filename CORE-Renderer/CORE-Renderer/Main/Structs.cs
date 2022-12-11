using CORERenderer.textures;
using CORERenderer.shaders;
using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.Main
{
    public struct Framebuffer
    {
        public uint FBO; //FrameBufferObject
        public uint VAO; //VertexArrayObject
        public uint Texture;
        public uint RBO; //RenderBufferObject
        public Shader shader;

        public uint VBO; //VBO isnt really needed, but just in case

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

            glDrawArrays(GL_TRIANGLES, 0, 6);
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

        public Material()
        {
            Name = "placeholder";
            Texture = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png"); //for now textures and diffuse maps are the same
            DiffuseMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholder.png");
            SpecularMap = Texture.ReadFromFile($"{CORERenderContent.pathRenderer}\\textures\\placeholderspecular.png");

            Ambient = new(0.2f, 0.2f, 0.2f);
            Diffuse = new(0.5f, 0.5f, 0.5f);
            Specular = new(1, 1, 1);
            EmissiveCoefficient = Vector3.Zero;
            Illum = 2;
            Shininess = 32;

        }
    }
}
