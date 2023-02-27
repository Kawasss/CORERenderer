using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using GLFW;
using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;

namespace CORERenderer.shaders
{
    public class Shader
    {
        public readonly uint Handle;
        public string vertexShaderSource;
        public string fragmentShaderSource;
        public string gridShaderSource;
        public string geometryShaderSource = null;

        public int byteSize = 0;

        public Shader(string vertexPath, string fragmentPath)
        {
            //reads all of the shaders
            vertexShaderSource = File.ReadAllText(vertexPath);
            fragmentShaderSource = File.ReadAllText(fragmentPath);

            //links the shaders
            uint vertexShader = glCreateShader(GL_VERTEX_SHADER);
            glShaderSource(vertexShader, vertexShaderSource);

            uint fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
            glShaderSource(fragmentShader, fragmentShaderSource);

            compileShader(vertexShader);
            compileShader(fragmentShader);

            Handle = glCreateProgram();

            glAttachShader(Handle, vertexShader);
            glAttachShader(Handle, fragmentShader);

            linkProgram(Handle);

            //removes the shaders
            glDetachShader(Handle, vertexShader);
            glDetachShader(Handle, fragmentShader);
            glDeleteShader(vertexShader);
            glDeleteShader(fragmentShader);
        }

        public Shader(string vertexPath, string fragmentPath, string geometryPath)
        {
            vertexShaderSource = File.ReadAllText(vertexPath);
            fragmentShaderSource = File.ReadAllText(fragmentPath);
            geometryShaderSource = File.ReadAllText(geometryPath);

            uint vertexShader = glCreateShader(GL_VERTEX_SHADER);
            glShaderSource(vertexShader, vertexShaderSource);

            uint fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
            glShaderSource(fragmentShader, fragmentShaderSource);

            uint geometryShader = glCreateShader(GL_GEOMETRY_SHADER);
            glShaderSource(geometryShader, geometryShaderSource);

            compileShader(vertexShader);
            compileShader(fragmentShader);
            compileShader(geometryShader);

            Handle = glCreateProgram();

            glAttachShader(Handle, vertexShader);
            glAttachShader(Handle, fragmentShader);
            glAttachShader(Handle, geometryShader);

            linkProgram(Handle);

            glDetachShader(Handle, vertexShader);
            glDetachShader(Handle, fragmentShader);
            glDetachShader(Handle, geometryShader);
            glDeleteShader(vertexShader);
            glDeleteShader(fragmentShader);
            glDeleteShader(geometryShader);
        }

        private static void compileShader(uint shader)
        {
            glCompileShader(shader);
            int[] pname = new int[] { 0 };
            glGetShaderiv(shader, GL_COMPILE_STATUS, pname);
            bool successful = pname[0] == GL_TRUE;
            if (!successful)
            {
                COREMain.console.WriteLine($"failed to compile shader {shader}, pname[0] != GL_TRUE");
                COREMain.console.WriteLine(glGetShaderInfoLog(shader));
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
                COREMain.console.WriteLine($"failed to link program {program}, pname[0] != GL_TRUE");
                COREMain.console.WriteLine(glGetProgramInfoLog(program));
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
