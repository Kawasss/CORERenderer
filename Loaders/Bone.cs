using COREMath;
using CORERenderer.OpenGL;
using CORERenderer.Main;

namespace CORERenderer.Loaders
{
    /// <summary>
    /// A bone is linked to the vertices of a submodel
    /// </summary>
    public class Bone
    {
        public static List<Bone> bones = new();
        public Transform transform;
        private int boneID;
        private Line line = null;
        private Vector3 top = Vector3.UnitVectorY;
        public Vector3 bottom = Vector3.Zero;
        
        public Bone(Vector3 positionBottom, Vector3 positionTop, Vector3 scaling, Vector3 rotation)
        {
            Vector3 average = (positionTop + positionBottom) / 2;
            transform = new(positionBottom, rotation, scaling, new(.1f, MathC.Abs((average.y - positionBottom.y) / 2), .1f), average);
            top = positionTop;
            bottom = positionBottom;
            bones.Add(this);
            boneID = bones.Count - 1;
        }

        /// <summary>
        /// Adds the weight of the bone to a vertex, this will increase the amount of data in the list
        /// </summary>
        /// <param name="vertices"></param>
        public void ApplyWeightsToVertices(Model model)
        {
            foreach (List<Vertex> l1 in model.Vertices)
                foreach (Vertex v in l1)
                    if (transform.BoundingBox.CollidsWith((new Vector4(v.x, v.y, v.z, 1) * model.Transform.ModelMatrix).xyz))
                    {
                        Console.WriteLine("collision");
                        v.AddBone(boneID);
                    }
        }

        public void Render()
        {
            line ??= new(bottom, top, new(1, 1, 1));
            Line.lineWidth = 1;
            line.model = transform.ModelMatrix;
            line.Render();
        }

        public void DebugUpdate()
        {
            this.transform.rotation.x += 1;
        }
    }
}
