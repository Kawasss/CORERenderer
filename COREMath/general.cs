using CORERenderer.GLFW;
using System.Numerics;

namespace COREMath
{
    public static partial class MathC
    {
        public const float PiF = 3.14159274f;
        public const double Pi = 3.1415926535897931;
        public const double Pi2 = 3.1415926535897931 * 2;
        public const double Precision = 0.001;

        public const string VERSION = "v1.0";

        public static Matrix LookAt(Vector3 eye, Vector3 target, Vector3 up)
        {
            Vector3 z = Normalize(eye - target); //forward
            Vector3 x = Normalize(GetCrossProduct(up, z)); //right
            Vector3 y = Normalize(GetCrossProduct(z, x)); //camera up

            Matrix matrix = new();

            matrix.matrix4x4[0, 0] = x.x;
            matrix.matrix4x4[0, 1] = x.y;
            matrix.matrix4x4[0, 2] = x.z;
            matrix.matrix4x4[0, 3] = -GetDotProductOf(eye, x);//-((x.x * eye.x) + (x.y * eye.y) + (x.z * eye.z));

            matrix.matrix4x4[1, 0] = y.x;
            matrix.matrix4x4[1, 1] = y.y;
            matrix.matrix4x4[1, 2] = y.z;
            matrix.matrix4x4[1, 3] = -GetDotProductOf(eye, y);//-((y.x * eye.x) + (y.y * eye.y) + (y.z * eye.z));

            matrix.matrix4x4[2, 0] = z.x;
            matrix.matrix4x4[2, 1] = z.y;
            matrix.matrix4x4[2, 2] = z.z;
            matrix.matrix4x4[2, 3] = -GetDotProductOf(eye, z);//-((z.x * eye.x) + (z.y * eye.y) + (z.z * eye.z));

            matrix.matrix4x4[3, 0] = 0;//-(GetDotProductOf(x, eye));
            matrix.matrix4x4[3, 1] = 0;
            matrix.matrix4x4[3, 2] = 0;
            matrix.matrix4x4[3, 3] = 1;

            return matrix;
        }

        public static float Distance(Vector3 v1, Vector3 v2) => GetLengthOf(v1 - v2);

        public static float Clamp(float n, float min, float max)
        {
            return MathF.Max(MathF.Min(n, max), min);
        }

        public static Vector3 Normalize(Vector3 vector)
        {
            float dividend = 1 / GetLengthOf(vector);
            vector.x *= dividend;
            vector.y *= dividend;
            vector.z *= dividend;

            return vector;
        }

        public static float DegToRad(float deg)
        {
            return deg * (PiF / 180);
        }

        public static double DegToRad(double deg)
        {
            return deg * (Pi / 180);
        }

        public static float RadToDeg(float deg)
        {
            return deg * (180f / PiF);
        }

        public static double RadToDeg(double deg)
        {
            return deg * (180 / Pi);
        }

        public static float Tan(float Angle)
        {
            return Sin(Angle) / Cos(Angle);
        }

        public static double Tan(double Angle)
        {
            return Sin(Angle) / Cos(Angle);
        }

        public static float Sin(float Angle)
        {
            return (float)Cos(Angle - 0.5 * Pi);
        }

        public static double Sin(double Angle)
        {
            return Cos(Angle - 0.5 * Pi);
        }

        public static float Cos(float Angle)
        {
            return (float)Cos((double)Angle);
        }

        public static double Cos(double angle)
        {
            angle = Abs(angle);
            double newAngle = Modulus(angle, Pi2);

            double estimation = newAngle * 1000;
            int index = (int)estimation;

            return LERP(estimation - index, cosSinTanLookUpTable[index], cosSinTanLookUpTable[index + 1]);
        }

        public static float Abs(float v1)
        {
            if (v1 < 0)
            {
                v1 = -v1;
            }
            return v1;
        }

        public static double Abs(double v1)
        {
            return (v1 <= 0) ? -v1 : v1;
        }

        /// <summary>
        /// A more well suited version of the modulus operator
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>Returns the modulus value</returns> 
        public static double Modulus(double v1, double v2)
        {
            return v1 - (int)(v1 / v2) * v2;
        }

