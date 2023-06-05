#region using statements
using Console = CORERenderer.GUI.Console;
using COREMath;
using CORERenderer.Fonts;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.GUI;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using System.Diagnostics;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.Main.Config;
#endregion

namespace CORERenderer.Main
{
    public class COREMain
    {
        [NotNull]
        #region Declarations
        //ints
        public  static int Width, Height, monitorWidth, monitorHeight, renderWidth, renderHeight, viewportX, viewportY;

        public  static int drawCallsPerSecond = 0, drawCallsPerFrame = 0;
        public  static int fps = 0, frameCount = 0, totalFrameCount = 0;
        private static int selectedScene = 0;
        public  static int selectedID = 0x00FFFF, nextAvaibleID = 9; //white (background) //first 9 IDs are used by Arrows
        public  static int refreshRate = 0;
        public  static int errorsCaught = 0;
        public  const  int NoIDSelected = 0x00FFFF;

        public  static int NewAvaibleID { get { nextAvaibleID++; return nextAvaibleID - 1; } } //automatically generates a new ID whenever its asked for one
        public  static int GetCurrentObjFromScene { get => scenes[selectedScene].currentObj; }
        public  static int FrameCount { get { return totalFrameCount; } }
        public  static int SelectedScene { get => selectedScene; set { scenes[value].OnSceneEnter(); selectedScene = value; } }

        //uints
        public static uint vertexArrayObjectLightSource;

        //doubles
        private static double previousTime = 0;
        public  static double CPUUsage = 0;
        public  static double scrollWheelMovedAmount = 0;
        public  static double timeSinceLastFrame = 0;

        //floats
        public  static float mousePosX, mousePosY;

        private static float currentFrameTime = 0;
        public  static float FrameTime { get { return (float)timeSinceLastFrame; } }

        //strings
        public  static string LoadFilePath = null;
        public  static string gpu = "Not Recognized";
        private static string pathRenderer;
        public  const  string VERSION = "v0.6";

        public  static string GPU { get => gpu; }
        public  static string BaseDirectory { get => pathRenderer; }

        //bools
        public  static bool renderGrid = true, renderBackground = true, renderGUI = true, renderIDFramebuffer = false, renderToIDFramebuffer = true; //rendering related options
        public  static bool secondPassed = true;
        public  static bool useChromAber = false, useVignette = false; //post processing related options
        public  static bool fullscreen = false;

        public  static bool destroyedWindow = false;
        public  static bool addCube = false, addCylinder = false; //add object related options
        public  static bool renderEntireDir = false;
        public  static bool allowAlphaOverride = true;
        public  static bool isCompiledForWindows = false;
        public  static bool newFrame = false;

        public  static bool keyIsPressed = false, mouseIsPressed = false, scrollWheelMoved = false;
        public  static bool clearedGUI = false;

        public  static bool subMenuOpenLastFrame = false, submenuOpen = false;

        public  static bool loadInfoOnstartup = true;
        private static bool appIsHealthy = true;
        public  static bool AppIsHealthy { get => appIsHealthy; }

        //enums
        public  static ModelType   LoadFile = ModelType.None;
        public  static Keys        pressedKey;
        public  static MouseButton pressedButton;

        //classes
        public  static List<Scene>  scenes = new();
        public  static SplashScreen splashScreen;
        public  static Font         debugText;
        public  static Console      console = null;
        public  static Arrows       arrows;
        private static Submenu      menu;
        private static Thread       mainThread;

        public  static Model        CurrentModel { get { if (CurrentScene.currentObj == -1 && CurrentScene.models.Count > 0) CurrentScene.currentObj = CurrentScene.models.Count - 1; return CurrentScene.models[GetCurrentObjFromScene]; } }
        public  static Scene        CurrentScene { get => scenes[SelectedScene]; }
        public  static Thread       MainThread { get { return mainThread; } }

        //structs
        private static TimeSpan    previousCPU;
        public  static Window      window;
        public  static Framebuffer IDFramebuffer;
        public  static Framebuffer renderFramebuffer;
        #endregion

