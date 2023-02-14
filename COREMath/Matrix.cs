using System.Numerics;

namespace COREMath
{
    public class Matrix //if doing mathematical functions with matrixes doesnt return the right answers, try changing h,i,j < 4 back to h,i,j < 3
    {
        public float[,] matrix4x4;

        public Matrix()
        {
            matrix4x4 = new float[4, 4];
        }

        public Matrix(float[,] matrix)
        {
            matrix4x4 = matrix;
        }

        /// <summary>
        /// Gives a transformation matrix with the given values
        /// </summary>
        /// <param name="r0c0">Scalar for x</param>
        /// <param name="r1c1">Scalar for y</param>
        /// <param name="r2c2">Scalar for z</param>
        /// <param name="r0c3">Translation for x</param>
        /// <param name="r1c3">Translation for y</param>
        /// <param name="r2c3">Translation for z</param>
        public Matrix(float r0c0, float r1c1, float r2c2, float r0c3, float r1c3, float r2c3)
        {
            this.matrix4x4 = new float[4, 4] 
            {
                { r0c0, 0, 0, r0c3 },
                { 0, r1c1, 0, r1c3 },
                { 0, 0, r2c2, r2c3 },
                { 0, 0, 0,       1 }
             };
        }

        /// <summary>
        /// Gives a transformation matrix with the given vectors
        /// </summary>
        /// <param name="v1">Scaling vector</param>
        /// <param name="v2">Translation vector</param>
        public Matrix(Vector4 v1, Vector4 v2)
        {
            this.matrix4x4 = new float[4, 4]
            {
                { v1.x, 0, 0, v2.x },
                { 0, v1.y, 0, v2.y },
                { 0, 0, v1.z, v2.z },
                { 0, 0, 0,       1 }
             };
        }
       
        /// <summary>
        /// Gives a transformation matrix with the given vectors
        /// </summary>
        /// <param name="v1">Scaling vector</param>
        /// <param name="v2">Translation vector</param>
        public Matrix(Vector3 v1, Vector3 v2)
        {
            this.matrix4x4 = new float[4, 4]
            {
                { v1.x, 0, 0, v2.x },
                { 0, v1.y, 0, v2.y },
                { 0, 0, v1.z, v2.z },
                { 0, 0, 0,       1 }
             };
        }

        /// <summary>
        /// Gives a transformation matrix with the given vectors
        /// </summary>
        /// <param name="v1">Scaling value</param>
        /// <param name="v2">Translation vector</param>
        public Matrix(float v1, Vector3 v2)
        {
            this.matrix4x4 = new float[4, 4]
            {
                { v1, 0, 0, v2.x },
                { 0, v1, 0, v2.y },
                { 0, 0, v1, v2.z },
                { 0, 0, 0,     1 }
             };
        }

