using CORERenderer.shaders;
using CORERenderer.Loaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.GL;
using COREMath;

namespace CORERenderer
{
    /// <summary>
    /// A lamp is only visible with the ray-tracing feature, it is invisible with rasterization
    /// </summary>
    internal class Lamp
    {
        internal Vector3 position;
        internal Vector3 color;
        internal float radius;

        internal uint ssbo;

        private ComputeShader computeShader;

        internal Lamp(Vector3 position, Vector3 color, float radius)
        {
            this.position = position;
            this.color = color;
            this.radius = radius;
        }

        internal void BindTo(ComputeShader compShader)
        {
            computeShader = compShader;

            computeShader.Use();

            ssbo = glGenBuffer();
            glBindBuffer(GL_SHADER_STORAGE_BUFFER, ssbo);

            uint blockIndex = glGetProgramResourceIndex(computeShader.Handle, GL_SHADER_STORAGE_BLOCK, "Lamp");
            uint bindingIndex = 2;

            glBufferData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 7, (IntPtr)null, GL_DYNAMIC_DRAW);

            #region repetitive assigning of values in buffer, using vec3's in an SSBO is in general ill-advised
            unsafe
            {
                fixed (float* temp = &position.x)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, 0, sizeof(float), temp);
                }
                fixed (float* temp = &position.y)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float), sizeof(float), temp);
                }
                fixed (float* temp = &position.z)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 2, sizeof(float), temp);
                }
                fixed (float* temp = &color.x)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 3, sizeof(float), temp);
                }
                fixed (float* temp = &color.y)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 4, sizeof(float), temp);
                }
                fixed (float* temp = &color.z)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 5, sizeof(float), temp);
                }
                fixed (float* temp = &radius)
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_SHADER_STORAGE_BUFFER, sizeof(float) * 6, sizeof(float), temp);
                }
            }
            #endregion

            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, bindingIndex, ssbo);
        }
    }
}
