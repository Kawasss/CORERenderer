using System;
using GLFW;
using COREMath;
using static OpenGL.GL;
using System.Security.Cryptography.X509Certificates;
using CORE_Renderer;
using OpenGL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;

namespace openGLToturial
{
    class CORERenderContent : Overrides
    {
        static private Shader shader;
        static private Shader lightShader;
        static private Shader gridShader;

        static private Texture diffuseTexture;
        static private Texture specularTexture;

        static private Matrix view;
        static private Matrix projection;
        static private Matrix lightModel;

        static private Camera camera;

        static Vector3 lastPos;

        static private uint vertexBufferObject;
        static private uint elementBufferObject;
        static private uint vertexArrayObject;
        static private uint vertexArrayObjectLightSource;
        static private uint vertexArrayObjectGrid;
        static private double time;
        static private bool firstMove = true;
        static double mousePosXD;
        static double mousePosYD;
        static float mousePosX;
        static float mousePosY;

        static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string? directory = System.IO.Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        private readonly float[] vertices = {
            //positions           //normals            //texture coords
            -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  0.0f,
             0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  1.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  1.0f,  1.0f,
            -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  1.0f,
            -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,  0.0f,  0.0f,

            -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  0.0f,
             0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  1.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  1.0f,  1.0f,
            -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  1.0f,
            -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,  0.0f,  0.0f,

            -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  1.0f,
            -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
            -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
            -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  0.0f,  0.0f,
            -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,  1.0f,  0.0f,

             0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  0.0f,
             0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  1.0f,
             0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
             0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  1.0f,
             0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  0.0f,  0.0f,
             0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,  1.0f,  0.0f,

            -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  1.0f,
             0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  1.0f,
             0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  0.0f,
             0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  1.0f,  0.0f,
            -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  0.0f,
            -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,  0.0f,  1.0f,

            -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  1.0f,
             0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  1.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  0.0f,
             0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  1.0f,  0.0f,
            -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  0.0f,
            -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,  0.0f,  1.0f
        };

        static readonly Vector3[] cubePos =
        {
            new Vector3(0, 0, 0),
            new Vector3(2, 5, -15),
            new Vector3(-1.5f, -2.2f, -2.5f),
            new Vector3(-3.8f, -2.0f, -12.3f),
            new Vector3(2.4f, -0.4f, -3.5f),
            new Vector3(-1.7f,  3.0f, -7.5f),
            new Vector3(1.3f, -2.0f, -2.5f),
            new Vector3(1.5f,  2.0f, -2.5f),
            new Vector3(1.5f,  0.2f, -1.5f),
            new Vector3(-1.3f,  1.0f, -1.5f)
        };

        static public Vector3 lightPos = new(0.6f, 1, 1f);

        public unsafe override void OnLoad()
        {
            Console.WriteLine("Initializing renderer");
            Console.WriteLine();
            MathC.Initialize(false);

            glEnable(GL_DEPTH_TEST);
            glEnable(GL_BLEND);
            glEnable(GL_TEXTURE_2D);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

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
            gridShader = new Shader($"{pathRenderer}\\shaders\\grid.vert", $"{pathRenderer}\\shaders\\grid.frag");
            
            { //assignes values from vertices to the vertex buffer object
                vertexArrayObject = glGenVertexArray();
                glBindVertexArray(vertexArrayObject);

                int vertexLocation = shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);

                int vertexLocation2 = shader.GetAttribLocation("aNormal");
                glVertexAttribPointer((uint)vertexLocation2, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation2);

                int vertexLocation3 = shader.GetAttribLocation("aTexCoords");
                glVertexAttribPointer((uint)vertexLocation3, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(6 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation3);
            }

            { //assignes values from vertices to the vertex buffer object for the light source
                vertexArrayObjectLightSource = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectLightSource);

                int vertexLocation = lightShader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0);
                glEnableVertexAttribArray((uint)vertexLocation);
            }
            