        /// <summary>
        /// Gives a scaling or translation matrix of the given vector
        /// </summary>
        /// <param name="isScalingVector">True if the vector is a scaling vector, false if translation vector</param>
        /// <param name="v1">transformation or translation vector</param>
        public Matrix(bool isScalingVector, Vector4 v1)
        {
            if (isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { v1.x, 0, 0, 0 },
                { 0, v1.y, 0, 0 },
                { 0, 0, v1.z, 0 },
                { 0, 0, 0,    1 }
             };
            } else if (!isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { 1, 0, 0, v1.x },
                { 0, 1, 0, v1.y },
                { 0, 0, 1, v1.z },
                { 0, 0, 0,    1 }
             };
            } else
            {
                this.matrix4x4 = new float[4, 4];
            }
        }


        /// <summary>
        /// Gives a scaling or translation matrix of the given vector
        /// </summary>
        /// <param name="isScalingVector">True if the vector is a scaling vector, false if translation vector</param>
        /// <param name="v1">transformation or translation vector</param>
        public Matrix(bool isScalingVector, Vector3 v1)
        {
            if (isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { v1.x, 0, 0, 0 },
                { 0, v1.y, 0, 0 },
                { 0, 0, v1.z, 0 },
                { 0, 0, 0,    1 }
             };
            }
            else if (!isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { 1, 0, 0, v1.x },
                { 0, 1, 0, v1.y },
                { 0, 0, 1, v1.z },
                { 0, 0, 0,    1 }
             };
            }
            else
            {
                this.matrix4x4 = new float[4, 4];
            }
        }

        /// <summary>
        /// Gives a scaling or translation matrix of the given values
        /// </summary>
        /// <param name="isScalingVector">True if the matrix scales, false if it translates</param>
        /// <param name="v1">transformation or translation vector</param>
        /// <param name="v2">transformation or translation vector</param>
        /// <param name="v3">transformation or translation vector</param>
        public Matrix(bool isScalingVector, float v1, float v2, float v3)
        {
            if (isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { v1, 0, 0, 0 },
                { 0, v2, 0, 0 },
                { 0, 0, v3, 0 },
                { 0, 0, 0,  1 }
             };
            }
            else if (!isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { 1, 0, 0, v1 },
                { 0, 1, 0, v2 },
                { 0, 0, 1, v3 },
                { 0, 0, 0,  1 }
             };
            }
            else
            {
                this.matrix4x4 = new float[4, 4];
            }
        }

        public Matrix(bool isScalingVector, float v1)
        {
            if (isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { v1, 0, 0, 0 },
                { 0, v1, 0, 0 },
                { 0, 0, v1, 0 },
                { 0, 0, 0,  1 }
             };
            }
            else if (!isScalingVector)
            {
                this.matrix4x4 = new float[4, 4]
            {
                { 1, 0, 0, v1 },
                { 0, 1, 0, v1 },
                { 0, 0, 1, v1 },
                { 0, 0, 0,  1 }
             };
            }
            else
            {
                this.matrix4x4 = new float[4, 4];
            }
        }

        public static Matrix IdentityMatrix = new(1, 1, 1, 0, 0, 0);

        public static Matrix Createorthographic(float width, float height, float depthNear, float depthFar) => CreateOrthographicOffCenter(-width / 2, width / 2, -height / 2, height / 2, depthNear, depthFar);

        public static Matrix CreateOrthographicOffCenter(float left, float right, float bottom, float top, float depthNear, float depthFar)
        {
            return new()
            {
                matrix4x4 = new float[,]
                {
                    { 2f / (right - left),                   0,                            0,                 -((right + left) / (right - left)) },
                    {                   0, 2f / (top - bottom),                            0,                 -((top + bottom) / (top - bottom)) },
                    {                   0,                   0, -2f / (depthFar - depthNear), -((depthFar + depthNear) / (depthFar - depthNear)) },
                    {                   0,                   0,                            0,                                                  1 },
                }
            };
        }

        public static void CreatePerspectiveOffCenter(float left, float right, float bottom, float top, float depthNear, float depthFar, out Matrix result)
        {
            float x = 2 * depthNear / (right - left);
            float y = 2 * depthNear / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(depthFar + depthNear) / (depthFar - depthNear);
            float d = -(2 * depthFar * depthNear) / (depthFar - depthNear);

            float[,] v1 = { { x, 0, a, 0 },
                            { 0, y, b, 0 },
                            { 0, 0, c, d },
                            { 0, 0,-1, 0 } };
            result = new(v1);
        }

        public static Matrix CreatePerspectiveFOV(float fovY, float aspectRatio, float depthNear, float depthFar)
        {
            if (fovY <= 0 || fovY > MathC.Pi2)
            {
                throw new ArgumentOutOfRangeException(nameof(fovY));
            } if (aspectRatio <= 0) {
                throw new ArgumentOutOfRangeException(nameof(aspectRatio));
            } if (depthNear <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depthNear));
            } if (depthFar <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(depthFar));
            }

            float maximumY = depthNear * MathC.Tan(fovY * 0.5f);
            float minimumY = -maximumY;
            float maximumX = maximumY * aspectRatio;
            float minimumX = minimumY * aspectRatio;

            CreatePerspectiveOffCenter(minimumX, maximumX, minimumY, maximumY, depthNear, depthFar, out Matrix result);
            return result;
        }

        public void FloatToMatrix(float[,] v1)
        {
            v1[0, 0] = this.matrix4x4[0, 0];
            v1[0, 1] = this.matrix4x4[0, 1];
            v1[0, 2] = this.matrix4x4[0, 2];
            v1[0, 3] = this.matrix4x4[0, 3];
            
            v1[1, 0] = this.matrix4x4[1, 0];
            v1[1, 1] = this.matrix4x4[1, 1];
            v1[1, 2] = this.matrix4x4[1, 2];
            v1[1, 3] = this.matrix4x4[1, 3];

            v1[2, 0] = this.matrix4x4[2, 0];
            v1[2, 1] = this.matrix4x4[2, 1];
            v1[2, 2] = this.matrix4x4[2, 2];
            v1[2, 3] = this.matrix4x4[2, 3];

            v1[3, 0] = this.matrix4x4[3, 0];
            v1[3, 1] = this.matrix4x4[3, 1];
            v1[3, 2] = this.matrix4x4[3, 2];
            v1[3, 3] = this.matrix4x4[3, 3];
        }

        /// <summary>
        /// Replaces a specified value with a given value
        /// </summary>
        /// <param name="row">Row of the current value</param>
        /// <param name="column">Column of the current value</param>
        /// <param name="value">Value to replace the current value with</param>
        public void Replace(int row, int column, float value)
        {
            this.matrix4x4[row, column] = value;
        }

        /// <summary>
        /// Multiplies each value of the matrix with a scalar
        /// </summary>
        /// <param name="scalar"></param>
        public static Matrix operator * (Matrix matrix, float scalar)
        {
            Matrix local = new();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    local.matrix4x4[i, j] = matrix.matrix4x4[i, j] * scalar;
                }
            }
            return local;
        }

        /// <summary>
        /// Multiplies the current matrix with the given matrix
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns>current vector multiplied with current vector</returns>
        public static Matrix operator * (Matrix classMatrix, Matrix classMatrix2)
        {
            float[,] matrix = classMatrix.matrix4x4;
            float[,] newMatrix = new float[4, 4];

            for (int h = 0; h < 4; h++)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        newMatrix[j, i] += classMatrix.matrix4x4[j, h] * classMatrix2.matrix4x4[h, i];
                    }
                }
            }

            newMatrix[3, 3] = 1;
            Matrix matrix2 = new(newMatrix);
            return matrix2;
        }

        /// <summary>
        /// Multiplies the current matrix with a given vector, resulting in a new vector
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>A new vector containing the product of the matrix and vector</returns>
        public static Vector4 operator * (Vector4 vector, Matrix matrix)
        {
            float[] fArray = new float[4];
            float[] axisArray = new float[4] { vector.x, vector.y, vector.z, vector.w };
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    fArray[i] += matrix.matrix4x4[i, j] * axisArray[j];
                }
            }
            Vector4 newVector = new(fArray[0], fArray[1], fArray[2], fArray[3]);

            return newVector;
        }

        /// <summary>
        /// Does matrix - matrix multiplication on the current matrix class with another matrix
        /// </summary>
        /// <param name="matrix"></param>
        public static Matrix operator * (Matrix matrix, float[,] matrix2)
        {
            float[,] newMatrix = new float[4, 4];

            for (int h = 0; h < 4; h++)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        newMatrix[j, i] += matrix.matrix4x4[j, h] * matrix2[h, i];
                    }
                }
            }

            newMatrix[3, 3] = 1;
            return new(newMatrix);
        }

        /// <summary>
        /// Switches the rows and columns of the current matrix class
        /// </summary>
        public void SwitchRowsAndColumns()
        {
            float[,] newMatrix = new float[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    newMatrix[j, i] = this.matrix4x4[i, j];
                }
            }
            newMatrix[3, 3] = 1;
            this.matrix4x4 = newMatrix;
        }

        /// <summary>
        /// Switches the rows and columns of the float[,] matrix
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns>returns the given vector, but the columns are the rows and vice versa</returns>
        public void SwitchRowsAndColumns(float[,] matrix)
        {
            float[,] newMatrix = new float[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    newMatrix[j, i] = this.matrix4x4[i, j];
                }
            }
            newMatrix[3, 3] = 1;
            this.matrix4x4 = newMatrix;
        }

        /// <summary>
        /// Prints the current matrix to the console
        /// </summary>
        public void Print()
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Console.Write(matrix4x4[i, j] + " ");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Prints the given matrix
        /// </summary>
        /// <param name="matrix"></param>
        public static void Print(float[,] matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Console.Write(matrix[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}
