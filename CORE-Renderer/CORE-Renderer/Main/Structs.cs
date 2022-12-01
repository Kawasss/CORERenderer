using CORERenderer.textures;
using COREMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.Main
{
    /// <summary>
    /// contains all the metadata for one object in the CRS directory
    /// </summary>
    public struct ObjectInstance 
    {
        public ObjectInstance(FileStream csv, FileStream csi, string objP, int amountVerticeGroups, int amountIndiceGroups)
        {
            csvFile = csv;
            csiFile = csi;
            objPath = objP;
            amountOfVerticeGroups = amountVerticeGroups;
            amountOfIndiceGroups = amountIndiceGroups;
        }
        public FileStream csvFile;
        public FileStream csiFile;
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
