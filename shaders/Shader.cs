using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.OpenGL;
using System.Text.RegularExpressions;
using Console = CORERenderer.GUI.Console;
using System.Diagnostics;

namespace CORERenderer.shaders
{
    public class Shader
    {
        public static Dictionary<uint, Shader> HandleShaderPair = new();

        public readonly uint Handle;
        public string vertexShaderSource;
        public string fragmentShaderSource;
        public string gridShaderSource;
        public string geometryShaderSource = null;

        public int SizeInVRAM { get { int[] pname = new int[] { 0 }; glGetProgramiv(Handle, GL_PROGRAM_BINARY_LENGTH, pname); return pname[0]; } }

        private Dictionary<string, int> uniformLocations = new();
        private List<Attribute> attributes = new();

        private long vertexCompilationTime = 0;
        private long fragCompilationTime = 0;
        private long geomCompilationTime = 0;
        private bool compiledSuccessfully = false;
        public bool IsCompiled { get => compiledSuccessfully; }

        private long linkingTime = 0;
        private bool linkedSuccessfully = false;
        public long LinkingTime { get => linkingTime; }
        public bool IsLinked { get => linkedSuccessfully; }

        public string StartLog
        {
            get
            {
                string geomText = geometryShaderSource != null ? $"{geomCompilationTime} ms" : "N.A.";
                string attributesS = "";
                foreach (Attribute a in attributes)
                    attributesS += $"       {Rendering.GLTypeToString[a.type.type]} {a.name}\n";
                return $""""
                Shader {Handle}:
                    Compiled: {compiledSuccessfully}
                        vertex compilation: {vertexCompilationTime} ms
                        fragment compilation: {fragCompilationTime} ms
                        geometry compilation: {geomText}
                    Linked: {linkedSuccessfully}
                        linking: {linkingTime} ms
                    Size: {SizeInVRAM} bytes
                    Attributes found: {attributes.Count}
                {attributesS}
                """";
            }
                
        }

        public Shader(string vertexPath, string fragmentPath)
        {
            vertexShaderSource = !vertexPath.ToLower().Contains("void main()") ? vertexShaderSource = File.ReadAllText(vertexPath) : vertexPath;

            fragmentShaderSource = !fragmentPath.ToLower().Contains("void main()") ? fragmentShaderSource = File.ReadAllText(fragmentPath) : fragmentPath;

            //links the shaders
            uint vertexShader = glCreateShader(GL_VERTEX_SHADER);
            glShaderSource(vertexShader, vertexShaderSource);

            uint fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
            glShaderSource(fragmentShader, fragmentShaderSource);

            bool vertexCompiled = compileShader(vertexShader, out vertexCompilationTime);
            bool FragCompiled = compileShader(fragmentShader, out fragCompilationTime);

            compiledSuccessfully = vertexCompiled && FragCompiled;

            Handle = glCreateProgram();

            glAttachShader(Handle, vertexShader);
            glAttachShader(Handle, fragmentShader);

            linkedSuccessfully = linkProgram(Handle, out linkingTime);

            //removes the shaders
            glDetachShader(Handle, vertexShader);
            glDetachShader(Handle, fragmentShader);
            glDeleteShader(vertexShader);
            glDeleteShader(fragmentShader);

            SetAttributes(vertexShaderSource);

            if (!HandleShaderPair.ContainsKey(Handle))
                HandleShaderPair.Add(Handle, this);
        }

        public Shader(string vertexPath, string fragmentPath, string geometryPath)
        {
            vertexShaderSource = !vertexPath.ToLower().Contains("void main()") ? vertexShaderSource = File.ReadAllText(vertexPath) : vertexPath;

            fragmentShaderSource = !fragmentPath.ToLower().Contains("void main()") ? fragmentShaderSource = File.ReadAllText(fragmentPath) : fragmentPath;

            geometryShaderSource = !geometryPath.ToLower().Contains("void main()") ? geometryShaderSource = File.ReadAllText(geometryPath) : geometryPath;

            uint vertexShader = glCreateShader(GL_VERTEX_SHADER);
            glShaderSource(vertexShader, vertexShaderSource);

            uint fragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
            glShaderSource(fragmentShader, fragmentShaderSource);

            uint geometryShader = glCreateShader(GL_GEOMETRY_SHADER);
            glShaderSource(geometryShader, geometryShaderSource);

            bool vertexCompiled = compileShader(vertexShader, out vertexCompilationTime); //compileShader(vertexShader);
            bool FragCompiled = compileShader(fragmentShader, out fragCompilationTime); //compileShader(fragmentShader);
            bool geomCompiled = compileShader(geometryShader, out geomCompilationTime); //compileShader(geometryShader);

            compiledSuccessfully = vertexCompiled && FragCompiled && geomCompiled;

            Handle = glCreateProgram();

            glAttachShader(Handle, vertexShader);
            glAttachShader(Handle, fragmentShader);
            glAttachShader(Handle, geometryShader);

            linkedSuccessfully = linkProgram(Handle, out linkingTime);

            glDetachShader(Handle, vertexShader);
            glDetachShader(Handle, fragmentShader);
            glDetachShader(Handle, geometryShader);
            glDeleteShader(vertexShader);
            glDeleteShader(fragmentShader);
            glDeleteShader(geometryShader);

            SetAttributes(vertexShaderSource);

            if (!HandleShaderPair.ContainsKey(Handle))
                HandleShaderPair.Add(Handle, this);
        }

