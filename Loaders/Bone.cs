using COREMath;

namespace CORERenderer.Loaders
{
    /// <summary>
    /// A bone is linked to the vertices of a submodel
    /// </summary>
    public class Bone
    {
        public Dictionary<Vector3, float> bonesWeightOnVertex = new();
        public Transform transform;
        
        public Bone(Vector3 position, Vector3 scaling, Vector3 rotation)
        {
            transform = new(position, rotation, scaling, Vector3.Zero, position);
        }

        /// <summary>
        /// Adds the weight of the bone to a vertex, this will increase the amount of data in the list
        /// </summary>
        /// <param name="vertices"></param>
        public void ApplyWeightsToVertices(List<float> vertices)
        {

        }
    }
}
