using COREMath;
using CORERenderer.Loaders;

namespace CORERenderer.OpenGL
{
    public struct Plane
    {
        Vector3 normal = Vector3.UnitVectorY;
        float distance = 0;

        public Plane(Vector3 normal, float distance)
        {
            this.normal = normal;
            this.distance = distance;
        }
    }

    public struct Frustrum
    {
        public Plane topFace;
        public Plane bottomFace;
        public Plane rightFace;
        public Plane leftFace;
        public Plane farFace;
        public Plane nearFace;
    }
}