        private static bool compileShader(uint shader, out long time)
        {
            Stopwatch timer = new();
            timer.Start();

            glCompileShader(shader);
            int[] pname = new int[] { 0 };
            glGetShaderiv(shader, GL_COMPILE_STATUS, pname);
            bool successful = pname[0] == GL_TRUE;
            if (!successful)
            {
                Console.WriteError($"failed to compile shader {shader}, pname[0] != GL_TRUE");
                Console.WriteError(glGetShaderInfoLog(shader));
            }
            timer.Stop();
            time = timer.ElapsedMilliseconds;

            return successful;
        }

        private static bool linkProgram(uint program, out long time)
        {
            Stopwatch timer = new();
            timer.Start();

            glLinkProgram(program);
            int[] pname = new int[] { 0 };
            glGetProgramiv(program, GL_LINK_STATUS, pname);
            bool successful = pname[0] == GL_TRUE;
            if (!successful)
            {
                Console.WriteError($"failed to link program {program}, pname[0] != GL_TRUE");
                Console.WriteError(glGetProgramInfoLog(program));
            }
            glGetProgramiv(program, GL_PROGRAM_BINARY_LENGTH, pname);
            Rendering.shaderByteSize += pname[0];

            timer.Stop();
            time = timer.ElapsedMilliseconds;

            return successful;
        }

        private void SetAttributes(string vertexShaderText)
        {
            try
            {
                string allDeclarations = vertexShaderText[..vertexShaderText.IndexOf("void main()")];

                string[] declarations = allDeclarations.Split(new string[] { ";" }, StringSplitOptions.None);

                for (int i = 0; i < declarations.Length; i++)
                    if (declarations[i].ToLower().Contains("in "))
                        ParseAttribute(declarations[i]);
            }
            catch (Exception err)
            {
                Console.WriteError($"Couldn't parse the attributes: {err}");
                attributes.Clear();
                return;
            }
        }

        private void ParseAttribute(string declaration)
        {
            string[] parsedDeclaration = declaration.Split(new string[] { " " }, StringSplitOptions.TrimEntries);
            attributes.Add(new(this, parsedDeclaration[^1], GLSLTypeLengthTable[parsedDeclaration[^2]]));

            if (attributes[^1].location != -1)
                return;

            string trimmedText = string.Concat(declaration.Where(c => c != ' '));
            if (trimmedText.Contains("location="))
            {
                int index = trimmedText.IndexOf("location=") + 9;
                Attribute g = new(this, parsedDeclaration[^1], GLSLTypeLengthTable[parsedDeclaration[^2]]);

                g.location = int.Parse(trimmedText[index..trimmedText.IndexOf(')')]);
                attributes[^1] = g;
                return;
            }
            Console.WriteError($"Unknown location ({attributes[^1].location}) for variable {attributes[^1].name} ({declaration})");
        }

        private struct Attribute
        {
            public string name;
            public int location;
            public GLSLType type;

            public Attribute(string name, GLSLType glslType, int location)
            {
                this.name = name;
                this.location = location;
                this.type = glslType;
            }

            public Attribute(Shader shader, string name, GLSLType glslType)
            {
                this.name = name;
                this.location = shader.GetAttribLocation(name);
                this.type = glslType;
            }
        }
        private struct GLSLType
        {
            public int elements;
            public int sizeInBytes;
            public int type;
            
            public GLSLType(int elements, int sizeInBytesOfSingleElement, int type)
            {
                this.elements = elements;
                this.sizeInBytes = sizeInBytesOfSingleElement * elements;
                this.type = type;
            }
        }
        private static Dictionary<string, GLSLType> GLSLTypeLengthTable = new()
        {
            {"bool", new(1, sizeof(bool), GL_BOOL)}, {"int", new(1, sizeof(int), GL_INT)},  {"uint", new(1, sizeof(uint), GL_UNSIGNED_INT)}, {"float", new(1, sizeof(float), GL_FLOAT)}, {"double", new(1, sizeof(double), GL_DOUBLE)},
            {"vec2", new(2, sizeof(float), GL_FLOAT) }, {"vec3", new(3, sizeof(float), GL_FLOAT) }, {"vec4", new(4, sizeof(float), GL_FLOAT) }, {"ivec2", new(2, sizeof(int), GL_INT) }, {"ivec3", new(3, sizeof(int), GL_INT) }, {"ivec4", new(4, sizeof(int), GL_INT) },
            {"bvec2", new(2, sizeof(bool), GL_BOOL) }, {"bvec3", new(3, sizeof(bool), GL_BOOL) }, {"bvec4", new(4, sizeof(bool), GL_BOOL) }, {"uvec2", new(2, sizeof(uint), GL_UNSIGNED_INT) }, {"uvec3", new(3, sizeof(uint), GL_UNSIGNED_INT) }, {"uvec4", new(4, sizeof(uint), GL_UNSIGNED_INT) },
            {"dvec2", new(2, sizeof(double), GL_DOUBLE) }, {"dvec3", new(3, sizeof(double), GL_DOUBLE) }, {"dvec4", new(4, sizeof(double), GL_DOUBLE) }
        };

