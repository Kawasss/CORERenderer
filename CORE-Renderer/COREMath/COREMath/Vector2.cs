using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COREMath
{
    public class Vector2
    {
        public float x;
        public float y;

        public Vector2()
        {
            this.x = 0;
            this.y = 0;
        }

        public Vector2(float X, float Y)
        {
            this.x = X;
            this.y = Y;
        }

        public static Vector2 Zero = new(0, 0);

        public static Vector2 UnitVectorX = new(1, 0);
        public static Vector2 UnitVectorY = new(0, 1);

        public Vector2 Subtract(Vector2 vector)
        {
            float newx = this.x - vector.x;
            float newy = this.y - vector.y;

            Vector2 newVector = new(newx, newy);
            return newVector;
        }

        public Vector2 Add(Vector2 vector)
        {
            Vector2 newVector = new(0, 0)
            {
                x = vector.x + this.x,
                y = vector.y + this.y
            };
            return newVector;
        }

        public Vector2 Add(float v1, float v2)
        {
            Vector2 newVector = new(0, 0)
            {
                x = v1 + this.x,
                y = v2 + this.y
            };
            return newVector;
        }

        public Vector2 Scalar(float value)
        {
            Vector2 v1 = new(this.x, this.y);
            v1.x *= value;
            v1.y *= value;

            return v1;
        }

        /// <summary>
        /// Multiplies the vector with another vector
        /// </summary>
        /// <param name="vector"></param>
        public Vector2 MulitplyBy(Vector2 vector)
        {
            Vector2 newVector = new()
            {
                x = this.x * vector.x,
                y = this.y * vector.y
            };

            return newVector;
        }

        /// <summary>
        /// Prints the current vector to the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine($"({this.x}, {this.y})");
        }
    }
}