        public static double LERP(double t, double v1, double v2)
        {
            return (1 - t) * v1 + t * v2;
        }

        /// <summary>
        /// Gives a new vector that is the cross product of the two given vectors
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>new vector containing the cross product</returns>
        public static Vector4 GetCrossProduct(Vector4 vector0, Vector4 vector)
        {
            Vector4 newVector = new()
            {
                x = (vector0.y * vector.z) - (vector0.z * vector.y),
                y = (vector0.z * vector.x) - (vector0.x * vector.z),
                z = (vector0.x * vector.y) - (vector0.y * vector.x),
                w = 1
            };
            return newVector;
        }

        public static Vector3 GetCrossProduct(Vector3 vector0, Vector3 vector)
        {
            Vector3 newVector = new()
            {
                x = (vector0.y * vector.z) - (vector0.z * vector.y),
                y = (vector0.z * vector.x) - (vector0.x * vector.z),
                z = (vector0.x * vector.y) - (vector0.y * vector.x),
            };
            return newVector;
        }

        /// <summary>
        /// Returns the dot product of a vector with another
        /// </summary>
        /// <param name="vector0">Vector to calculate the dot product with</param>
        /// <param name="vector">Second vector to calculate the dot product with</param>
        /// <returns>The dot product of two vectors as a float</returns>
        public static float GetDotProductOf(Vector4 vector0, Vector4 vector)
        {
            return vector0.x * vector.x + vector0.y * vector.y + vector0.z * vector.z + vector0.w * vector.w;
        }

        /// <summary>
        /// Returns the dot product of a vector with another
        /// </summary>
        /// <param name="vector0">Vector to calculate the dot product with</param>
        /// <param name="vector">Second vector to calculate the dot product with</param>
        /// <returns>The dot product of two vectors as a float</returns>
        public static float GetDotProductOf(Vector3 vector0, Vector3 vector)
        {
            return vector0.x * vector.x + vector0.y * vector.y + vector0.z * vector.z;
        }

        /// <summary>
        /// Returns the length / magnitude of given vector
        /// </summary>
        /// <returns>Given vectors length as a float</returns>
        public static float GetLengthOf(Vector4 vector)
        {
            return MathF.Sqrt(Squared(vector.x) + Squared(vector.y) + Squared(vector.z));
        }

        public static float GetLengthOf(Vector3 vector)
        {
            return MathF.Sqrt(Squared(vector.x) + Squared(vector.y) + Squared(vector.z));
        }

        /// <summary>
        /// returns the product of GetRotationXmatrix, GetRotationYmatrix and GetRotationZmatrix
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix GetRotationMatrix(Vector3 v) => GetRotationMatrix(v.x, v.y, v.z);

        /// <summary>
        /// returns the product of GetRotationXmatrix, GetRotationYmatrix and GetRotationZmatrix
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Matrix GetRotationMatrix(float x, float y, float z) => GetRotationXMatrix(x) * GetRotationYMatrix(y) * GetRotationZMatrix(z);

        /// <summary>
        /// Gives a rotation matrix around the x axis with the given angle
        /// </summary>
        /// <param name="degAngle">Angle in degrees</param>
        /// <returns>Rotation matrix around the x axis</returns>
        public static Matrix GetRotationXMatrix(float degAngle)
        {
            degAngle *= (PiF / 180);

            float r1c1 = Cos(degAngle);
            float r1c2 = -Sin(degAngle);
            float r2c1 = Sin(degAngle);
            float r2c2 = Cos(degAngle);

            float[,] fM = new float[4, 4]
            {
                {1, 0,    0,    0},
                {0, r1c1, r1c2, 0},
                {0, r2c1, r2c2, 0},
                {0, 0,    0,    1}
            };
            Matrix matrix = new(fM);

            return matrix;
        }

