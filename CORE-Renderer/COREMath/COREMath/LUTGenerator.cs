using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace COREMath
{
    public partial class MathC
    {
        /// <summary>
        /// Creates the lookup table for MathC.Cos so that it can function properly
        /// </summary>
        public static void GenerateLUTCosSinTan()
        {
            //using FileStream file = File.Create($"{path}\\lookUpTables\\CosSinTanLUT.cs");
            using FileStream file = CreateLUT();
            string firstHalf =
            "using System; \n" +
            "\n" +
            "namespace COREMath \n" +
            "{ \n " +
            "partial class MathC \n" +
            "{ \n" +
            "public static double[] cosSinTanLookUpTable = { \n";
            byte[] bytesFirstHalf = Encoding.UTF8.GetBytes(firstHalf);

            file.Write(bytesFirstHalf);

            int j = 0;
            double p = 0;

            while (p < 2 * Math.PI)
            {
                double cos = Math.Cos(p);

                string cosine = string.Format(cos.ToString("F20", InvariantCulture) + ", \n");
                byte[] byteCosine = Encoding.UTF8.GetBytes(cosine);
                file.Write(byteCosine);
                j += 1;
                p += Precision;
                Console.Write($"\rCalculating values... {Math.Round(p / (2 * Math.PI) * 100),1}%");
            }
            Console.WriteLine();
            string secondHalf = $"1.0f}}; \n" +
                                $"public const int cosSinTanLUTSize = {j + 1};}}}} \n";
            byte[] bytesSecondHalf = Encoding.UTF8.GetBytes(secondHalf);
            file.Write(bytesSecondHalf);
        }

        private static FileStream CreateLUT()
        {
            return File.Create($"{path}\\CosSinTanLUT.cs");
        }
    }
}
