using COREMath;
using CORERenderer.OpenGL;
using CORERenderer.Main;
using Console = CORERenderer.GUI.Console;
using Assimp;
using Material = CORERenderer.Main.Material;
using Assimp.Configs;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static Error LoadOBJ(string path, out string name, out List<List<Vertex>> vertices, out List<Material> materials, out Vector3 center, out Vector3 extents)
        {
            if (!File.Exists(path))
            {
                vertices = new();
                name = "PLACEHOLDER";
                center = Vector3.Zero;
                extents = Vector3.Zero;
                materials = new();
                return Error.InvalidPath;
            }

            Assimp.Scene s = GetAssimpScene(path);

            name = Path.GetFileNameWithoutExtension(path);

            vertices = GetBasicSceneData(path, s, out materials, out center, out extents);

            return Error.None;
        }

        private static List<List<Vertex>> GetBasicSceneData(string path, Assimp.Scene s, out List<Material> materials, out Vector3 center, out Vector3 extents)
        {
            List<List<Vertex>> vertices = new();
            materials = new();

            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            for (int i = 0; i < s.MeshCount; i++)
            {
                Material material = new();
                Mesh mesh = s.Meshes[i];
                vertices.Add(new());
                foreach (int indice in mesh.GetIndices())
                {
                    int uvIndex = 0;
                    if (s.HasMaterials)
                        uvIndex = s.Materials[mesh.MaterialIndex].TextureDiffuse.UVIndex;

                    Vector3D aPos = mesh.Vertices[indice];
                    Vector3 pos = new(aPos.X, aPos.Y, aPos.Z);

                    max.x = pos.x > max.x ? pos.x : max.x;
                    max.y = pos.y > max.y ? pos.y : max.y;
                    max.z = pos.z > max.z ? pos.z : max.z;

                    min.x = pos.x < min.x ? pos.x : min.x;
                    min.y = pos.y < min.y ? pos.y : min.y;
                    min.z = pos.z < min.z ? pos.z : min.z;

                    Vector3 normal = Vector3.UnitVectorY;
                    if (mesh.HasNormals)
                    {
                        Vector3D aNormal = mesh.Normals[indice];
                        normal = new(aNormal.X, aNormal.Y, aNormal.Z);
                    }

                    //Vector3D texCoor = mesh.TextureCoordinateChannels[i][indice];
                    Vector2 uv = new();
                    if (mesh.HasTextureCoords(uvIndex))
                        uv = s.HasMaterials ? new(mesh.TextureCoordinateChannels[uvIndex][indice].X, mesh.TextureCoordinateChannels[uvIndex][indice].Y) : new();

                    vertices[^1].Add(new(pos, uv, normal));
                }

                if (s.HasMaterials)
                {
                    Assimp.Material mat = s.Materials[mesh.MaterialIndex];
                    if (mat.HasTextureDiffuse)
                        material.Texture = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{mat.TextureDiffuse.FilePath}");
                    if (mat.HasTextureDiffuse)
                        material.DiffuseMap = material.Texture;
                    if (mat.HasTextureSpecular)
                        material.SpecularMap = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{mat.TextureSpecular.FilePath}");
                    if (mat.HasTextureNormal)
                        material.NormalMap = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{mat.TextureNormal.FilePath}");
                    if (mat.HasTextureEmissive)
                        material.MetalMap = Globals.FindTexture($"{Path.GetDirectoryName(path)}\\{mat.TextureEmissive.FilePath}");

                    if (mat.HasColorTransparent)
                    {
                        material.overrideColor = new(mat.ColorTransparent.R, mat.ColorTransparent.G, mat.ColorTransparent.B);
                        material.Transparency = mat.Opacity;
                    }

                    if (mat.HasShininess)
                        material.Shininess = mat.Shininess;
                }
                materials.Add(material);
            }
            center = (min + max) * 0.5f;
            extents = max - center;
            return vertices;
        }

        private static Assimp.Scene GetAssimpScene(string path)
        {
            AssimpContext context = new();
            Assimp.Configs.RemoveComponentConfig a = new(ExcludeComponent.Normals);
            context.SetConfig(a);
            return context.ImportFile(path,  PostProcessSteps.FixInFacingNormals | PostProcessSteps.RemoveComponent | PostProcessPreset.TargetRealTimeMaximumQuality);
        }

        [Obsolete("Made redundant by the inclusion of Assimp")]
        public static Error LoadOBJ(string path, out List<string> mtlNames, out List<List<Vertex>> outVertices, out List<List<uint>> outIndices, out List<Vector3> offsets, out Vector3 center, out Vector3 extents, out string mtllib)
        {
            extents = Vector3.Zero;
            center = Vector3.Zero;

            Vector3 min = Vector3.Zero;
            Vector3 max = Vector3.Zero;

            if (path == null)
            {
                outVertices = new();
                outIndices = new();
                mtllib = null;
                mtlNames = new();
                offsets = new();
                return Error.InvalidPath;
            }

            List<int> temp = new();

            string filename = Path.GetFileName(path);

            if (path[^4..].ToLower() != ".obj")
            {
                outVertices = new();
                outIndices = new();
                mtllib = null;
                mtlNames = new();
                offsets = new();
                return Error.InvalidFile;
            }

            outIndices = new();

            List<Vector3> vertices = new();

            List<Vector3> normals = new();

            List<Vector2> UVCoordinates = new();

            List<string> sValues = new();

            offsets = new();

            mtlNames = new();

            mtllib = "default"; //not null for later mtl use

            List<string> unreadableLines = new();

            bool withTextures = true;

            Console.WriteDebug($"Reading {filename}:");

            int aa = 0;
            outVertices = new();
            Dictionary<string, int> indiceBinder = new();

            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BufferedStream bs = new(fs))
                using (StreamReader sr = new(bs))
                {
                    int currentgroup = -1; //becomes 0 when the first object is found, remains -1 so that it can be detected if it doesnt use objects
                    for (string n = sr.ReadLine(); n != null; n = sr.ReadLine())
                    {
                        if (n.Length < 2)
                            n = "  ";
                        switch (n[0..2])
                        {
                            case "  ": //empty lines
                                break;

                            case "o ":
                                outIndices.Add(new());
                                outVertices.Add(new());
                                aa = 0;
                                indiceBinder = new();
                                currentgroup++;
                                break;

                            case "mt":
                                mtllib = n[7..];
                                break;

                            case "v ": //vector
                                vertices.Add(GetThreeFloatsWithRegEx(n));
                                if (offsets.Count == 0)
                                    offsets.Add(vertices[^1]);
                                else
                                    offsets.Add(offsets[0]);
                                vertices[^1] -= offsets[0];

                                max.x = vertices[^1].x > max.x ? vertices[^1].x : max.x;
                                max.y = vertices[^1].y > max.y ? vertices[^1].y : max.y;
                                max.z = vertices[^1].z > max.z ? vertices[^1].z : max.z;

                                min.x = vertices[^1].x < min.x ? vertices[^1].x : min.x;
                                min.y = vertices[^1].y < min.y ? vertices[^1].y : min.y;
                                min.z = vertices[^1].z < min.z ? vertices[^1].z : min.z;
                                break;

                            case "vn": //vector normal
                                normals.Add(GetThreeFloatsWithRegEx(n));
                                break;

                            case "vt": //UV coordinates
                                UVCoordinates.Add(GetTwoFloatsWithRegEx(n));
                                break;

                            case "s ": //s value
                                sValues.Add(n[2..]);
                                break;

                            case "us": //usemtl (maybe better way?)
                                mtlNames.Add(n[7..]); //"usemtl " is 7 chars long
                                break;

                            case "f ": //v / vn / vt indicator, speed of reading the file dramatically slows down here
                                if (currentgroup == -1)
                                {
                                    outIndices.Add(new());
                                    outVertices.Add(new());
                                    currentgroup = 0;
                                }
                                int i = currentgroup; //improves readability

                                List<int> local = new();
                                List<int> local2 = new();
                                List<string> local3 = new();

                                //code below isolates the indices into ../../.. then by / so the uint value remains
                                for (int k = n.IndexOf(" "); k > -1; k = n.IndexOf(" ", k + 1))
                                    if (k != n.Length - 1)
                                        local.Add(k);

                                //isolates the indices to ../../.. by using the indexes of the surrounding " "'s
                                for (int k = 0; k < local.Count - 1; k++)
                                    local3.Add(n[(local[k] + 1)..local[k + 1]]);
                                local3.Add(n[(local[^1] + 1)..]);

                                //isolates each int from local ( ../../.. ) and parses it
                                List<Vertex> polygons = new();
                                foreach (string s in local3)
                                {
                                    Vertex localPoly = new();
                                    if (s.Contains('/'))
                                    {
                                        local2 = new();

                                        Vector3 values = GetThreeFloatsWithRegEx(s);

                                        //texture coords are between the vertex coords and normals ../TC/.., without textures it would like ..//.., making it possible to check whether it uses textures by checking if the / indexes are directly next to eachother
                                        if (s.Contains("//"))
                                            withTextures = false;

                                        //adds the vertex coordinates
                                        int verCoord = (int)values.x - 1;
                                        localPoly.x = vertices[verCoord].x;
                                        localPoly.y = vertices[verCoord].y;
                                        localPoly.z = vertices[verCoord].z;
                                        if (withTextures) //adds the texture coordinates if they exist
                                        {
                                            int texCoords = (int)values.y - 1;
                                            localPoly.uvX = UVCoordinates[texCoords].x;
                                            localPoly.uvY = UVCoordinates[texCoords].y;
                                        }
                                        //adds normals
                                        int normal = (int)values.z - 1;
                                        localPoly.normalX = normals[normal].x;
                                        localPoly.normalY = normals[normal].y;
                                        localPoly.normalZ = normals[normal].z;
                                    }
                                    else //if a vertex doesnt contain texture coordinates and normals its info is written as only the coordinates without any /'s, instead of VC/TC/N or VC//N
                                    {
                                        //adds the vertex coordinates and empty data for the texture coordinates and normals
                                        int verCoord = int.Parse(s) - 1;
                                        localPoly.x = vertices[verCoord].x;
                                        localPoly.y = vertices[verCoord].y;
                                        localPoly.z = vertices[verCoord].z;
                                    }
                                    //updates the indices binder so that later on indices can easily be created, the vertex coords, texture coords and normals are always grouped together in strides of 8, one indice covering all 8 of them
                                    if (!indiceBinder.ContainsKey(s))
                                        indiceBinder.Add(s, aa);
                                    aa++;
                                    polygons.Add(localPoly);
                                }
                                Vector3 totalSumOfNormals = Vector3.Zero;
                                for (int j = 0; j < polygons.Count; j++)
                                {
                                    Vertex polygon = polygons[j];
                                    Vector3 newNormal = new(polygon.normalX, polygon.normalY, polygon.normalZ);
                                    totalSumOfNormals += newNormal;
                                }
                                Vector3 averageNormal = totalSumOfNormals / polygons.Count;
                                foreach (Vertex polygon in polygons)
                                    outVertices[i].Add(polygon);
                                //a triangle, square and circles indices are all structured differently so it has to be checked what kind of shape it is
                                if (local3.Count == 3) //triangle
                                {
                                    for (int k = 0; k < local3.Count; k++)
                                        outIndices[i].Add((uint)indiceBinder[local3[k]]);
                                }
                                else if (local3.Count > 3) //plane & circle
                                {
                                    for (int k = 0; k < local3.Count - 3; k++)
                                    {
                                        outIndices[i].Add((uint)indiceBinder[local3[0]]);
                                        outIndices[i].Add((uint)indiceBinder[local3[k + 1]]);
                                        outIndices[i].Add((uint)indiceBinder[local3[k + 2]]);

                                        outIndices[i].Add((uint)indiceBinder[local3[k]]);
                                        outIndices[i].Add((uint)indiceBinder[local3[k + 2]]);
                                        outIndices[i].Add((uint)indiceBinder[local3[k + 3]]);
                                    }
                                }
                                break;

                            default:
                                if (n[0] == '#')
                                    break;
                                unreadableLines.Add(n);
                                break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Error.InvalidContents;
            }
            center = (min + max) * 0.5f;
            extents = max - center;

            if (unreadableLines.Count > 0)
                Console.WriteError($" Couldn't read {unreadableLines.Count} lines in {filename}");

            Console.WriteDebug($"finished reading {filename}");

            return Error.None;
        }
    }
}