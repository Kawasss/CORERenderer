using COREMath;
using CORERenderer.OpenGL;

namespace CORERenderer
{
    public class Transform
    {
        public Vector3 translation = Vector3.Zero, scale = new(1, 1, 1), rotation = Vector3.Zero;

        private Vector3 previousTranslation = Vector3.Zero, previousScale = new(1, 1, 1), previousRotation = Vector3.Zero;
        private Matrix model = Matrix.IdentityMatrix;
        private AABB boundingBox;
        public AABB BoundingBox { get { return boundingBox; } set { boundingBox = value; } }


        public Matrix ModelMatrix { get 
            { 
                if (previousRotation != rotation || previousScale != scale || previousTranslation != translation)
                    model = Matrix.IdentityMatrix * MathC.GetRotationMatrix(this.rotation) * MathC.GetTranslationMatrix(this.translation) * MathC.GetScalingMatrix(this.scale);
                return model;
            } }

        public Vector3 Right { get { return new(model.matrix4x4[0, 0], model.matrix4x4[0, 1], model.matrix4x4[0, 2]); } }
        public Vector3 Up { get { return new(model.matrix4x4[1, 0], model.matrix4x4[1, 1], model.matrix4x4[1, 2]); } }
        public Vector3 Backward { get { return new(model.matrix4x4[2, 0], model.matrix4x4[2, 1], model.matrix4x4[2, 2]); } }
        public Vector3 Forward { get { return -new Vector3(model.matrix4x4[2, 0], model.matrix4x4[2, 1], model.matrix4x4[2, 2]); } }
        public Vector3 GlobalScale { get { return new(Right.Length, Up.Length, Backward.Length); } }

        public Transform() { translation = Vector3.Zero; scale = new(1, 1, 1); rotation = Vector3.Zero; }

        public Transform(Vector3 translation, Vector3 rotation, Vector3 scale, Vector3 extents, Vector3 center)
        {
            this.translation = translation;
            this.rotation = rotation;
            this.scale = scale;

            boundingBox = new(center, extents.x, extents.y, extents.z);
        }
    }
}
