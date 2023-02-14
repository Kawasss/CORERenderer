using COREMath;
using CORERenderer.Main;
using System.Globalization;

namespace CORERenderer.Loaders
{
    public partial class Readers
    {
        public static bool LoadOBJ(string path, out List<string> mtlNames, out List<List<float>> outVertices, out List<List<uint>> outIndices, out string mtllib)
        {
            if (path == null)
            {
                outVertices = new();
                outIndices = new();
                mtllib = null;
                mtlNames = new();
                return true;
            }

            List<int> temp = new();

            for (int i = path.IndexOf("\\"); i > -1; i = path.IndexOf("\\", i + 1))
                temp.Add(i);
            string filename = path[(temp[^1] + 1)..];

            if (path[^4..].ToLower() != ".obj")
            {
                outVertices = new();
                outIndices = new();
                mtllib = null;
                mtlNames = new();
                return false;
            }

            outIndices = new();

            List<Vector3> vertices = new();

            List<Vector3> normals = new();

            List<Vector2> UVCoordinates = new();

            List<string> sValues = new();

            mtlNames = new();

            mtllib = "default"; //not null for later mtl use

            List<string> unreadableLines = new();

            bool withTextures = true;

            Console.WriteLine($"Reading {filename}:");

            int aa = 0;
            outVertices = new();
            Dictionary<string, int> indiceBinder = new();

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
                            if (n.Contains("v  "))
                                n = "vv " + n[3..];
                            int[] localV = new int[4];
                            int z = 0;
                            for (int k = n.IndexOf(" "); k > -1; k = n.IndexOf(" ", k + 1), z++)
                                localV[z] = k;
                            vertices.Add(
                             new(
                                 n[localV[0]..localV[1]],
                                 n[localV[1]..localV[2]],
                                 n[localV[2]..]
                                ));
                            break;

                        case "vn": //vector normal
                            if (n.Contains("vn  "))
                                n = "vvn " + n[4..];
                            int[] localVn = new int[4];
                            int y = 0;
                            for (int k = n.IndexOf(" "); k > -1; k = n.IndexOf(" ", k + 1), y++)
                                localVn[y] = k;
                            normals.Add(
                                new(
                                        n[localVn[0]..localVn[1]],
                                        n[localVn[1]..localVn[2]],
                                        n[localVn[2]..]
                                   ));
                            break;

                        case "vt": //UV coordinates
                            if (n.Contains("vt  "))
                                n = "vvt " + n[4..];
                            int[] localVt = new int[3];
                            int holder = n.IndexOf(" ");
                            //finds and adds the 2 texture coordinates by the 2 spaces surrounding it
                            UVCoordinates.Add(
                             new(
                                    n[holder..n.IndexOf(" ", holder + 1)],
                                    n[n.IndexOf(" ", holder + 1)..]
                                ));
                            break;

                        case "s ": //s value
                            sValues.Add(n[2..]);
                            break;

                        case "us": //usemtl (maybe better way?)
                            mtlNames.Add(n[7..]); //"usemtl " is 7 chars long
                            break;

                        case "f ": //v / vn / vt indicator
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
                            foreach (string s in local3)
                            {
                                if (s.Contains('/'))
                                {
                                    local2 = new();
                                    //gets all of the index surrounding the ints, making them parsable
                                    for (int k = s.IndexOf("/"); k > -1; k = s.IndexOf("/", k + 1))
                                        local2.Add(k);

                                    if (local2[0] == local2[1] - 1)
                                        withTextures = false;

                                    for (int l = 0; l < 3; l++)
                                        outVertices[i].Add(vertices[int.Parse(s[..local2[0]], CultureInfo.InvariantCulture) - 1].xyz[l]);
                                    if (withTextures)
                                    {
                                        outVertices[i].Add(UVCoordinates[int.Parse(s[(local2[0] + 1)..local2[1]], CultureInfo.InvariantCulture) - 1].x);
                                        outVertices[i].Add(UVCoordinates[int.Parse(s[(local2[0] + 1)..local2[1]], CultureInfo.InvariantCulture) - 1].y);

                                        for (int l = 0; l < 3; l++)
                                            outVertices[i].Add(normals[int.Parse(s[(local2[1] + 1)..], CultureInfo.InvariantCulture) - 1].xyz[l]);
                                    }
                                    else
                                    {
                                        outVertices[i].Add(0);
                                        outVertices[i].Add(0);

                                        for (int l = 0; l < 3; l++)
                                            outVertices[i].Add(normals[int.Parse(s[(local2[1] + 1)..], CultureInfo.InvariantCulture) - 1].xyz[l]);
                                    }
                                }
                                else
                                {
                                    for (int l = 0; l < 3; l++)
                                        outVertices[i].Add(vertices[int.Parse(s, CultureInfo.InvariantCulture) - 1].xyz[l]); ;

                                    for (int l = 0; l < 5; l++)
                                        outVertices[i].Add(0);
                                }

                                if (!indiceBinder.ContainsKey(s))
                                {
                                    if (i == 0)
                                        indiceBinder.Add(s, aa);
                                    else
                                        indiceBinder.Add(s, aa);
                                }
                                aa++;
                            }
                            if (local3.Count == 3)
                            {
                                for (int k = 0; k < local3.Count; k++)
                                    outIndices[i].Add((uint)indiceBinder[local3[k]]);
                            }
                            else if (local3.Count == 4)
                            {   //adds the faces in the correct for face culling
                                outIndices[i].Add((uint)indiceBinder[local3[0]]);
                                outIndices[i].Add((uint)indiceBinder[local3[1]]);
                                outIndices[i].Add((uint)indiceBinder[local3[2]]);

                                outIndices[i].Add((uint)indiceBinder[local3[0]]);
                                outIndices[i].Add((uint)indiceBinder[local3[2]]);
                                outIndices[i].Add((uint)indiceBinder[local3[3]]);
                            }
                            else if (local3.Count > 4)
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
            if (unreadableLines.Count > 0)
                COREMain.console.WriteError($" Couldn't read {unreadableLines.Count} lines in {filename}");

            COREMain.console.WriteLine($"finished reading {filename}");

            return true;
        }
    }
}