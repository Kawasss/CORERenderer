using System;
using COREMath;
using static CORERenderer.GL;
using CORERenderer.Main;
using CORERenderer.Loaders;
using CORERenderer.shaders;
using CORERenderer.textures;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;


namespace CORERenderer
{
    

    public class CORERenderContent : Rendering, ICommonData
    {
        static private Shader shader;
        static private Shader lightShader;
        static private Shader gridShader;

        static private Texture diffuseTexture;
        static private Texture specularTexture;

        static private Matrix view;
        static private Matrix projection;

        static public Camera camera;

        static private Obj obj;

        static Vector3 lastPos;

        static private uint vertexArrayObjectLightSource;
        static private uint vertexArrayObjectGrid;
        static private double time;
        static private bool firstMove = true;
        static double mousePosXD;
        static double mousePosYD;
        static float mousePosX;
        static float mousePosY;

        static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string directory = Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        public static float[] vertices;
        public static uint[] indices;

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

            obj = new($"{pathRenderer}\\loaders\\testOBJ\\human_low.obj");
            
            //initialises given shaders
            //shader = new Shader($"{pathRenderer}\\shaders\\shader.vert", $"{pathRenderer}\\shaders\\lighting.frag"); unneeded if obj.cs is done
            lightShader = new Shader($"{pathRenderer}\\shaders\\lightSource.vert", $"{pathRenderer}\\shaders\\lightSource.frag");
            gridShader = new Shader($"{pathRenderer}\\shaders\\grid.vert", $"{pathRenderer}\\shaders\\grid.frag");

            { //assignes values from vertices to the vertex buffer object for the light source
                vertexArrayObjectLightSource = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectLightSource);
            }

            { //allows the grid to render bufferless
                vertexArrayObjectGrid = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectGrid);
            }

            camera = new Camera(new(0, 1, 5), Width / Height);

            //Glfw.SetScrollCallback(window, ScrollCallback);

            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop");

            //resets all of the printed lines before this
            /*Console.CursorTop = 0;
            for (int i = 0; i <= 50; i++)
                Console.WriteLine("                                                                                                 "); //space needed to replace all characters
            Console.CursorTop = 0;*/
        }

        public unsafe override void RenderEveryFrame()
        {
            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            obj.Render(camera);

            //assigns all the values for placement of the light source
            lightShader.Use();
            lightShader.SetMatrix("view", camera.GetViewMatrix());
            lightShader.SetMatrix("projection", camera.GetProjectionMatrix());

            glBindVertexArray(vertexArrayObjectLightSource);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(MathC.GetTranslationMatrix(5, 5, 5)).MultiplyWith(MathC.GetScalingMatrix(0.2f)));

            glDrawArrays(GL_TRIANGLES, 0, 36);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(MathC.GetTranslationMatrix(0, 10, 0)).MultiplyWith(MathC.GetScalingMatrix(0.2f)));

            glDrawArrays(GL_TRIANGLES, 0, 36);
        }

        public override void AlwaysRender()
        {
            //assigns all the values for placement of the grid
            gridShader.Use();

            gridShader.SetMatrix("model", Matrix.IdentityMatrix.MultiplyWith(new Matrix(true, 100 * MathC.GetLengthOf(camera.position))));
            gridShader.SetMatrix("view", camera.GetArcBallViewMatrix());
            gridShader.SetMatrix("projection", camera.GetProjectionMatrix());

            gridShader.SetVector3("playerPos", camera.position);

            glBindVertexArray(vertexArrayObjectGrid);
            glDrawArrays(GL_TRIANGLES, 0, 6);
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
        public void ScrollCallback(Window window, double x, double y)
        {
            camera.Fov -= (float)y * 1.5f;
        }
    }
}
