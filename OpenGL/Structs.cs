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

    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;

        private const float EPSILON = 0.0000001f;

        public Ray(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public bool Intersects(Model model, out Vector3 intersectionPoint)
        {
            foreach (List<float> allPolyData in model.vertices)
            {
                for (int i = 0; i < allPolyData.Count; i += 24)
                {
                    //Moller-Trumbore intersection algorithm
                    Vector3 firstVertex = new Vector3(allPolyData[i], allPolyData[i + 1], allPolyData[i + 2]) * model.Scaling + model.translation;
                    Vector3 secondVertex = new Vector3(allPolyData[i + 8], allPolyData[i + 9], allPolyData[i + 10]) * model.Scaling + model.translation;
                    Vector3 thirdVertex = new Vector3(allPolyData[i + 16], allPolyData[i + 17], allPolyData[i + 18]) * model.Scaling + model.translation;

                    Vector3 firstEdge = secondVertex - firstVertex;
                    Vector3 secondEdge = thirdVertex - firstVertex;

                    Vector3 h = MathC.GetCrossProduct(this.direction, secondEdge);
                    float a = MathC.GetDotProductOf(firstEdge, h);

                    if (a > -EPSILON && a < EPSILON)
                        continue;
                        
                    float f = 1 / a;
                    Vector3 s = this.origin - firstVertex;
                    float u = f * MathC.GetDotProductOf(s, h);

                    if (u < 0 || u > 1)
                        continue;

                    Vector3 q = MathC.GetCrossProduct(s, firstEdge);
                    float v = f * MathC.GetDotProductOf(this.direction, q);

                    if (v < 0 || v > 1)
                        continue;

                    float t = f * MathC.GetDotProductOf(secondEdge, q);
                    if (t > EPSILON) //intersection
                    {
                        intersectionPoint = this.origin + this.direction * t;
                        return true;
                    }
                    else
                        continue;
                }
            }
            intersectionPoint = Vector3.Zero;
            return false;
        }
    }
}