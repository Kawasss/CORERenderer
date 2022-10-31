using System;
using COREMath;
using static CORERenderer.GL;
using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.shaders;
using CORERenderer.textures;
using CORERenderer.Bodies;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;


namespace CORERenderer
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

        static private Camera camera;

        static private Body object1;

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
        static string? directory = Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        public static float[] vertices;
        public static uint[] indices;
        private static float[] dummyVertices;
        private static uint[] dummyIndices;

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

        static readonly Vector3[] pointLightPositions =
        {
            new Vector3(0.7f, 0.2f, 2),
            new Vector3(2.3f, -3.3f, -4),
            new Vector3(-4, 2, 12),
            new Vector3(0, 0, -3)
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

            new OBJLoader().LoadOBJ($"{pathRenderer}\\loaders\\testOBJ\\test.obj", out vertices, out indices);
            //new OBJLoader().LoadOBJ($"{pathRenderer}\\loaders\\testOBJ\\human_low.obj", out vertices, out indices);
            //new OBJLoader().LoadOBJ($"{pathRenderer}\\loaders\\testOBJ\\bugatti.obj", out vertices, out indices);
            //new OBJLoader().LoadOBJ($"{pathRenderer}\\loaders\\testOBJ\\logo.obj", out vertices, out indices);
            new OBJLoader().LoadOBJ($"None", out dummyVertices, out dummyIndices);

            vertexBufferObject = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, vertexBufferObject);
            fixed (float* temp = &vertices[0])
            {
                IntPtr intptr = new(temp);
                glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
            }

            elementBufferObject = glGenBuffer();
            

            //initialises given shaders
            shader = new Shader($"{pathRenderer}\\shaders\\shader.vert", $"{pathRenderer}\\shaders\\lighting.frag"); //specify the type of light
            lightShader = new Shader($"{pathRenderer}\\shaders\\lightSource.vert", $"{pathRenderer}\\shaders\\lightSource.frag");
            gridShader = new Shader($"{pathRenderer}\\shaders\\grid.vert", $"{pathRenderer}\\shaders\\grid.frag");

            { //assignes values from vertices to the vertex buffer object
                vertexArrayObject = glGenVertexArray();
                glBindVertexArray(vertexArrayObject);

                int vertexLocation = shader.GetAttribLocation("aPos");
                glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0); //8 *
                glEnableVertexAttribArray((uint)vertexLocation);

                int vertexLocation2 = shader.GetAttribLocation("aTexCoords");
                glVertexAttribPointer((uint)vertexLocation2, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation2);

                int vertexLocation3 = shader.GetAttribLocation("aNormal");
                glVertexAttribPointer((uint)vertexLocation3, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float)));
                glEnableVertexAttribArray((uint)vertexLocation3);

                glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, elementBufferObject);
                fixed (uint* temp2 = &indices[0])
                {
                    IntPtr intptr = new(temp2);
                    glBufferData(GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(uint), intptr, GL_STATIC_DRAW);
                }
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
            diffuseTexture = Texture.ReadFromFile($"{pathRenderer}\\textures\\placeholder.png");
            specularTexture = Texture.ReadFromFile($"{pathRenderer}\\textures\\placeholderspecular.png");

            diffuseTexture.Use(GL_TEXTURE0);
            specularTexture.Use(GL_TEXTURE1);

            camera = new Camera(new(0, 1, 5), COREMain.Width / COREMain.Height);

            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop");

            //object1 = new($"{pathRenderer}\\loaders\\testOBJ\\logo.obj");
            

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

            shader.SetVector3("viewPos", camera.position);

            //textures
            shader.SetFloat("material.shininess", 2.0f);
            shader.SetInt("material.diffuse", GL_TEXTURE0);
            shader.SetInt("material.specular", GL_TEXTURE1);

            //directional light
            shader.SetVector3("dirLight.direction", -0.2f, -1.0f, -0.3f);
            shader.SetVector3("dirLight.ambient", 0.1f, 0.1f, 0.1f);
            shader.SetVector3("dirLight.diffuse", 0.5f, 0.5f, 0.5f);
            shader.SetVector3("dirLight.specular", 1.0f, 1.0f, 1.0f);

            shader.SetVector3("pointLights[0].position", 10, 5, 10);

            shader.SetVector3("pointLights[1].position", -10, 5, -10);

            for (int i = 0; i < 2; i++)
            {
                shader.SetVector3($"pointLights[{i}].position", 0, 10, 0);
                shader.SetFloat($"pointLights[{i}].constant", 1.0f);
                shader.SetFloat($"pointLights[{i}].linear", 0.022f);
                shader.SetFloat($"pointLights[{i}].quadratic", 0.0019f);
                shader.SetVector3($"pointLights[{i}].ambient", 0.2f, 0.2f, 0.2f);
                shader.SetVector3($"pointLights[{i}].diffuse", 0.5f, 0.5f, 0.5f);
                shader.SetVector3($"pointLights[{i}].specular", 1.0f, 1.0f, 1.0f);
            }

            //spotLight
            shader.SetVector3("spotLight.position", camera.position);
            shader.SetVector3("spotLight.direction", camera.front);
            shader.SetVector3("spotLight.ambient", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.diffuse", 0.0f, 0.0f, 0.0f);
            shader.SetVector3("spotLight.specular", 0.0f, 0.0f, 0.0f);
            shader.SetFloat("spotLight.constant", 1.0f);
            shader.SetFloat("spotLight.linear", 0.09f);
            shader.SetFloat("spotLight.quadratic", 0.032f);
            shader.SetFloat("spotLight.cutOff", MathC.Cos(MathC.DegToRad(12.5f)));
            shader.SetFloat("spotLight.outerCutOff", MathC.Cos(MathC.DegToRad(15.0f)));

            shader.SetMatrix("view", camera.GetArcBallViewMatrix());
            shader.SetMatrix("projection", camera.GetProjectionMatrix());

            glBindVertexArray(vertexArrayObject);

            Matrix model = Matrix.IdentityMatrix.MultiplyWith(MathC.GetScalingMatrix(0.1f));

            shader.SetMatrix("model", model);
            //glDrawArrays(GL_TRIANGLES, 0, vertices.Length / 8); 
            glDrawElements(GL_TRIANGLES, indices.Length, GL_UNSIGNED_INT, (void*)0);

            //assigns all the values for placement of the light source
            lightShader.Use();
            lightShader.SetMatrix("view", camera.GetViewMatrix());
            lightShader.SetMatrix("projection", camera.GetProjectionMatrix());

            glBindVertexArray(vertexArrayObjectLightSource);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(MathC.GetTranslationMatrix(5, 5, 5)).MultiplyWith(MathC.GetScalingMatrix(0.2f)));

            glDrawArrays(GL_TRIANGLES, 0, 36);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(MathC.GetTranslationMatrix(0, 10, 0)).MultiplyWith(MathC.GetScalingMatrix(0.2f)));

            glDrawArrays(GL_TRIANGLES, 0, 36);

            //assigns all the values for placement of the grid
            gridShader.Use();

            gridShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(new Matrix(true, 100 * MathC.GetLengthOf(camera.position))));
            gridShader.SetMatrix("view", camera.GetArcBallViewMatrix());
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

            const float CAMERA_SPEED = 1.05f;
            const float SENSITIVITY = 0.1f;

            InputState state = Glfw.GetMouseButton(window, MouseButton.Left);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Middle);

            //basic movement
            if (state == InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

                if (Glfw.GetKey(window, Keys.W) == InputState.Press)
                {
                    Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * 0.001f);
                    camera.position = camera.position.Subtract(v1);
                }
                if (Glfw.GetKey(window, Keys.S) == InputState.Press)
                {
                    Vector3 v1 = camera.front.Scalar(CAMERA_SPEED * 0.001f);
                    camera.position = camera.position.Add(v1);
                }
                if (Glfw.GetKey(window, Keys.A) == InputState.Press)
                {
                    Vector3 v1 = camera.right.Scalar(CAMERA_SPEED * 0.001f);
                    camera.position = camera.position.Add(v1);
                }
                if (Glfw.GetKey(window, Keys.D) == InputState.Press)
                {
                    Vector3 v1 = camera.right.Scalar(CAMERA_SPEED * 0.001f);
                    camera.position = camera.position.Subtract(v1);
                }
                if (Glfw.GetKey(window, Keys.Space) == InputState.Press)
                {
                    Vector3 v1 = camera.up.Scalar(CAMERA_SPEED * 0.001f);
                    camera.position = camera.position.Add(v1);
                }
                if (Glfw.GetKey(window, Keys.LeftShift) == InputState.Press)
                {
                    Vector3 v1 = camera.up.Scalar(CAMERA_SPEED * 0.001f);
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