            { //allows the grid to render bufferless
                vertexArrayObjectGrid = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectGrid);
            }

            //loads in and uses textures
            diffuseTexture = Texture.ReadFromFile($"{pathRenderer}\\textures\\container2.png");
            specularTexture = Texture.ReadFromFile($"{pathRenderer}\\textures\\container2_specular.png");

            diffuseTexture.Use(GL_TEXTURE0);
            specularTexture.Use(GL_TEXTURE1);

            camera = new Camera(new(0, 1, -3), COREMain.WIDTH / COREMain.HEIGHT);

            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop"                          );

            Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); Console.WriteLine(); //more space for debug
        }

        public unsafe override void RenderEveryFrame()
        {
            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            //assigns all the values to the object shaders for proper lighting and placement
            shader.Use();

            shader.SetInt("material.diffuse", 0);
            shader.SetInt("material.specular", 1);

            shader.SetVector3("light.position", lightPos);
            shader.SetVector3("viewPos", camera.position);
            
            shader.SetVector3("light.ambient", 0.2f, 0.2f, 0.2f);
            shader.SetVector3("light.diffuse", 0.7f, 0.7f, 0.7f);
            shader.SetVector3("light.specular", 1, 1, 1);

            shader.SetVector3("material.specular", 0.5f, 0.5f, 0.5f);

            shader.SetFloat("material.shininess", 32);
            shader.SetFloat("light.constant", 1);
            shader.SetFloat("light.linear", 0.022f);
            shader.SetFloat("light.quadratic", 0.0019f);

            shader.SetMatrix("view", camera.GetViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            //draws 10 objects
            glBindVertexArray(vertexArrayObject);
            for (int i = 0; i < 10; i++)
            {
                Matrix model = MathC.GetTranslationMatrix(cubePos[i]);
                float angle = 20 * i;
                model = model.MultiplyWith(MathC.GetRotationXMatrix(angle)).MultiplyWith(MathC.GetRotationZMatrix(angle));

                shader.SetMatrix("model", model);

                glDrawArrays(GL_TRIANGLES, 0, 36);
            }

            //assigns all the values for placement of the light source
            lightShader.Use();

            lightPos.x = (float)(1 + MathC.Cos(Glfw.Time) * 2);
            lightPos.y = (float)MathC.Sin(Glfw.Time / 2);

            lightModel = Matrix.IdentityMatrix.MultiplyWith(new Matrix(false, lightPos.x, lightPos.y, lightPos.z));
            lightModel = lightModel.MultiplyWith(new Matrix(true, 0.2f));

            lightShader.SetMatrix("model", lightModel);
            lightShader.SetMatrix("view", camera.GetViewMatrix());
            lightShader.SetMatrix("projection", camera.GetProjectionMatrix());

            glBindVertexArray(vertexArrayObjectLightSource);
            glDrawArrays(GL_TRIANGLES, 0, 36);
            
            //assigns all the values for placement of the grid
            gridShader.Use();

            gridShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(new Matrix(true, 100 * MathC.GetLengthOf(camera.position))));
            gridShader.SetMatrix("view", camera.GetViewMatrix());
            gridShader.SetMatrix("projection", camera.GetProjectionMatrix());

            gridShader.SetVector3("playerPos", camera.position);

            glBindVertexArray(vertexArrayObjectGrid);
            glDrawArrays(GL_TRIANGLES, 0, 6);

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

            camera.Debug();

            const float CAMERA_SPEED = 1.1f;
            const float SENSITIVITY = 0.1f;

            InputState state = Glfw.GetMouseButton(window, MouseButton.Left);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Middle);

            //basic movement
            if (state == InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

                if (Glfw.GetKey(window, Keys.W) == InputState.Press)
                {
                    Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * delta);
                    camera.position = camera.position.Subtract(v1);
                }
                if (Glfw.GetKey(window, Keys.S) == InputState.Press)
                {
                    Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * delta);
                    camera.position = camera.position.Add(v1);
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
                //rotating the camera with mouse movement
                if (firstMove)
                {
                    lastPos = new(mousePosX, 0, mousePosY);
                    firstMove = false;
                }
                else
                {
                    float deltaX = mousePosX - lastPos.x;
                    float deltaY = lastPos.z - mousePosY;

                    lastPos = new(mousePosX, 0, mousePosY);

                    camera.Yaw += deltaX * SENSITIVITY;
                    camera.Pitch -= deltaY * SENSITIVITY;
                }
            }

            if (state2 == InputState.Press) //doesnt work
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

                if (firstMove)
                {
                    lastPos = new(mousePosX, 0, mousePosY);
                    firstMove = false;
                }
                else
                {
                    float deltaX = mousePosX - lastPos.x;
                    float deltaY = lastPos.z - mousePosY;

                    lastPos = new(mousePosX, 0, mousePosY);

                    camera.Yaw += deltaX * SENSITIVITY;
                    camera.Pitch -= deltaY * SENSITIVITY;
                }
            }

            if (state != InputState.Press && state2 != InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
            }
        }
        //zoom in or out
        public static void ScrollCallback(Window window, double x, double y)
        {
            camera.Fov -= (float)y * 1.5f;
        }
    }
}