        /// <summary>
        /// Activates all of the attributes found when the shader was created
        /// </summary>
        public void ActivateAttributes()
        {
            if (attributes.Count < 1)
            {
                Console.WriteError($"No attributes found for shader {this.Handle}, refrain from using this method if the shader is bufferless");
                return;
            }

            int stride = 0;
            int pointer = 0;
            foreach (Attribute attribute in attributes)
                stride += attribute.type.sizeInBytes;
            foreach (Attribute attribute in attributes)
            {
                unsafe { glVertexAttribPointer((uint)attribute.location, attribute.type.elements, attribute.type.type, false, stride, (void*)pointer); }
                glEnableVertexAttribArray((uint)attribute.location);
                //Console.WriteLine($"Actived attribute with location {attribute.location}, size {attribute.type.elements}, type {nameof(attribute.type.type)}, stride {stride}, pointer {pointer}");
                pointer += attribute.type.sizeInBytes;
            }
        }

        private int GetUniformLocation(string name) //caches the location of uniform variables so they can be found faster, string comparisons arent cheap
        {
            if (!uniformLocations.ContainsKey(name))
                uniformLocations.Add(name, glGetUniformLocation(Handle, name));

            if (uniformLocations[name] == -1)
            {
                uniformLocations.Remove(name);
                uniformLocations.Add(name, glGetUniformLocation(Handle, name));

                if (uniformLocations[name] == -1)
                {
                    Console.WriteError($"Invalid uniform {name} (location == -1)");
                    return -1;
                }
            }
            return uniformLocations[name];
        }

        public void SetSampler(string name, ActiveTexture texture) => SetInt(name, ATToInt[texture]);

        //seems dumb but GL_TEXTURE0 etc isnt 0 but another value
        private Dictionary<ActiveTexture, int> ATToInt = new() { { ActiveTexture.Texture0, 0}, { ActiveTexture.Texture1, 1 }, { ActiveTexture.Texture2, 2 }, { ActiveTexture.Texture3, 3 }, { ActiveTexture.Texture4, 4 }, { ActiveTexture.Texture5, 5 },
        { ActiveTexture.Texture6, 6}, { ActiveTexture.Texture7, 7}, { ActiveTexture.Texture8, 8}, { ActiveTexture.Texture9, 9}, { ActiveTexture.Texture10, 10}, { ActiveTexture.Texture11, 11}, { ActiveTexture.Texture12, 12}, { ActiveTexture.Texture13, 13},
        { ActiveTexture.Texture14, 14}, { ActiveTexture.Texture15, 15}, { ActiveTexture.Texture16, 16}, { ActiveTexture.Texture17, 17}, { ActiveTexture.Texture18, 18}, { ActiveTexture.Texture19, 19}, { ActiveTexture.Texture20, 20}, { ActiveTexture.Texture21, 21},
        { ActiveTexture.Texture22, 22}, { ActiveTexture.Texture23, 23}, { ActiveTexture.Texture24, 24}, { ActiveTexture.Texture25, 25}, { ActiveTexture.Texture26, 26}, { ActiveTexture.Texture27, 27}, { ActiveTexture.Texture28, 28}, { ActiveTexture.Texture29, 29},
        { ActiveTexture.Texture30, 30}, { ActiveTexture.Texture31, 31} };

        public void SetInt(string name, int value)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name + char.MinValue);

            GetError();
            glUniform1i(location, value);
        }

        public void SetBool(string name, bool value)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name);

            glUniform1i(location, value ? 1 : 0);
        }

        public void SetFloat(string name, float value)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name);

            glUniform1f(location, value);
        }

        public unsafe void SetMatrix(string name, Matrix matrix)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name);

            fixed (float* temp = &matrix.matrix4x4[0, 0])
            {
                glUniformMatrix4fv(location, 1, false, temp);
            }
        }

        public unsafe void SetVector3(string name, Vector3 v3)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name);

            glUniform3f(location, v3.x, v3.y, v3.z);
        }

        public unsafe void SetVector3(string name, float v1, float v2, float v3)
        {
            glUseProgram(Handle);

            int location = GetUniformLocation(name);

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