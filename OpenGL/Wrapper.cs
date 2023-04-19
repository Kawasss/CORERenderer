using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.OpenGL
{
    public partial class Rendering : GL
    {
        private static int totalAmountOfTransferredBytes = 0;
        private static int lastAmountOfTransferredBytes = 0;

        public static int unresolvedInstances = 0;
        private static int estimatedDataLoss = 0;
        public static int shaderByteSize = 0;

        public static int TotalAmountOfTransferredBytes { get { return totalAmountOfTransferredBytes; } set { totalAmountOfTransferredBytes += value; lastAmountOfTransferredBytes = value; } }
        public static string TotalAmountOfTransferredBytesString { get { if (totalAmountOfTransferredBytes >= 1000000) return $"{MathF.Round(totalAmountOfTransferredBytes * 0.000001f):N0} MB"; else if (totalAmountOfTransferredBytes >= 1000) return $"{MathF.Round(totalAmountOfTransferredBytes * 0.001f):N0} KB"; else return $"{totalAmountOfTransferredBytes}"; } }
        public static string LastAmountOfTransferredBytesString { get { if (lastAmountOfTransferredBytes >= 1000000) return $"{MathF.Round(lastAmountOfTransferredBytes * 0.000001f):N0} MB"; else if (lastAmountOfTransferredBytes >= 1000) return $"{MathF.Round(lastAmountOfTransferredBytes * 0.001f):N0} KB"; else return $"{lastAmountOfTransferredBytes}"; } }
        public static string EstimatedDataLossString { get { if (estimatedDataLoss >= 1000000) return $"{MathF.Round(estimatedDataLoss * 0.000001f):N0} MB"; else if (estimatedDataLoss >= 1000) return $"{MathF.Round(estimatedDataLoss * 0.001f):N0} KB"; else return $"{estimatedDataLoss}"; } }
        public static string TotalShaderByteSizeString { get { if (shaderByteSize >= 1000000) return $"{MathF.Round(shaderByteSize * 0.000001f):N0} MB"; else if (shaderByteSize >= 1000) return $"{MathF.Round(shaderByteSize * 0.001f):N0} KB"; else return $"{shaderByteSize}"; } }

        public static int drawCalls = 0;

        public static void glBindBuffer(BufferTarget target, uint buffer) => GlBindBuffer((int)target, buffer);

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, ReadOnlySpan<byte> pixels)
        {
            unsafe
            {
                fixed (byte* temp = &pixels[0])
                {
                    IntPtr intptr = new(temp);
                    GlTexImage2D(target, level, internalFormat, width, height, border, format, type, intptr);
                }
            }
            TotalAmountOfTransferredBytes = pixels.Length * sizeof(byte); //bytes are 1 byte of size but still
        }

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, IntPtr pixels)
        {
            unsafe
            {
                GlTexImage2D(target, level, internalFormat, width, height, border, format, type, pixels);
            }
            unresolvedInstances++;
            estimatedDataLoss += width * height * 4;
        }

        public static void glTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int format, int type, byte[] pixels)
        {
            unsafe
            {
                if (pixels != null)
                    fixed (byte* temp = &pixels[0])
                    {
                        IntPtr intptr = new(temp);
                        GlTexImage2D(target, level, internalFormat, width, height, border, format, type, intptr);
                        TotalAmountOfTransferredBytes = pixels.Length;
                    }
                else
                    GlTexImage2D(target, level, internalFormat, width, height, border, format, type, null);
            }
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="first">Specifies the starting index in the enabled arrays.</param>
        /// <param name="count">Specifies the number of indices to be rendered.</param>
        public static void glDrawArrays(PrimitiveType mode, int first, int count)
        {
            drawCalls++;
            GlDrawArrays((int)mode, first, count);
        }

        /// <summary>
        ///     Render primitives from array data.
        /// </summary>
        /// <param name="mode">Specifies what kind of primitives to render.</param>
        /// <param name="count">Specifies the number of elements to be rendered.</param>
        /// <param name="type">
        ///     Specifies the type of the values in indices.
        /// </param>
        /// <param name="indices">Specifies a pointer to the location where the indices are stored.</param>
        public unsafe static void glDrawElements(PrimitiveType mode, int count, GLType type, void* indices)
        {
            drawCalls++;
            GlDrawElements((int)mode, count, (int)type, indices);
        }
    }
}
