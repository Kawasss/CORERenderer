using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COREMath
{
    public class Vector2
    {
        public float x = 0;
        public float y = 0;

        public Vector2()
        {
            this.x = 0;
            this.y = 0;
        }

        public Vector2(string x, string y)
        {
            bool sX = float.TryParse(x, NumberStyles.Any, CultureInfo.InvariantCulture, out this.x);
            bool sY = float.TryParse(y, NumberStyles.Any, CultureInfo.InvariantCulture, out this.y);

            if (!sX)
            {
                Console.WriteLine($"Couldn't parse {x}, set to 0");
                this.x = 0;
            } if (!sY)
            {
                Console.WriteLine($"Couldn't parse {y}, set to 0");
                this.y = 0;
            }
        }

        public Vector2(Vector2 v2)
        {
            this.x = v2.x;
            this.y = v2.y;
        }

        public Vector2(float X, float Y)
        {
            this.x = X;
            this.y = Y;
        }

        public static Vector2 Zero = new();

        public static Vector2 UnitVectorX = new(1, 0);
        public static Vector2 UnitVectorY = new(0, 1);

        public static Vector2 operator  - (Vector2 v1, Vector2 v2)
        {
            return new()
            {
                x = v1.x - v2.x,
                y = v1.y - v2.y
            };
        }

        public Vector2 Add(Vector2 vector)
        {
            Vector2 newVector = new()
            {
                x = vector.x + this.x,
                y = vector.y + this.y
            };
            return newVector;
        }

        public static Vector2 operator + (Vector2 v1, Vector2 v2)
        {

            return new()
            {
                x = v1.x + v2.x,
                y = v1.y + v2.y
            };
        }

        public static Vector2 operator * (Vector2 v1, float value)
        {
            return new()
            {
                x = v1.x * value,
                y = v1.y * value
            };
        }

        /// <summary>
        /// Multiplies the vector with another vector
        /// </summary>
        /// <param name="vector"></param>
        public static Vector2 operator * (Vector2 v1, Vector2 v2)
        {
            return new()
            {
                x = v1.x * v2.x,
                y = v1.y * v2.y
            };
        }

        /// <summary>
        /// Prints the current vector to the console
        /// </summary>
        public override string ToString()
        {
            return $"({this.x}, {this.y})";
        }
    }
}