        /// <summary>
        /// Gives a rotation matrix around the y axis with the given angle
        /// </summary>
        /// <param name="degAngle">Angle in degrees</param>
        /// <returns>Rotation matrix around the y axis</returns>
        public static Matrix GetRotationYMatrix(float degAngle)
        {
            degAngle *= (PiF / 180);

            float r0c0 = Cos(degAngle);
            float r0c2 = Sin(degAngle);
            float r2c0 = -Sin(degAngle);
            float r2c2 = Cos(degAngle);

            float[,] fM = new float[4, 4]
            {
                {r0c0, 0, r0c2, 0},
                {0,    1,    0, 0},
                {r2c0, 0, r2c2, 0},
                {0, 0,    0,    1}
            };
            Matrix matrix = new(fM);

            return matrix;
        }

        /// <summary>
        /// Gives a rotation matrix around the z axis with the given angle
        /// </summary>
        /// <param name="degAngle">Angle in degrees</param>
        /// <returns>Rotation matrix around the z axis</returns>
        public static Matrix GetRotationZMatrix(float degAngle)
        {
            degAngle *= (PiF / 180);

            float r0c0 = Cos(degAngle);
            float r0c1 = -Sin(degAngle);
            float r1c0 = Sin(degAngle);
            float r1c1 = Cos(degAngle);

            float[,] fM = new float[4, 4]
            {
                {r0c0, r0c1, 0, 0},
                {r1c0, r1c1, 0, 0},
                {   0,    0, 1, 0},
                {   0,    0, 0, 1}
            };
            Matrix matrix = new(fM);

            return matrix;
        }

        /// <summary>
        /// Gives the translation matrix of a translation vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Matrix version of the translation vector</returns>
        public static Matrix GetTranslationMatrix(Vector4 vector)
        {
            Matrix matrix = new(false, vector);
            return matrix;
        }

        /// <summary>
        /// Gives the translation matrix of the translation values
        /// </summary>
        /// <param name="v1">translate x</param>
        /// <param name="v2">translate y</param>
        /// <param name="v3">translate z</param>
        /// <returns>Matrix version of the translation values</returns>
        public static Matrix GetTranslationMatrix(float v1, float v2, float v3)
        {
            Matrix matrix = new(false, v1, v2, v3);
            return matrix;
        }

        public static Matrix GetTranslationMatrix(Vector3 vector)
        {
            Matrix matrix = new(false, vector.x, vector.y, vector.z);
            return matrix;
        }

        /// <summary>
        /// Gives the scaling matrix of a scaling vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Matrix version of the scaling vector</returns>
        public static Matrix GetScalingMatrix(Vector4 vector)
        {
            Matrix matrix = new(true, vector);
            return matrix;
        }

        public static Matrix GetScalingMatrix(float v1)
        {
            Matrix matrix = new(true, v1, v1, v1);
            return matrix;
        }

        /// <summary>
        /// Gives the scaling matrix of the scaling values
        /// </summary>
        /// <param name="v1">scaling x</param>
        /// <param name="v2">scaling y</param>
        /// <param name="v3">scaling z</param>
        /// <returns>Matrix version of the scaling values</returns>
        public static Matrix GetScalingMatrix(float v1, float v2, float v3)
        {
            Matrix matrix = new(true, v1, v2, v3);
            return matrix;
        }

        public static Matrix GetScalingMatrix(Vector3 v)
        {
            Matrix matrix = new(true, v);
            return matrix;
        }

        /// <summary>
        /// Gives the number to the power of 2
        /// </summary>
        /// <param name="root">float that needs to be squared</param>
        /// <returns>A squared float</returns>
        public static float Squared(float root)
        {
            return (root * root);
        }
        
        /// <summary>
        /// Gives the number to the given power
        /// </summary>
        /// <param name="root">float that needs to be squared</param>
        /// <param name="AmountSquared">The power to calculate the squared root with</param>
        /// <returns></returns>
        public static float Squared(float root, float AmountSquared)
        {
            float result = root;
            for (int i = 0; i < AmountSquared - 1; i++)
            {
                result *= root;
            }
            return result;
        }

        /// <summary>
        /// Gives the unit vector of the given vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>unit vector of the given vector</returns>
        public static Vector4 GetUnitVectorOf(Vector4 vector)
        {
            Vector4 NVector = new(vector);
            NVector.Normalized();

            return NVector;
        }
    }
}
