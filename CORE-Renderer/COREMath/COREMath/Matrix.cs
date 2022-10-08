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
                { 0, 0, 0, v1.x },
                { 0, 0, 0, v1.y },
                { 0, 0, 0, v1.z },
                { 0, 0, 0,    1 }
             };
            } else
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
                { 0, 0, 0, v1 },
                { 0, 0, 0, v2 },
                { 0, 0, 0, v3 },
                { 0, 0, 0,  1 }
             };
            }
            else
            {
                this.matrix4x4 = new float[4, 4];
            }
        }

        /// <summary>
        /// Makes the current matrix the sum of the current matrix and a given matrix
        /// </summary>
        /// <param name="matrix"></param>
        public void Add(float[,] matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.matrix4x4[i, j] = this.matrix4x4[i, j] + matrix[i, j];
                }
            }
        }

        /// <summary>
        /// Subtracts a matrix from the current matrix
        /// </summary>
        /// <param name="matrix"></param>
        public void Subtract(float[,] matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.matrix4x4[i, j] = this.matrix4x4[i, j] - matrix[i, j];
                }
            }
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
        public void Scalar(float scalar)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    this.matrix4x4[i, j] = this.matrix4x4[i, j] * scalar;
                }
            }
        }

        /// <summary>
        /// Multiplies the current matrix with the given matrix
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns>current vector multiplied with current vector</returns>
        public Matrix MultiplyWith(Matrix classMatrix)
        {
            float[,] matrix = classMatrix.matrix4x4;
            float[,] newMatrix = new float[4, 4];

            for (int h = 0; h < 4; h++)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        newMatrix[j, i] += this.matrix4x4[j, h] * matrix[h, i];
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
        public Vector4 MultiplyWith(Vector4 vector)
        {
            float[] fArray = new float[4];
            float[] axisArray = new float[4] { vector.x, vector.y, vector.z, vector.w };
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    fArray[i] += this.matrix4x4[i, j] * axisArray[j];
                }
            }
            Vector4 newVector = new(fArray[0], fArray[1], fArray[2], fArray[3]);

            return newVector;
        }

        /// <summary>
        /// Does matrix - matrix multiplication on the current matrix class with another matrix
        /// </summary>
        /// <param name="matrix"></param>
        public void MultiplyWith(float[,] matrix)
        {
            float[,] newMatrix = new float[4, 4];

            for (int h = 0; h < 4; h++)
            {
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        newMatrix[j, i] = this.matrix4x4[j, h] * matrix[h, i];
                    }
                }
            }

            newMatrix[3, 3] = 1;
            matrix4x4 = newMatrix;
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
