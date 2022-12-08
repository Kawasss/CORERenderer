using COREMath;
using static CORERenderer.OpenGL.GL;
using CORERenderer.Main;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW.Enums;
using System.IO;

namespace CORERenderer
{
    public class CORERenderContent : Rendering, EngineProperties
    {
        static private Shader lightShader;
        static private Shader gridShader;

        static public Camera camera;

        static Vector3 lastPos;

        public static bool loaded = false;
        static bool loadable = true;
        static bool canChange = true;
        public static bool canDelete = false;

        static private uint vertexArrayObjectLightSource;
        static private uint vertexArrayObjectGrid;
        static private double time;
        static private bool firstMove = true;
        static double mousePosXD;
        static double mousePosYD;
        static float mousePosX;
        static float mousePosY;

        public static CRS.CRS givenCRS;

        static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string directory = Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        public static float[] vertices;
        public static uint[] indices;

        static public Vector3 lightPos = new(0.6f, 1, 1f);
        static public int currentObj = 0;
        static private float called = 0;

        static public int placeholder = 0; //temporary for .crs related issues

        public unsafe override void OnLoad()
        {
            Console.WriteLine("Initializing renderer");
            Console.WriteLine();
            MathC.Initialize(false);

            glEnable(GL_BLEND);
            glEnable(GL_DEPTH_TEST);
            glEnable(GL_TEXTURE_2D);
            glEnable(GL_DEBUG_OUTPUT);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glEnable(GL_CULL_FACE);
            glCullFace(GL_BACK);
            glFrontFace(GL_CCW);
            givenCRS = CRS.CRS.LoadCRS($"{pathRenderer}\\test.crs", "test");

            //initialises given shaders
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
        }

        public unsafe override void RenderEveryFrame()
        {

            time += Glfw.Time - time * 2;

            //sets background color
            glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

            for (int i = 0; i < givenCRS.allOBJs.Count; i++)
            {
                givenCRS.allOBJs[i].Render(camera);
            }

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            //assigns all the values for placement of the light source
            lightShader.Use();
            lightShader.SetMatrix("view", camera.GetViewMatrix());
            lightShader.SetMatrix("projection", camera.GetProjectionMatrix());

            glBindVertexArray(vertexArrayObjectLightSource);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(5, 5, 5) * MathC.GetScalingMatrix(0.2f));

            glDrawArrays(GL_TRIANGLES, 0, 36);

            lightShader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(0, 10, 0) * MathC.GetScalingMatrix(0.2f));

            glDrawArrays(GL_TRIANGLES, 0, 36);
        }

        public override void AlwaysRender()
        {
            //assigns all the values for placement of the grid
            gridShader.Use();

            gridShader.SetMatrix("model", Matrix.IdentityMatrix * new Matrix(true, 100 * MathC.GetLengthOf(camera.position)));
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

            if (called <= 0.3f)
                called += delta;
            if (called > 0.3f)
            {
                canChange = true;
                canDelete = true;
            }

            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }

            const float CAMERA_SPEED = 1.05f;
            const float SENSITIVITY = 0.1f;

            InputState state = Glfw.GetMouseButton(window, MouseButton.Left);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Right);

            if (Glfw.GetKey(window, Keys.L) == InputState.Press && loaded)
            {
                glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);
                glDisable(GL_CULL_FACE);
            } 
            if (Glfw.GetKey(window, Keys.L) == InputState.Release && loaded)
            {
                glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
                glEnable(GL_CULL_FACE);
            }

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
                    {
                        givenCRS.CSTAddObj($"{pathRenderer}\\Loaders\\testOBJ\\c4520.obj");
                        loaded = true;
                        loadable = false;
                        if (givenCRS.allOBJs.Count > 0)
                            HighlightLogic();
                        givenCRS.nextUnusedID++; //may not be best solution but works atleast
                    } 
                }
                if (Glfw.GetKey(window, Keys.Backspace) == InputState.Press && loaded && canDelete)
                    givenCRS.RemoveObject(givenCRS.allOBJs[currentObj].ID);
                //code below is checking if the current is selected and moves, transforms or rotates the object
                if (Glfw.GetKey(window, Keys.Delete) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].rotationX += 15f * delta;
                if (Glfw.GetKey(window, Keys.End) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].rotationY += 15f * delta;
                if (Glfw.GetKey(window, Keys.PageDown) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].rotationZ += 15f * delta;

                if (Glfw.GetKey(window, Keys.Minus) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].Scaling -= 2f * delta;
                if (Glfw.GetKey(window, Keys.Equal) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].Scaling += 2f * delta;


                if (Glfw.GetKey(window, Keys.Up) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation += new Vector3(0, 1f * delta, 0); //obj

                if (Glfw.GetKey(window, Keys.Down) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation -= new Vector3(0, 1f * delta, 0); //obj

                if (Glfw.GetKey(window, Keys.Left) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation -= new Vector3(1f * delta, 0, 0); //obj

                if (Glfw.GetKey(window, Keys.Right) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation += new Vector3(1f * delta, 0, 0); //obj
            }

            //basic movement
            if (state == InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

                if (Glfw.GetKey(window, Keys.W) == InputState.Press)
                    camera.position -= camera.front * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.S) == InputState.Press)
                    camera.position += camera.front * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.A) == InputState.Press)
                    camera.position += camera.right * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.D) == InputState.Press)
                    camera.position -= camera.right * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.Space) == InputState.Press)
                    camera.position += camera.up * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.LeftShift) == InputState.Press)
                    camera.position -= camera.up * (CAMERA_SPEED * delta);
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

            if (Glfw.GetKey(window, Keys.S) == InputState.Press && Glfw.GetKey(window, Keys.LeftControl) == InputState.Press)
                givenCRS.SaveChanges();

            if (state != InputState.Press && state2 != InputState.Press)
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
        }

        //handles all of the logic for deciding which object to select, highlight and manipulate
        public static void HighlightLogic()
        {
            canChange = false;
            called = 0;
            if (currentObj == -1)
            {
                currentObj = 0;
                givenCRS.allOBJs[0].highlighted = true;
                return;
            }
            if (currentObj == 0)
            {
                if (givenCRS.allOBJs.Count > 1)
                {
                    givenCRS.allOBJs[currentObj + 1].highlighted = true;
                    givenCRS.allOBJs[currentObj].highlighted = false;
                    currentObj++;
                    return;
                }
                if (!givenCRS.allOBJs[0].highlighted)
                {
                    givenCRS.allOBJs[0].highlighted = true;
                    return;
                }
                givenCRS.allOBJs[0].highlighted = false;
                return;
            }
            if (currentObj >= givenCRS.allOBJs.Count - 1)
            {
                givenCRS.allOBJs[^1].highlighted = false;
                givenCRS.allOBJs[0].highlighted = true;
                currentObj = 0;
                return;
            }
            givenCRS.allOBJs[currentObj].highlighted = false;
            givenCRS.allOBJs[currentObj + 1].highlighted = true;
            currentObj++;
        }

        //zoom in or out
        public void ScrollCallback(Window window, double x, double y)
        {
            CORERenderContent.camera.Fov -= (float)y * 1.5f;
        }
    }
}
