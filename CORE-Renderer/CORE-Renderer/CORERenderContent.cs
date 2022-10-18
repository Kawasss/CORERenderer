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
        static private Shader lightShader;

        static private Texture texture;
        static private Texture texture2;
       
        static private Matrix view;
        static private Matrix projection;
        static private Matrix model;

        static private Camera camera;

        static private uint vertexBufferObject;
        static private uint vertexArrayObject;
        static private uint vertexArrayObjectLightSource;
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

        private readonly float[] vertices = {
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,

        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,

        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,

         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,

        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,

        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f
    };


        static readonly uint[] indices = { 0, 1, 3,
                                  1, 2, 3
                                };

        static public Vector3 lightPos = new(0.7f, 1, 1.5f); //new(1.2f, 1, 2);

        public unsafe override void OnLoad()
        {
            Console.WriteLine("Initializing renderer");
            Console.WriteLine();
            MathC.Initialize(false);

            glEnable(GL_DEPTH_TEST);

            vertexBufferObject = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, vertexBufferObject);
            fixed (float* temp = &vertices[0])
            {
                IntPtr intptr = new(temp);
                glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
            }

            //initialises given shaders
            shader = new Shader($"{pathRenderer}\\shaders\\shader.vert", $"{pathRenderer}\\shaders\\shader.frag");
            lightShader = new Shader($"{pathRenderer}\\shaders\\shader.vert", $"{pathRenderer}\\shaders\\lampShader.frag");

            {
                vertexArrayObject = glGenVertexArray();
                glBindVertexArray(vertexArrayObject);

                int vertexLocation = shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 6 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                int vertexLocation2 = shader.GetAttribLocation("aNormal");
                glVertexAttribPointer((uint)vertexLocation2, 3, GL_FLOAT, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation2);
            }

            {
                vertexArrayObjectLightSource = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectLightSource);

                int vertexLocation = lightShader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 6 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);
            }

            Vector3 newVector = new(0, 0, 3f);
            camera = new Camera(newVector, COREMain.WIDTH / COREMain.HEIGHT);

            Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);
            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop"                          );

            Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); //more space for debug
        }

        public unsafe override void RenderEveryFrame()
        {
            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            glBindVertexArray(vertexArrayObject);

            shader.Use();

            shader.SetMatrix("model", Matrix.IdentityMatrix);
            shader.SetMatrix("view", camera.GetViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            shader.SetVector3("objectColor", 1, 0.5f, 0.31f);
            shader.SetVector3("lightColor", 1, 1, 1);
            shader.SetVector3("lightPos", lightPos);

            glDrawArrays(GL_TRIANGLES, 0, 36);

            glBindVertexArray(vertexArrayObjectLightSource);

            lightShader.Use();

            Matrix lightMatrix = new(0.2f, 0.2f, 0.2f, lightPos.x, lightPos.y, lightPos.z);
            lightMatrix = lightMatrix.MultiplyWith(lightMatrix);

            lightShader.SetMatrix("model", lightMatrix);
            lightShader.SetMatrix("view", camera.GetViewMatrix());
            lightShader.SetMatrix("projection", camera.GetProjectionMatrix());

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

            InputState state = Glfw.GetMouseButton(window, MouseButton.Left);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Middle);

            if (state == InputState.Press)
            {
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
                }
                else
                {
                    float deltaX = mousePosX - lastPos.x;
                    float deltaY = mousePosY - lastPos.z;

                    lastPos = new(mousePosX, 0, mousePosY);

                    camera.Yaw += deltaX * SENSITIVITY;
                    camera.Pitch -= deltaY * SENSITIVITY;
                }
            }
        }

        public static void ScrollCallback(Window window, double x, double y)
        {
            camera.Fov -= (float)y * 1.5f;
        }
    }
}
