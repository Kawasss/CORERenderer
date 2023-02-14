using System.Globalization;
using System.Numerics;

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
        public Vector3(Vector2 v1)
        {
            this.x = v1.x;
            this.y = v1.y;
            this.z = 0;
        }

        public static Vector3 Zero = new(0, 0, 0);

        public static Vector3 UnitVectorX = new(1, 0, 0);
        public static Vector3 UnitVectorY = new(0, 1, 0);
        public static Vector3 UnitVectorZ = new(0, 0, 1);

        public static Vector3 operator -(Vector3 v1)
        {
            return new()
            {
                x = -v1.x,
                y = -v1.y,
                z = -v1.z
            };
        }

        public static Vector3 operator - (Vector3 v1, Vector3 v2)
        {
            return new()
            {
                x = v1.x - v2.x,
                y = v1.y - v2.y,
                z = v1.z - v2.z
            };
        }

        public static Vector3 operator + (Vector3 v1, Vector3 v2)
        {
            
            return new()
            {
                x = v1.x + v2.x,
                y = v1.y + v2.y,
                z = v1.z + v2.z
            };
        }

        public static Vector3 operator * (Vector3 v1, float value)
        {
            return new()
            {
                x = v1.x * value,
                y = v1.y * value,
                z = v1.z * value
            };
        }

        /// <summary>
        /// Multiplies the vector with another vector
        /// </summary>
        /// <param name="v1"></param>
        /// <param name = "v2"></param>
        public static Vector3 operator * (Vector3 v1, Vector3 v2)
        {
            return new()
            {
                x = v1.x * v2.x,
                y = v1.y * v2.y,
                z = v1.z * v2.z
            };
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
