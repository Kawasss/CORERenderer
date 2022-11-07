using System.Globalization;

namespace COREMath
{
    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        public float[] xyz = new float[3];

        public Vector3()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
            for (int i = 0; i < 3; i++)
                this.xyz[i] = 0;
        }

        public Vector3(string x, string y, string z)
        {
            bool sX = float.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out this.x);
            bool sY = float.TryParse(y, NumberStyles.Any, CultureInfo.InvariantCulture, out this.y);
            bool sZ = float.TryParse(z, NumberStyles.Any, CultureInfo.InvariantCulture, out this.z);

            if (!sX)
            {
                Console.WriteLine($"Couldn't parse {x}, set to 0");
                this.x = 0;
            }
            if (!sY)
            {
                Console.WriteLine($"Couldn't parse {y}, set to 0");
                this.y = 0;
            }
            if (!sZ)
            {
                Console.WriteLine($"Couldn't parse {z}, set to 0");
                this.z = 0;
            }
            this.xyz[0] = this.x;
            this.xyz[1] = this.y;
            this.xyz[2] = this.z;
        }

        public Vector3(float X, float Y, float Z)
        {
            this.x = X;
            this.y = Y;
            this.z = Z;
            this.xyz[0] = this.x;
            this.xyz[1] = this.y;
            this.xyz[2] = this.z;
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

        public Vector3 Add(float v1, float v2, float v3)
        {
            Vector3 newVector = new(0, 0, 0)
            {
                x = v1 + this.x,
                y = v2 + this.y,
                z = v3 + this.z
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
            newVector.x = this.x * vector.x;
            newVector.y = this.y * vector.y;
            newVector.z = this.z * vector.z;

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
