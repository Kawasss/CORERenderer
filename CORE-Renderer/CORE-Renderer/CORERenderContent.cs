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
using System.Runtime.CompilerServices;

namespace CORERenderer
{
    

    public class CORERenderContent : Rendering, EngineProperties
    {
        static private Shader lightShader;
        static private Shader gridShader;

        static public Camera camera;

        static private List<Obj> objs = new();

        static Vector3 lastPos;

        static bool loaded = false;
        static bool loadable = true;
        static bool singleHighlighted = true;
        static bool canChange = true;

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
        static private int currentObj = 0;
        static private int called = 0;

        public unsafe override void OnLoad()
        {
            Console.WriteLine("Initializing renderer");
            Console.WriteLine();
            MathC.Initialize(false);

            glEnable(GL_DEPTH_TEST);
            glEnable(GL_BLEND);
            glEnable(GL_TEXTURE_2D);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

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

            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop");

            //resets all of the printed lines before this
            /*Console.CursorTop = 0;
            for (int i = 0; i <= 50; i++)
                Console.WriteLine("                                                                                                 "); //space needed to replace all characters
            Console.CursorTop = 0;*/

            EngineProperties.showFrameTime = true;
            EngineProperties.showFPS = true;
            EngineProperties.maxFPS = 60;
        }

        public unsafe override void RenderEveryFrame()
        {
            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            for (int i = 0; i < objs.Count; i++)
            {
                objs[i].Render(camera);
            }
                    

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

            if (called <= 500)
                called++;
            if (called > 500)
                canChange = true;

            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }

            const float CAMERA_SPEED = 1.05f;
            const float SENSITIVITY = 0.1f;

            InputState state = Glfw.GetMouseButton(window, MouseButton.Left);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Right);

            //!!temporary debug movement for obj files !!rewrite
            if (state2 == InputState.Press && state != InputState.Press) //
            {   //calls the logic checks for highlighting the current object
                if (Glfw.GetKey(window, Keys.D) == InputState.Press && loaded && canChange)
                    HighlightLogic();
                //code below loads in new objects and checks if they can be loaded in
                if (Glfw.GetKey(window, Keys.E) == InputState.Press)
                    loadable = true;
                if (Glfw.GetKey(window, Keys.Q) == InputState.Press && loadable)// && !loaded)
                {
                    if (loadable)
                        objs.Add(new($"{pathRenderer}\\loaders\\testOBJ\\c4520.obj"));
                    if (objs.Count > 1)
                        objs[^2].highlighted = false;
                    objs[^1].highlighted = true;
                    currentObj = objs.Count - 1;
                    loaded = true;
                    loadable = false;
                }
                //code below is checking if the current is selected and moves, transforms or rotates the object
                if (Glfw.GetKey(window, Keys.Delete) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].rotationX += 0.01f; //obj
                if (Glfw.GetKey(window, Keys.End) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].rotationY += 0.01f; //obj
                if (Glfw.GetKey(window, Keys.PageDown) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].rotationZ += 0.01f; //obj


                if (Glfw.GetKey(window, Keys.Minus) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].Scaling -= 0.00015f; //obj
                if (Glfw.GetKey(window, Keys.Equal) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].Scaling += 0.00015f; //obj


                if (Glfw.GetKey(window, Keys.Up) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].translation.y += 0.0002f; //obj

                if (Glfw.GetKey(window, Keys.Down) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].translation.y -= 0.0002f; //obj

                if (Glfw.GetKey(window, Keys.Left) == InputState.Press && loaded)
                    if (objs[currentObj].highlighted)
                        objs[currentObj].translation.x -= 0.0002f; //obj

                if (Glfw.GetKey(window, Keys.Right) == InputState.Press && loaded)
                {
                    Console.WriteLine(currentObj);
                    objs[currentObj].translation.x += 0.0002f; //obj
                }
            }

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

            if (state != InputState.Press && state2 != InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
            }
        }

        //handles all of the logic for deciding which object to select, highlight and manipulate
        private void HighlightLogic()
        {
            canChange = false;
            called = 0;
            currentObj++;
            if (currentObj >= objs.Count)
            {
                if (objs.Count == 1 && currentObj >= objs.Count && singleHighlighted)
                {
                    objs[0].highlighted = false;
                    singleHighlighted = false;
                }
                else if (objs.Count == 1)
                {
                    objs[0].highlighted = true;
                    singleHighlighted = true;
                }

                if (objs.Count > 1)
                {
                    objs[0].highlighted = true;
                    objs[^1].highlighted = false;
                }
                currentObj = 0;
            }

            if (currentObj > 0 && currentObj < objs.Count - 1)
            {
                objs[currentObj].highlighted = true;
                objs[currentObj - 1].highlighted = false;
                objs[currentObj + 1].highlighted = false;
            }
            else if (currentObj == 0 && objs.Count > 1)
            {
                objs[currentObj].highlighted = true;
                objs[^1].highlighted = false;
                objs[1].highlighted = false;
            }
            else if (currentObj == objs.Count - 1 && objs.Count > 1)
            {
                objs[currentObj].highlighted = true;
                objs[currentObj - 1].highlighted = false;
                objs[0].highlighted = false;
            }
        }

        //zoom in or out !!Unused due to new architecture
        public void ScrollCallback(Window window, double x, double y)
        {
            CORERenderContent.camera.Fov -= (float)y * 1.5f;
        }
    }
}
