using COREMath;
using CORERenderer.CRSFile;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.Main.Rendering;
using CORERenderer.Main;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace CORERenderer
{
    public class CORERenderContent : Overrides, EngineProperties
    {
        static public Shader lightShader;
        static public Shader gridShader;

        static public Camera camera;

        static Vector2 lastPos;

        static Framebuffer fbo;
        static public Cubemap cubemap;

        public static bool loaded = false;
        static bool loadable = true;
        static bool canChange = true;
        public static bool canDelete = false;

        static public uint vertexArrayObjectLightSource;
        static public uint vertexArrayObjectGrid;
        static public uint uboMatrices;
        static private double time;
        static private bool firstMove = true;
        static double mousePosXD;
        static double mousePosYD;
        static float mousePosX;
        static float mousePosY;

        public static CRS givenCRS;

        static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string directory = Path.GetDirectoryName(root);
        static int MathCIndex = directory.IndexOf("CORE-Renderer");

        static public string pathRenderer = directory.Substring(0, MathCIndex) + "CORE-Renderer\\CORE-Renderer";

        static public List<Light> lights;
        static public int currentObj = 0;
        static private float called = 0;

        static public Font test;


        public unsafe override void OnLoad()
        {
            Console.WriteLine("Initializing renderer");
            Console.WriteLine();
            MathC.Initialize(false);

            glEnable(GL_BLEND);
            
            glEnable(GL_DEPTH_TEST);
            glDepthFunc(GL_LESS);

            glEnable(GL_STENCIL_TEST);
            glStencilFunc(GL_NOTEQUAL, 1, 0xFF);
            glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);

            glEnable(GL_TEXTURE_2D);
            glEnable(GL_TEXTURE_CUBE_MAP);

            //glEnable(GL_FRAMEBUFFER_SRGB);

            glEnable(GL_DEBUG_OUTPUT);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glEnable(GL_CULL_FACE);
            glCullFace(GL_BACK);
            glFrontFace(GL_CCW);

            givenCRS = CRS.LoadCRS($"{pathRenderer}\\test.crs", "test");

            //initialises given shaders
            lightShader = new Shader($"{pathRenderer}\\shaders\\lightSource.vert", $"{pathRenderer}\\shaders\\lightSource.frag");
            gridShader = new Shader($"{pathRenderer}\\shaders\\grid.vert", $"{pathRenderer}\\shaders\\grid.frag");

            //creates space in the gpu memory for the global matrix uniforms
            uboMatrices = glGenBuffer();
            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            glBufferData(GL_UNIFORM_BUFFER, 3 * GL_MAT4_FLOAT_SIZE, NULL, GL_STATIC_DRAW);
            glBindBuffer(GL_UNIFORM_BUFFER, 0);
            glBindBufferRange(GL_UNIFORM_BUFFER, 0, uboMatrices, 0, 3 * GL_MAT4_FLOAT_SIZE);

            //generates the skybox
            string[] faces = new string[6] { $"{pathRenderer}\\textures\\right.jpg", $"{pathRenderer}\\textures\\left.jpg", $"{pathRenderer}\\textures\\top.jpg", $"{pathRenderer}\\textures\\bottom.jpg", $"{pathRenderer}\\textures\\front.jpg", $"{pathRenderer}\\textures\\back.jpg"};
            cubemap = GenerateSkybox(faces);

            fbo = GenerateFramebuffer();

            { //assignes values from vertices to the vertex buffer object for the light source
                vertexArrayObjectLightSource = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectLightSource);
            }

            { //allows the grid to render bufferless
                vertexArrayObjectGrid = glGenVertexArray();
                glBindVertexArray(vertexArrayObjectGrid);
            }


            test = new(32);


            lights = new();
            //lightSourcePos.Add(new(2, 2, 2));
            lights.Add(new() { position = new(0, 3, 0), color = new(1, 1, 1)});

            camera = new Camera(new(0, 1, 5), Width / Height);

            //assigns values the freed up gpu memory for global uniforms
            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            MatrixToUniformBuffer(camera.GetProjectionMatrix(), 0);
            glBindBuffer(GL_UNIFORM_BUFFER, 0);

            if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
                throw new GLFW.Exception("Framebuffer is not complete");
            glBindFramebuffer(GL_FRAMEBUFFER, 0);

            Console.Write($"\rInitialised in {Glfw.Time} seconds                         \n");
            Console.WriteLine("Beginning render loop");
        }

        public unsafe override void RenderEveryFrame()
        {
            //calculates the time between frames
            time += Glfw.Time - time;

            //assigns values the freed up gpu memory for global uniforms
            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            MatrixToUniformBuffer(camera.GetViewMatrix(), GL_MAT4_FLOAT_SIZE);
            MatrixToUniformBuffer(camera.GetTranslationlessViewMatrix(), GL_MAT4_FLOAT_SIZE * 2);

            //binds the correct framebuffer for accurate writing
            glBindFramebuffer(GL_FRAMEBUFFER, fbo);
            glEnable(GL_DEPTH_TEST);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

            //RenderAllObjects(givenCRS);
            RenderAllObjectsAsPBRDebugs(givenCRS);

            glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);

            glStencilMask(0x00);

            RenderLights(lights);
            
            RenderCubemap(cubemap);

            test.RenderText("This is sample text", 50f, 25.0f, 1.2f, new Vector2(1f, 0f));

            //RenderGrid();

            //for (int i = 0; i < givenCRS.allOBJs.Count; i++)
            //    givenCRS.allOBJs[i].RenderOutlines();

            fbo.RenderFramebuffer();
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
                givenCRS.allOBJs[currentObj].renderLines = true;
            if (Glfw.GetKey(window, Keys.L) == InputState.Release && loaded)
                givenCRS.allOBJs[currentObj].renderLines = false;

            if (Glfw.GetKey(window, Keys.K) == InputState.Press && loaded)
                givenCRS.allOBJs[currentObj].renderNormals = true;
            else if (Glfw.GetKey(window, Keys.K) == InputState.Release && loaded)
                givenCRS.allOBJs[currentObj].renderNormals = false;

            //!!temporary debug movement for obj files !!rewrite
            if (state2 == InputState.Press && state != InputState.Press)
            {   //calls the logic checks for highlighting the current object
                if (Glfw.GetKey(window, Keys.R) == InputState.Press && loaded)
                {
                    givenCRS.allOBJs[currentObj].translation = new(0, 0, 0);
                    givenCRS.allOBJs[currentObj].Scaling = 1;
                    givenCRS.allOBJs[currentObj].rotationX = 0;
                    givenCRS.allOBJs[currentObj].rotationY = 0;
                    givenCRS.allOBJs[currentObj].rotationZ = 0;
                }

                if (Glfw.GetKey(window, Keys.D) == InputState.Press && loaded && canChange)
                    HighlightLogic();
                //code below loads in new objects and checks if they can be loaded in
                if (Glfw.GetKey(window, Keys.E) == InputState.Press)
                    loadable = true;
                if (Glfw.GetKey(window, Keys.Q) == InputState.Press && loadable)
                {
                    if (loadable)
                    {
                        givenCRS.CSTAddObj($"{pathRenderer}\\Loaders\\testOBJ\\cube.obj");
                        loaded = true;
                        loadable = false;
                        if (givenCRS.allOBJs.Count > 0)
                            HighlightLogic();
                        givenCRS.nextUnusedID++; //may not be best solution but works atleast
                    } 
                }
                if (Glfw.GetKey(window, Keys.Backspace) == InputState.Press && loaded && canDelete)
                {
                    givenCRS.RemoveObject(givenCRS.allOBJs[currentObj].ID);
                    HighlightLogic();
                }

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
                        givenCRS.allOBJs[currentObj].translation += new Vector3(0, 1f * delta, 0);

                if (Glfw.GetKey(window, Keys.Down) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation -= new Vector3(0, 1f * delta, 0);

                if (Glfw.GetKey(window, Keys.Left) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation -= new Vector3(1f * delta, 0, 0);

                if (Glfw.GetKey(window, Keys.Right) == InputState.Press && loaded)
                    if (givenCRS.allOBJs[currentObj].highlighted)
                        givenCRS.allOBJs[currentObj].translation += new Vector3(1f * delta, 0, 0);
            }

            //basic movement
            if (state == InputState.Press)
            {
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

                if (Glfw.GetKey(window, Keys.W) == InputState.Press)
                    camera.position += camera.front * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.S) == InputState.Press)
                    camera.position -= camera.front * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.A) == InputState.Press)
                    camera.position -= camera.right * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.D) == InputState.Press)
                    camera.position += camera.right * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.Space) == InputState.Press)
                    camera.position += camera.up * (CAMERA_SPEED * delta);

                if (Glfw.GetKey(window, Keys.LeftShift) == InputState.Press)
                    camera.position -= camera.up * (CAMERA_SPEED * delta);
                //rotating the camera with mouse movement
                if (firstMove)
                {
                    lastPos = new(mousePosX, mousePosY);
                    firstMove = false;
                }
                else
                {
                    float deltaX = mousePosX - lastPos.x;
                    float deltaY = lastPos.y - mousePosY;

                    lastPos = new(mousePosX, mousePosY);

                    camera.Yaw += deltaX * SENSITIVITY;
                    camera.Pitch += deltaY * SENSITIVITY;
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
            if (givenCRS.allOBJs.Count == 0)
                return;
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
            camera.Fov -= (float)y * 1.5f;
        }
    }
}
