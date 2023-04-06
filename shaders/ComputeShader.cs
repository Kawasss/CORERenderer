using COREMath;
using CORERenderer.OpenGL;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.shaders
{
    public class ComputeShader
    {
        public readonly uint Handle;
        public uint byteSize = 0;

        public ComputeShader(string shaderSourceCode)
        {
            if (!shaderSourceCode.ToLower().Contains("void main()"))
                shaderSourceCode = File.ReadAllText(shaderSourceCode);

            uint compute = glCreateShader(GL_COMPUTE_SHADER);
            glShaderSource(compute, shaderSourceCode);
            
            compileShader(compute);

            Handle = glCreateProgram();

            glAttachShader(Handle, compute);
            
            linkProgram(Handle);

            glDetachShader(Handle, compute);
            glDeleteShader(compute);
        }

        private static void compileShader(uint shader)
        {
            glCompileShader(shader);
            int[] pname = new int[] { 0 };
            glGetShaderiv(shader, GL_COMPILE_STATUS, pname);
            bool successful = pname[0] == GL_TRUE;
            if (!successful)
            {
                Console.WriteLine($"failed to compile shader {shader}, pname[0] != GL_TRUE");
                Console.WriteLine(glGetShaderInfoLog(shader));
            }
        }

        private static void linkProgram(uint program)
        {
            glLinkProgram(program);
            int[] pname = new int[] { 0 };
            glGetProgramiv(program, GL_LINK_STATUS, pname);
            bool successful = pname[0] == GL_TRUE;
            if (!successful)
            {
                Console.WriteLine($"failed to link program {program}, pname[0] != GL_TRUE");
                Console.WriteLine(glGetProgramInfoLog(program));
            }
            glGetProgramiv(program, GL_PROGRAM_BINARY_LENGTH, pname);
            Rendering.shaderByteSize += pname[0];
        }

        public void SetInt(string name, int value)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);
            glUniform1i(location, value);
        }

        public void SetBool(string name, bool value)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);
            glUniform1i(location, value ? 1 : 0);
        }

        public void SetFloat(string name, float value)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);
            glUniform1f(location, value);
        }

        public unsafe void SetMatrix(string name, Matrix matrix)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);

            fixed (float* temp = &matrix.matrix4x4[0, 0])
            {
                glUniformMatrix4fv(location, 1, false, temp);
            }
        }

        public unsafe void SetVector3(string name, Vector3 v3)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);
            glUniform3f(location, v3.x, v3.y, v3.z);
        }

        public unsafe void SetVector3(string name, float v1, float v2, float v3)
        {
            glUseProgram(Handle);

            int location = glGetUniformLocation(Handle, name);
            glUniform3f(location, v1, v2, v3);
        }

        public void Use()
        {
            glUseProgram(Handle);
        }

        public int GetAttribLocation(string attribName)
        {
            return glGetAttribLocation(Handle, attribName);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                glDeleteProgram(Handle);
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
