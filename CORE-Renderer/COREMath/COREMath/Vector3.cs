namespace COREMath
{
    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        public Vector3()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
        }

        public Vector3(float X, float Y, float Z)
        {
            this.x = X;
            this.y = Y;
            this.z = Z;
        }

        public static Vector3 Zero = new(0, 0, 0);

        public static Vector3 UnitVectorX = new(1, 0, 0);
        public static Vector3 UnitVectorY = new(0, 1, 0);
        public static Vector3 UnitVectorZ = new(0, 0, 1);

        public Vector3 Subtract(Vector3 vector)
        {
            float newx = this.x - vector.x;
            float newy = this.y - vector.y;
            float newz = this.z - vector.z;

            Vector3 newVector = new(newx, newy, newz);
            return newVector;
        }

        public Vector3 Add(Vector3 vector)
        {
            Vector3 newVector = new(0, 0, 0)
            {
                x = vector.x + this.x,
                y = vector.y + this.y,
                z = vector.z + this.z
            };
            return newVector;
        }

        public Vector3 Scalar(float value)
        {
            Vector3 v1 = new(this.x, this.y, this.z);
            v1.x *= value;
            v1.y *= value;
            v1.z *= value;

            return v1;
        }

        /// <summary>
        /// Multiplies the vector with another vector
        /// </summary>
        /// <param name="vector"></param>
        public Vector3 MulitplyBy(Vector3 vector)
        {
            Vector3 newVector = new();
            newVector.x *= vector.x;
            newVector.y *= vector.y;
            newVector.z *= vector.z;

            return newVector;
        }

        /// <summary>
        /// Prints the current vector to the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine($"({this.x}, {this.y}, {this.z})");
        }
    }
}
