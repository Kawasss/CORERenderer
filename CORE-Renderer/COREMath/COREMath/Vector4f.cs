using Microsoft.VisualBasic;
using System.Diagnostics;

namespace COREMath
{
    public class Vector4
    {                     //still needs to be tested for faulty code
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float w { get; set; }

        /// <summary>
        /// Creates a vector with 4 values
        /// </summary>
        /// <param name="vec4X">x</param>
        /// <param name="vec4Y">y</param>
        /// <param name="vec4Z">z</param>
        /// <param name="vec4W">w</param>
        public Vector4(float vec4X, float vec4Y, float vec4Z, float vec4W)
        {
            this.x = vec4X;
            this.y = vec4Y;
            this.z = vec4Z;
            this.w = vec4W;
        }
        /// <summary>
        /// Creates a new vector with the given vector
        /// </summary>
        /// <param name="vector"></param>
        public Vector4(Vector4 vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
            this.w = vector.w;
        }

        /// <summary>
        /// Creates a vector with 4 blank values
        /// </summary>
        public Vector4()
        {
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.w = 1;
        }

        /// <summary>
        /// Scales and translates the current vector
        /// </summary>
        /// <param name="v1">Scaling vector</param>
        /// <param name="v2">Translation vector</param>
        public void Transform(Vector4 v1, Vector4 v2)
        {
            this.x = v1.x * this.x + v2.x;
            this.y = v1.y * this.y + v2.y;
            this.z = v1.z * this.z + v2.z;
        }

        public void Transform(Matrix matrix)
        {
            this.x = matrix.matrix4x4[0, 0] * this.x + matrix.matrix4x4[0, 3];
            this.y = matrix.matrix4x4[1, 1] * this.x + matrix.matrix4x4[1, 3];
            this.z = matrix.matrix4x4[2, 2] * this.z + matrix.matrix4x4[2, 3];
        }

        /// <summary>
        /// Rotates the vector around x axis
        /// </summary>
        /// <param name="angle">angle in degrees</param>
        public void RotateX(float angleF)
        {
            float lY, lZ;
            float angle = angleF * (MathF.PI / 180);

            Vector4 vector = MathC.GetUnitVectorOf(this); //prevents gimbal lock

            lY = (MathF.Cos(angle) * vector.y) - (MathF.Sin(angle) * vector.z);
            lZ = (MathF.Sin(angle) * vector.y) + (MathF.Cos(angle) * vector.z);

            lY *= MathC.GetLengthOf(this);
            lZ *= MathC.GetLengthOf(this);

            this.y = lY;
            this.z = lZ;
        }

        /// <summary>
        /// Rotates the vector around y axis
        /// </summary>
        /// <param name="angle">angle in degrees</param>
        public void RotateY(float angleF)
        {
            float lX, lZ;
            float angle = angleF * (MathF.PI / 180);

            Vector4 vector = MathC.GetUnitVectorOf(this); //prevents gimbal lock

            lX = (MathF.Cos(angle) * vector.x) + (MathF.Sin(angle) * vector.z);
            lZ = (-MathF.Sin(angle) * vector.x) + (MathF.Cos(angle) * vector.z);

            lX *= MathC.GetLengthOf(this);
            lZ *= MathC.GetLengthOf(this);

            this.x = lX;
            this.z = lZ;
        }

        /// <summary>
        /// Rotates the vector around z axis
        /// </summary>
        /// <param name="angle">angle in degrees</param>
        public void RotateZ(float angleF)
        {
            float lX, lY;
            float angle = angleF * (MathF.PI / 180);

            Vector4 vector = MathC.GetUnitVectorOf(this); //prevents gimbal lock
            
            lX = (MathF.Cos(angle) * vector.x) - (MathF.Sin(angle) * vector.y);
            lY = (MathF.Sin(angle) * vector.x) + (MathF.Cos(angle) * vector.y);

            lX *= MathC.GetLengthOf(this);
            lY *= MathC.GetLengthOf(this);

            this.x = lX;
            this.y = lY;
        }



        /// <summary>
        /// Translates the current vector with the given vector
        /// </summary>
        /// <param name="vector"></param>
        public void Translate(Vector4 vector)
        {
            this.Add(vector);
        }