        public static int Main(string[] args)
        {
#if OS_WINDOWS
            isCompiledForWindows = true;
#endif
            try
            {
                #region set up
                mainThread = Thread.CurrentThread;

                pathRenderer = GetBaseDirectory();
                //-------------------------------------------------------------------------------------------
                Glfw.Init();

                splashScreen = new();
                refreshRate = splashScreen.refreshRate;

                //CheckForCompatibility();

                CalculateDimensions();

                SetModelType(args);

                StartOtherProcesses();

                vertexArrayObjectLightSource = GenerateBufferlessVAO();

                gpu = GetGPU();

                debugText = new((uint)(monitorHeight * 0.01333), $"{BaseDirectory}\\Fonts\\baseFont.ttf");

                #region Initializing the GUI, and setting the appriopriate values
                TitleBar tb = new();

                Div modelList = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.004f), (int)(monitorHeight * 0.004f));
                Div submodelList = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.004f), (int)(monitorHeight * 0.004f));
                Div modelInformation = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.879f), (int)(monitorHeight * 0.004f));

                int[] cDimensions = Console.ConsoleDimensionsWithoutDebugmenu;
                console = new(cDimensions[0], cDimensions[1], cDimensions[2], cDimensions[3]);//new((int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.242f - 25),monitorWidth - viewportX - (int)(monitorWidth * 0.496 - monitorWidth * 0.125f),(int)(monitorHeight * 0.004f));
                Console.GenerateConsoleErrorLog(BaseDirectory);

                TabManager tab = new(new string[] { "Models", "Submodels" });
                TabManager sceneManager = new("Scene");

                Button button = new("Scene", 5, monitorHeight - 25);
                Button saveAsImage = new("Save as PNG", 10 + 5 * (int)debugText.GetStringWidth("Scene", 1), monitorHeight - 25);
                menu = new(new string[] { "Render Grid", "Render Background", "Render Wireframe", "Render Normals", "Render GUI", "Render IDFramebuffer", "Render to ID framebuffer", "Render orthographic", "  ", "Cull Faces", " ", "Add Object:", "  Cube", "  Cylinder", "   ", "Load entire directory", "Allow alpha override", "Use chrom. aber.", "Use vignette", "fullscreen" });

                tab.AttachTo(modelList);
                tab.AttachTo(submodelList);
                menu.AttachTo(ref button);
                button.OnClick(menu.Render);
                menu.SetBool("Render Grid", renderGrid);
                menu.SetBool("Render Background", renderBackground);
                menu.SetBool("Render GUI", renderGUI);
                menu.SetBool("Render IDFramebuffer", renderIDFramebuffer);
                menu.SetBool("Render to ID framebuffer", renderToIDFramebuffer);
                menu.SetBool("Render orthographic", renderOrthographic);
                menu.SetBool("Cull Faces", cullFaces);
                menu.SetBool("  Cube", addCube);
                menu.SetBool("  Cylinder", addCylinder);
                menu.SetBool("Load entire directory", renderEntireDir);
                menu.SetBool("Allow alpha override", allowAlphaOverride);
                menu.SetBool("Use chrom. aber.", useChromAber);
                menu.SetBool("Use vignette", useVignette);
                menu.SetBool("fullscreen", fullscreen);
                #endregion

                GenerateNeededFramebuffers(out Framebuffer gui, out Framebuffer wrapperFBO);

                SetCPUValues();

                SetDefaultScene(args);
                Scene.EnableGLOptions();

                Callbacks.SetCallbacks();
                
                arrows = new();

                SetUniformBuffers();

                glViewport(0, 0, monitorWidth, monitorHeight);
                //-------------------------------------------------------------------------------------------
                if (!renderFramebuffer.isComplete)
                    throw new GLFW.Exception("Framebuffer is not complete");

                Console.WriteLine($"Initialised in {Glfw.Time} seconds");
                Console.WriteLine("Beginning render loop");

                double timeSinceLastFrame2 = Glfw.Time;
                #endregion

                #region First time rendering
                Rendering.UpdateUniformBuffers();
                UpdateRenderStatistics();
                UpdateCursorLocation();

                gui.Bind();

                glClearColor(0.085f, 0.085f, 0.085f, 1);
                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);
                clearedGUI = true;
                tb.CheckForUpdate(mousePosX, mousePosY);
                tb.Render();

                tab.Render();

                modelInformation.Render();

                button.RenderConditionless();
                saveAsImage.RenderStatic();

                console.RenderEvenIfNotChanged();

                sceneManager.Render();
                #endregion

                #region render loop
                while (!Glfw.WindowShouldClose(window)) //maybe better to let the render loop run in Rendering.cs
                {
                    try
                    {
                        timeSinceLastFrame2 = FrameSetup(timeSinceLastFrame2);

                        glViewport(0, 0, monitorWidth, monitorHeight);

                        RenderGUI(gui, tb, button, saveAsImage, tab, modelInformation, modelList, sceneManager);

                        RenderProtectedScene(args);

                        RenderMonitorFramebuffer(gui);

                        UpdateFrameDependentValues();
                    }
                    catch (System.Exception err)
                    {
                        RuntimeErrorHandling(err);
                    }
                    Glfw.SwapBuffers(window);
                    Glfw.PollEvents();

                    DestroySplashscreen();
                }
                #endregion

                #region app termination
                DeleteAllBuffers();
                Glfw.Terminate();
                #endregion
            }
            catch (System.Exception err)
            {
                ReportFatalError(err);
            }
            return 0;
        }

        private static void CheckForCompatibility()
        {
            if (glGetString(GL_VENDOR).ToLower().Contains("intel"))
                throw new System.Exception("CORE doesn't support Intel (i)GPUs. The software will exit in order to prevent any crashes");
        }

        private static void ReportFatalError(System.Exception err)
        {
            LogError(err);
            Environment.Exit(-1);
        }

        private static void RuntimeErrorHandling(System.Exception err)
        {
            Console.WriteError(err);
            errorsCaught++;
            appIsHealthy = false;
        }

        private static void RenderProtectedScene(string[] args)
        {
            try
            {
                RenderScene(args);
            }
            catch (System.Exception ex)
            {
                Console.WriteError($"Scene error: {ex}");
            }
        }

        private static double FrameSetup(double timeSinceLastFrame2)
        {
            newFrame = false;
            timeSinceLastFrame = Glfw.Time - timeSinceLastFrame2;

            return Glfw.Time;
        }

        private static void CheckForMousePicking()
        {
            UpdateSelectedID();
            arrows.UpdateArrowsMovement();
        }

        private static void RenderMonitorFramebuffer(Framebuffer gui)
        {
            gui.RenderFramebuffer();

            if (!fullscreen)
                glViewport(viewportX, viewportY, renderWidth, renderHeight); //make screen smaller for GUI space

            IDFramebuffer.RenderFramebuffer();

            CheckForMousePicking();

            renderFramebuffer.RenderFramebuffer();

            if (renderIDFramebuffer)
                DoIDFramebufferSetup();

            glViewport(0, 0, monitorWidth, monitorHeight);
        }

        private static void UpdateFrameDependentValues()
        {
            scrollWheelMoved = false;
            scrollWheelMovedAmount = 0;
            totalFrameCount++;
            appIsHealthy = true;
            newFrame = true;

            Rendering.UpdateUniformBuffers();
            UpdateRenderStatistics();
            UpdateCursorLocation();
        }

        private static void DestroySplashscreen()
        {
            if (destroyedWindow)
                return;

            splashScreen.Dispose();

            if (loadInfoOnstartup)
                console.ShowInfo();

            destroyedWindow = true;
        }

        private static void RenderGUI(Framebuffer gui, TitleBar tb, Button button, Button saveAsImage, TabManager tab, Div modelInformation, Div modelList, TabManager sceneManager)
        {
            gui.Bind();
            tb.CheckForUpdate(mousePosX, mousePosY);
            if (renderGUI)
            {
                tb.Render();

                button.Render();
                saveAsImage.Render();

                if ((keyIsPressed || mouseIsPressed) && !Submenu.isOpen) //only draw new stuff if the app is actively being used
                {
                    tab.Render();
                    sceneManager.Render();

                    modelInformation.Render();
                    modelList.RenderModelList(CurrentScene.models);
                }
                console.Update();
                console.Render();

                if (saveAsImage.isPressed)
                    SaveAsPNG();
            }
            Debugmenu.Render();

            clearedGUI = false;
        }

        private static void DoIDFramebufferSetup()
        {
            glViewport((int)(viewportX + renderWidth * 0.75f), (int)(viewportY + renderHeight * 0.75f), (int)(renderWidth * 0.25f), (int)(renderHeight * 0.25f));
            IDFramebuffer.RenderFramebuffer();
        }

        private static void DoRequiredFrameSetup()
        {
            SetIDFrameSetup();
            SetRenderFrameSetup();
            CheckForFullscreen();
        }

        private static void RenderOneFrame()
        {
            DoRequiredFrameSetup();

            if (addCube)
                AddCube();
            if (addCylinder)
                AddCylinder();

            CurrentScene.RenderEveryFrame(currentFrameTime);

            if (renderGrid)
                RenderGrid();

            if (!Arrows.disableArrows)
            {
                glClear(GL_DEPTH_BUFFER_BIT); //clear the buffer bit so that the arrows are always visible
                arrows.Render();
            }
        }

        private static void RenderScene(string[] args)
        {
            if (Scene.IsCursorInFrame(mousePosX, mousePosY))
                UpdateCamera(currentFrameTime);

            if (!mouseIsPressed)
                Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);

            if (!destroyedWindow || keyIsPressed || mouseIsPressed)
                RenderOneFrame();

            CurrentScene.EveryFrame(window, currentFrameTime);

            CheckIfDirectoryIsLoading(args);
        }

        private static void CheckIfDirectoryIsLoading(string[] args)
        {
            if (!renderEntireDir)
                return;

            if (LoadFilePath == null || args.Length == 0) //if there isnt any directory given, dont load it
                Console.WriteError("Can't load directory since one isn't given");
            else
            {
                string dir = Path.GetDirectoryName(LoadFilePath);
                LoadDir(dir);
            }
            renderEntireDir = false;
            menu.SetBool("Load entire directory", false);
        }

        private static void SetCPUValues()
        {
            previousTime = Glfw.Time;
            Process CPUProcess = Process.GetCurrentProcess();
            previousCPU = CPUProcess.TotalProcessorTime;
            CPUProcess.Dispose();
        }

        private static string GetBaseDirectory()
        {
            //get the root folder of the renderer by removing the .exe folders from the path (\bin\Debug\...)
            string root = AppDomain.CurrentDomain.BaseDirectory;
            string directory = Path.GetDirectoryName(root);
            int MathCIndex = directory.LastIndexOf("CORERenderer");

            return directory.Substring(0, MathCIndex) + "CORERenderer";
        }

        private static void GenerateNeededFramebuffers(out Framebuffer gui, out Framebuffer wrapperFBO)
        {
            gui = GenerateFramebuffer(monitorWidth, monitorHeight);
            renderFramebuffer = GenerateFramebuffer(monitorWidth, monitorHeight);
            wrapperFBO = GenerateFramebuffer(monitorWidth, monitorHeight);
            IDFramebuffer = GenerateFramebuffer(monitorWidth, monitorHeight);

            //test
            renderFramebuffer.shader.SetBool("useVignette", useVignette);
            renderFramebuffer.shader.SetFloat("vignetteStrength", 0.1f);
            renderFramebuffer.shader.SetBool("useChromaticAberration", useChromAber);
            renderFramebuffer.shader.SetVector3("chromAberIntensities", 0.014f, 0.009f, 0.006f);
        }

        private static void StartOtherProcesses()
        {
            LoadConfig();
            Overrides.AlwaysLoad();
            Rendering.Init(monitorWidth, monitorHeight);
        }

        public static void SaveAsPNG() => Texture.WriteAsPNG($"{BaseDirectory}\\Renders\\render{DateTime.Now.ToString()[(DateTime.Now.ToString().IndexOf(' ') + 1)..].Replace(':', '-')}.png", renderFramebuffer.Texture, renderFramebuffer.width, renderFramebuffer.height);

        private static void CalculateDimensions()
        {
            Width = monitorWidth;
            Height = monitorHeight;

            //sets the translation for the 3D space window
            viewportX = (int)(monitorWidth * 0.125f);
            viewportY = (int)(monitorHeight * 0.25f - 25);

            renderWidth = (int)(monitorWidth * 0.75f);
            renderHeight = (int)(monitorHeight * 0.727f);
        }

        private static void SetDefaultScene(string[] args)
        {
            scenes.Add(new());
            scenes[0].OnLoad(args);
            if (LoadFile == ModelType.CRSFile)
            {
                Scene local = scenes[0];
                Readers.LoadCRS(args[0], ref local, out string _);
            }
            SelectedScene = 0;
        }

        private static string GetGPU()
        {
            string GPUString = glGetString(GL_RENDERER);

            if (GPUString.IndexOf('/') != -1)
                return GPUString[..GPUString.IndexOf('/')];
            else if (GPUString.Contains("HD Graphics")) //intel iGPU
                return GPUString;
            else
                return "Not Recognized";
        }

        private static void AddCube() => AddModel("cube");
        private static void AddCylinder() => AddModel("cylinder");
        private static void AddModel(string model)
        {
            if (model == "cube")
            {
                CurrentScene.models.Add(Model.Cube);
                addCube = false;
                menu.SetBool("  Cube", addCube);
            }
            else if (model == "cylinder")
            {
                CurrentScene.models.Add(Model.Cylinder);
                addCylinder = false;
                menu.SetBool("  Cylinder", addCylinder);
            }
        }

        private static void CheckForFullscreen()
        {
            if (!KeyIsPressed(Keys.Escape))
                return;

            fullscreen = false;
            menu.SetBool("fullscreen", false);
        }

        private static void SetRenderFrameSetup()
        {
            renderFramebuffer.Bind(); //bind the framebuffer for the 3D scene

            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            glEnable(GL_DEPTH_TEST);
            SetClearColor(ClearColor);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);
        }

        private static void SetIDFrameSetup()
        {
            IDFramebuffer.Bind();

            glClearColor(1f, 1f, 1f, 1);
            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);
        }

        public static bool KeyIsPressed(Keys key) => Glfw.GetKey(window, key) == InputState.Press;
        public static bool MouseButtonIsPressed(MouseButton button) => Glfw.GetMouseButton(window, button) == InputState.Press;
        public static bool MouseButtonIsReleased(MouseButton button) => Glfw.GetMouseButton(window, button) == InputState.Release;

        public static void RestartWithoutArgsWithConfig() => Restart(true, false);
        public static void RestartWithArgsAndConfig() => Restart(true, true);
        public static void Restart(bool regenerateConfig, bool withArgs)
        {
            if (regenerateConfig)
                GenerateConfig();

            if (LoadFilePath == null || !withArgs)
                Process.Start("CORERenderer.exe");
            else
                Process.Start("CORERenderer.exe", LoadFilePath);

            Environment.Exit(1);
        }

        public static void MergeAllModels(out List<List<float>> vertices, out List<Vector3> offsets)
        {
            vertices = new();
            offsets = new();
            foreach (Model model in CurrentScene.models)
            {
                List<List<Vertex>> lv = model.Vertices;
                for (int i = 0; i < lv.Count; i++)
                {
                    vertices.Add(new());
                    foreach (Vertex v in lv[i])
                    {
                        vertices[^1].Add(v.x); vertices[^1].Add(v.y); vertices[^1].Add(v.z);
                        vertices[^1].Add(v.uvX); vertices[^1].Add(v.uvY);
                        vertices[^1].Add(v.normalX); vertices[^1].Add(v.normalY); vertices[^1].Add(v.normalZ);
                    }
                    offsets.Add(model.Offsets[i]);
                }
            }
        }

        public static void LoadDir(string dir)
        {
            string[] allFiles = Directory.GetFiles(dir);
            foreach (string file in allFiles)
                if (file != LoadFilePath && Path.GetExtension(file) == ".obj")
                    CurrentScene.models.Add(new(file));
        }

        public static Vector3 GenerateIDColor(int ID) => new(((ID & 0x000000FF) >> 0) / 255f, ((ID & 0x0000FF00) >> 8) / 255f, ((ID & 0x00FF0000) >> 16) / 255f); //bit manipulation to convert an ID to color values

        public static void UpdateSelectedID()
        {
            glFlush();
            glFinish();

            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
            //read color id of mouse position
            byte[] data = new byte[4];
            glReadPixels((int)mousePosX, (int)(monitorHeight - mousePosY), 1, 1, GL_RGBA, GL_UNSIGNED_BYTE, data);
            //convert color id to single int for uses, only change the id if the current one isnt being used (i.e being moved rotated or scaled
            if (Glfw.GetMouseButton(window, MouseButton.Right) != InputState.Press && Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Release && !arrows.isBeingUsed)
                selectedID = data[0] + data[1] * 256 + data[2] * 256 * 256;
            if (Glfw.GetMouseButton(window, MouseButton.Left) != InputState.Release && selectedID < 9)
                selectedID = NoIDSelected;
                
        }

        public static bool CheckAABBCollision(int x, int y, int width, int height) =>
            mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

        public static bool CheckAABBCollisionWithClick(int x, int y, int width, int height) =>
            MouseButtonIsPressed(MouseButton.Left) && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

        public static bool CheckAABBCollisionWithRelease(int x, int y, int width, int height) =>
            MouseButtonIsReleased(MouseButton.Left) && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

        private static void UpdateRenderStatistics()
        {
            double currentTime = Glfw.Time;
            frameCount++;
            // If a second has passed.
            if (currentTime - previousTime >= 1.0)
            {
                //calculates the fps
                fps = frameCount;
                currentFrameTime = 1.0f / frameCount;

                drawCallsPerSecond = drawCalls;
                drawCallsPerFrame = drawCallsPerSecond / frameCount;
                drawCalls = 0;

                Process proc = Process.GetCurrentProcess();
                TimeSpan currentCPU = proc.TotalProcessorTime;
                double timeDifference = currentTime - previousTime;
                double currentCPUUsage = (currentCPU - previousCPU).TotalSeconds;

                CPUUsage = Math.Round(currentCPUUsage / (Environment.ProcessorCount * timeDifference) * 100, 1);
                previousCPU = currentCPU;
                proc.Dispose();

                frameCount = 0;
                previousTime = currentTime;
                secondPassed = true;
            }
            else
                secondPassed = false;
        }

        /// <summary>
        /// Updates the cameras position if the assigned button for it is pressed
        /// </summary>
        /// <param name="delta"></param>
        private static void UpdateCamera(float delta)
        {
            if (!MouseButtonIsPressed(MouseButton.Right))
                return;

            Rendering.Camera.UpdatePosition(mousePosX, mousePosY, delta);
        }

        private static void UpdateCursorLocation()
        {
            Glfw.GetCursorPosition(window, out double mousePosXD, out double mousePosYD);
            mousePosX = (float)mousePosXD;
            mousePosY = (float)mousePosYD;
        }

        private static void DeleteAllBuffers()
        {
        }

        private static void SetModelType(string[] arg)
        {
            if (arg.Length <= 0)
                return;

            LoadFile = GetModelType(arg[0]);
            LoadFilePath = arg[0];
        }

        public static ModelType GetModelType(string arg)
        {
            string extension = Path.GetExtension(arg);
            
            if (!RenderModeLookUpTable.ContainsKey(extension))
                return ModelType.None;

            return RenderModeLookUpTable[extension];
        }

        public static void LogError(string msg)
        {
            if (!File.Exists($"{BaseDirectory}\\ErrorLog.txt"))
            {
                FileStream fs =  File.Create($"{BaseDirectory}\\ErrorLog.txt");
                fs.Close();
            }

            using StreamWriter sw = File.AppendText($"{BaseDirectory}\\ErrorLog.txt");
            sw.WriteLine($"Error occured at {DateTime.Now:h:mm:ss tt}: \n    {msg}");
        }

        public static void LogError(System.Exception msg) => LogError(msg.ToString());
    }
}