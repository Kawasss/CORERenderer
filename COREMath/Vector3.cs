using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

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

        public Vector3(float value)
        {
            x = value;
            y = value;
            z = value;
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

        public static Vector3 Zero { get { return new(0, 0, 0); } }

        public static Vector3 UnitVectorX { get { return new(1, 0, 0); } }
        public static Vector3 UnitVectorY { get { return new(0, 1, 0); } }
        public static Vector3 UnitVectorZ { get { return new(0, 0, 1); } }

        public float Length { get { return MathC.GetLengthOf(this); } }

        public override string ToString()
        {
            return $"{x} {y} {z}";
        }

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

        public static Vector3 operator * (float value, Vector3 v1)
        {
            return new()
            {
                x = value * v1.x,
                y = value * v1.y,
                z = value * v1.z
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

        public static Vector3 operator / (Vector3 v, int i)
        {
            return new()
            {
                x = v.x / i,
                y = v.y / i,
                z = v.z / i
            };
        }

        /// <summary>
        /// Prints the current vector to the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine($"({this.x}, {this.y}, {this.z})");
        }

        /// <summary>
        /// Returns 12 bytes representing the x, y and z values
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                byte[] allBytes = new byte[12];

                byte[] bytesX = BitConverter.GetBytes(x);
                byte[] bytesY = BitConverter.GetBytes(y);
                byte[] bytesZ = BitConverter.GetBytes(z);

                bytesX.CopyTo(allBytes, 0);
                bytesY.CopyTo(allBytes, bytesX.Length);
                bytesZ.CopyTo(allBytes, bytesX.Length + bytesY.Length);

                return allBytes;
            }
        }
    }
}