        /// <summary>
        /// Translates the current vector with the given values
        /// </summary>
        /// <param name="fX">x axis transformer</param>
        /// <param name="fY">y axis transformer</param>
        /// <param name="fZ">z axis transformer</param>
        public void Translate(float fX, float fY, float fZ)
        {
            Vector4 vector = new(fX, fY, fZ, 1);

            this.Add(vector);
        }

        /// <summary>
        /// Transform the current vector with the given values
        /// </summary>
        /// <param name="fX">x axis transformer</param>
        /// <param name="fY">y axis transformer</param>
        /// <param name="fZ">z axis transformer</param>
        public void Scale(float fX, float fY, float fZ)
        {
            Vector4 vector = new(fX, fY, fZ, 1);

            this.MulitplyBy(vector);
        }

        /// <summary>
        /// Transform the current vector with the given vector
        /// </summary>
        /// <param name="vector"></param>
        public void Scale(Vector4 vector)
        {
            this.MulitplyBy(vector);
        }

        /// <summary>
        /// Gives a new vector that is the cross product of the two given vectors
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>new vector containing the cross product</returns>
        public Vector4 Cross(Vector4 vector)
        {
            Vector4 newVector = new()
            {
                x = (this.y * vector.z) - (this.z * vector.y),
                y = (this.z * vector.x) - (this.x * vector.z),
                z = (this.x * vector.y) - (this.y * vector.x),
                w = 1
            };
            return newVector;
        }

        /// <summary>
        /// Negates the vector
        /// </summary>
        public void Negate()
        {
            this.Scalar(-1);
        }

        /// <summary>
        /// Returns the dot product of this vector with another
        /// </summary>
        /// <param name="vector">Vector to calculate the dot product with</param>
        /// <returns>The dot product of two vectors as a float</returns>
        public float Dot(Vector4 vector)
        {
            return this.x * vector.x + this.y * vector.y + this.z * vector.z + this.w * vector.w;
        }

        /// <summary>
        /// Normalizes the vector
        /// </summary>
        /// <returns>The normalized vector</returns>
        public void Normalized()
        {
            this.DivideBy(MathC.GetLengthOf(this));
        }

        /// <summary>
        /// Divides the entire vector with another vector
        /// </summary>
        /// <param name="vector">new vector to divide the vector with</param>
        public void DivideBy(Vector4 vector)
        {
            this.x /= vector.x;
            this.y /= vector.y;
            this.z /= vector.z;
        }
        /// <summary>
        /// Divides the entire vector with a given value
        /// </summary>
        /// <param name="dividend">Float to divide the vector with</param>
        public void DivideBy(float dividend)
        {
            this.x /= dividend;
            this.y /= dividend;
            this.z /= dividend;
        }

        /// <summary>
        /// Multiplies the vector with another vector
        /// </summary>
        /// <param name="vector"></param>
        public void MulitplyBy(Vector4 vector)
        {
            this.x *= vector.x; 
            this.y *= vector.y;
            this.z *= vector.z;
        }
        /// <summary>
        /// Multiplies the vector with a scalar
        /// </summary>
        /// <param name="value"></param>
        public void Scalar(float value)
        {
            this.x *= value;
            this.y *= value;
            this.z *= value;
        }

        /// <summary>
        /// makes the current vector the sum of the current vector and the given vector 
        /// </summary>
        /// <param name="vector"></param>
        public void Add(Vector4 vector)
        {
            this.x += vector.x;
            this.y += vector.y;
            this.z += vector.z;
        }
        /// <summary>
        /// Adds a value to the current vector
        /// </summary>
        /// <param name="value"></param>
        public void Add(float value)
        {
            this.x += value;
            this.y += value;
            this.z += value;
        }

        /// <summary>
        /// Subtracts a vector from the current vector
        /// </summary>
        /// <param name="vector"></param>
        public void Min(Vector4 vector)
        {
            this.x -= vector.x;
            this.y -= vector.y;
            this.z -= vector.z;
        }
        /// <summary>
        /// Subtracts a value from the current vector
        /// </summary>
        /// <param name="value"></param>
        public void Min(float value)
        {
            this.x -= value;
            this.y -= value;
            this.z -= value;
        }
        
        /// <summary>
        /// Prints the current vector to the console
        /// </summary>
        public void Print()
        {
            Console.WriteLine($"({this.x}, {this.y}, {this.z}, {this.w})");
        }
    }
}