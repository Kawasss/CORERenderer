using System;
using GLFW;
using COREMath;
using static OpenGL.GL;
using System.Security.Cryptography.X509Certificates;
using CORE_Renderer;
using OpenGL;
using System.Runtime.CompilerServices;

namespace openGLToturial
{
    class CORERenderContent : Overrides
    {
        static private Shader shader;
        
        static private Texture texture;
        static private Texture texture2;
       
        static private Matrix view;
        static private Matrix projection;
        static private Matrix model;

        static private Camera camera;

        static private uint vertexBufferObject;
        static private uint vertexArrayObject;
        static private double time;

        static private bool firstMove = true;
        static double mousePosXD;
        static double mousePosYD;
        static float mousePosX;
        static float mousePosY;
        static Vector3 lastPos;

        static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string? directory = System.IO.Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        static readonly float[] vertices = {
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,

            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,  1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, 1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f,
             0.5f,  0.5f, -0.5f,  1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,  1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f, 1.0f
        };

        static readonly uint[] indices = { 0, 1, 3,
                                  1, 2, 3
                                };

        public unsafe override void OnLoad()
        {
            MathC.Initialize(false);

            glEnable(GL_DEPTH_TEST);

            //initialises given shaders
            shader = new Shader($"{pathRenderer}\\shaders\\shader.vert", $"{pathRenderer}\\shaders\\shader.frag");
            shader.Use();

            //making VBO and assigning it the vertices
            vertexBufferObject = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, vertexBufferObject);
            fixed (float* temp = &vertices[0])
            {
                IntPtr ptr = new IntPtr(temp);
                glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), ptr, GL_STATIC_DRAW);
            }
            Console.WriteLine("Successfully initialised VBO");

            //making VAO and assigning it the vertices
            uint elementBufferObject;

            vertexArrayObject = glGenVertexArray();
            glBindVertexArray(vertexArrayObject);

            elementBufferObject = glGenBuffer();
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, elementBufferObject);
            fixed (uint* temp = &indices[0])
            {
                IntPtr ptr = new IntPtr(temp);
                glBufferData(GL_ELEMENT_ARRAY_BUFFER, vertices.Length * sizeof(uint), ptr, GL_STATIC_DRAW);
            }
            Console.WriteLine("Successfully initialised VAO");

            //telling the vertexAttribArray the amount of values to add (3 values, when it repeats and when it starts)
            int vertexLocation = shader.GetAttribLocation("aPos");
            glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 5 * sizeof(float), (void*)0);
            glEnableVertexAttribArray(0);

            Console.WriteLine("Successfully assigned vertex shader values");

            //telling the vertexAttribArray the amount of values to add (2 values, when it repeats and when it starts)
            int texCoordLocation = shader.GetAttribLocation("aTexCoord");
            glEnableVertexAttribArray((uint)texCoordLocation);
            glVertexAttribPointer((uint)texCoordLocation, 2, GL_FLOAT, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
            glEnableVertexAttribArray(2);


            texture = Texture.ReadFromFile($"{pathRenderer}\\textures\\container.png");
            texture.Use(GL_TEXTURE0);

            Console.WriteLine("successfully initialised texture 0");

            texture2 = Texture.ReadFromFile($"{pathRenderer}\\textures\\water.png");
            texture2.Use(GL_TEXTURE1);

            Console.WriteLine("successfully initialised texture 1");

            shader.SetInt("texture0", 0);
            shader.SetInt("texture1", 1);

            Console.WriteLine("Successfully assigned textures");

            Vector3 newVector = new(0, 0, 3f);
            camera = new Camera(newVector, COREMain.WIDTH / COREMain.HEIGHT);

            //view = MathC.GetTranslationMatrix(0, 0, -3f);
            //projection = Matrix.CreatePerspectiveFOV(MathC.DegToRad(120), COREMain.WIDTH / COREMain.HEIGHT, 0.1f, 100);

            Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

            Console.WriteLine($"Initialised in {Glfw.Time} seconds");
            Console.WriteLine("Beginning render loop");
        }

        public unsafe override void RenderEveryFrame()
        {
            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            //applies the shaders and textures
            texture.Use(GL_TEXTURE0);
            texture2.Use(GL_TEXTURE1);
            shader.Use();

            model = Matrix.IdentityMatrix.MultiplyWith(MathC.GetRotationXMatrix((float)Glfw.Time * 30));

            shader.SetMatrix("model", model);
            shader.SetMatrix("view", camera.GetViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            //draws the final polygons
            glBindVertexArray(vertexArrayObject);
            //glDrawElements(GL_TRIANGLES, indices.Length, GL_UNSIGNED_INT, (void*)3);
            glDrawArrays(GL_TRIANGLES, 0, 36);
            Glfw.SwapBuffers(COREMain.window);
        }

        public override void EveryFrame(Window window, float delta)
        {
            Glfw.GetCursorPosition(window, out mousePosXD, out mousePosYD);
            mousePosX = (float)mousePosXD;
            mousePosY = (float)mousePosYD;

            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }

            const float CAMERA_SPEED = 1.1f;
            const float SENSITIVITY = 0.1f;

            if (Glfw.GetKey(window, Keys.W) == InputState.Press)
            {
                Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Add(v1);
            }
            if (Glfw.GetKey(window, Keys.S) == InputState.Press)
            {
                Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Subtract(v1);
            }
            if (Glfw.GetKey(window, Keys.A) == InputState.Press)
            {
                Vector3 v1 = camera.right.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Add(v1);
            }
            if (Glfw.GetKey(window, Keys.D) == InputState.Press)
            {
                Vector3 v1 = camera.right.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Subtract(v1);
            }
            if (Glfw.GetKey(window, Keys.Space) == InputState.Press)
            {
                Vector3 v1 = camera.up.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Add(v1);
            }
            if (Glfw.GetKey(window, Keys.LeftShift) == InputState.Press)
            {
                Vector3 v1 = camera.up.Scalar(CAMERA_SPEED * delta);
                camera.position = camera.position.Subtract(v1);
            }

            if (firstMove)
            {
                lastPos = new(mousePosX, 0, mousePosY);
                firstMove = false;
            } else
            {
                float deltaX = mousePosX - lastPos.x;
                float deltaY = mousePosY - lastPos.z;
                
                lastPos = new(mousePosX, 0, mousePosY);

                camera.Yaw -= deltaX * SENSITIVITY; //+=
                camera.Pitch -= deltaY * SENSITIVITY;
            }        
        }

        public static void ScrollCallback(Window window, double x, double y)
        {
            camera.Fov -= (float)y * 1.5f;
        }
    }
}
