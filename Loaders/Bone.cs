using COREMath;
using CORERenderer.OpenGL;

namespace CORERenderer.Loaders
{
    /// <summary>
    /// A bone is linked to the vertices of a submodel
    /// </summary>
    public class Bone
    {
        public static List<Bone> bones = new();

        public Dictionary<Vector3, float> bonesWeightOnVertex = new();
        public Transform transform;
        private int boneID;
        
        public Bone(Vector3 position, Vector3 scaling, Vector3 rotation)
        {
            transform = new(position, rotation, scaling, Vector3.Zero, position);
            bones.Add(this);
            boneID = bones.IndexOf(this);
        }

        /// <summary>
        /// Adds the weight of the bone to a vertex, this will increase the amount of data in the list
        /// </summary>
        /// <param name="vertices"></param>
        public void ApplyWeightsToVertices(Model model)
        {
            foreach (List<Vertex> l1 in model.Vertices)
                foreach (Vertex v in l1)
                    if (MathC.Distance(this.transform.translation, new Vector3(v.x, v.y, v.z)) <= 0.1f)
                        v.AddBone(boneID);
        }
    }
}
