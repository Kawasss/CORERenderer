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
using CORERenderer.shaders;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using Assimp;
#endregion

namespace CORERenderer.Main
{
    public class COREMain
    {
        [NotNull]
        #region Declarations
        //ints
        public static int Width, Height, monitorWidth, monitorHeight, renderWidth, renderHeight, viewportX, viewportY;

        public static int drawCallsPerSecond = 0, drawCallsPerFrame = 0;
        public static int fps = 0, frameCount = 0, totalFrameCount = 0;

        private static int selectedScene = 0;
        public static int SelectedScene { get => selectedScene; set { if (value != selectedScene) Rendering.Camera = scenes[value].camera; selectedID = value; } }

        public static int selectedID = 0x00FFFF, nextAvaibleID = 9; //white (background) //first 9 IDs are used by Arrows
        public static int NewAvaibleID { get { nextAvaibleID++; return nextAvaibleID - 1; } } //automatically generates a new ID whenever its asked for one
        public static int GetCurrentObjFromScene { get => scenes[selectedScene].currentObj; }
        public static int FrameCount { get { return totalFrameCount; } }
        public const int NoIDSelected = 0x00FFFF;

        public static int refreshRate = 0;
        public static int errorsCaught = 0;

        //uints
        public static uint vertexArrayObjectLightSource;

        //doubles
        private static double previousTime = 0;
        public static double CPUUsage = 0;
        public static double scrollWheelMovedAmount = 0;
        public static double timeSinceLastFrame = 0;
        private static TimeSpan previousCPU;

        //floats
        public static float mousePosX, mousePosY;

        private static float currentFrameTime = 0;
        public static float FrameTime { get { return (float)timeSinceLastFrame; } }

        //strings
        public static string LoadFilePath = null;
        public static string GPU = "Not Recognized";
        public const string VERSION = "v0.6";
        public static string BaseDirectory { get => pathRenderer; }
        private static string pathRenderer;

        //bools
        public static bool renderGrid = true, renderBackground = true, renderGUI = true, renderIDFramebuffer = false, renderToIDFramebuffer = true; //rendering related options
        public static bool secondPassed = true;
        public static bool useChromAber = false, useVignette = false; //post processing related options
        public static bool fullscreen = false;

        public static bool destroyWindow = false;
        public static bool addCube = false, addCylinder = false; //add object related options
        public static bool renderEntireDir = false;
        public static bool allowAlphaOverride = true;
        public static bool isCompiledForWindows = false;
        public static bool newFrame = false;

        public static bool keyIsPressed = false, mouseIsPressed = false, scrollWheelMoved = false;
        public static bool clearedGUI = false;

        public static bool subMenuOpenLastFrame = false, submenuOpen = false;

        private static bool loadInfoOnstartup = true;
        public static bool appIsHealthy = true;

        //enums
        public static ModelType LoadFile = ModelType.None;
        public static Keys pressedKey;
        public static MouseButton pressedButton;

        //classes
        public static List<Scene> scenes = new();

        public static SplashScreen splashScreen;
        public static Font debugText;
        public static Console console = null;
        public static Arrows arrows;
        public static Div modelList;
        public static Submenu menu;

        public static Model CurrentModel { get { if (CurrentScene.currentObj == -1 && CurrentScene.models.Count > 0) CurrentScene.currentObj = CurrentScene.models.Count - 1; return CurrentScene.models[GetCurrentObjFromScene]; } }
        public static Scene CurrentScene { get => scenes[SelectedScene]; }

        private static Thread mainThread;
        public static Thread MainThread { get { return mainThread; } }

        //structs
        public static Window window;
        public static Framebuffer IDFramebuffer;
        public static Framebuffer renderFramebuffer;

        public static uint ssbo;
        public static ComputeShader comp;
        #endregion

        public static int Main(string[] args)
        {
            #if OS_WINDOWS
                isCompiledForWindows = true;
            #endif
            try //primitive error handling, could be better
            {
                mainThread = Thread.CurrentThread;

                //get the root folder of the renderer by removing the .exe folders from the path (\bin\Debug\...)
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string directory = Path.GetDirectoryName(root);
                int MathCIndex = directory.LastIndexOf("CORERenderer");

                pathRenderer = directory.Substring(0, MathCIndex) + "CORERenderer";
                //-------------------------------------------------------------------------------------------
                Glfw.Init();

                splashScreen = new();
                refreshRate = splashScreen.refreshRate;
                //sets the width for the window that shows the 3D space
                #region Calculates all of the appropriate dimensions
                Width = monitorWidth;
                Height = monitorHeight;

                //sets the translation for the 3D space window
                viewportX = (int)(monitorWidth * 0.125f);
                viewportY = (int)(monitorHeight * 0.25f - 25);

                renderWidth = (int)(monitorWidth * 0.75f);
                renderHeight = (int)(monitorHeight * 0.727f);
                #endregion

                SetRenderMode(args);

                #region Starting other processes
                Overrides.AlwaysLoad();
                Rendering.Init(monitorWidth, monitorHeight);
                LoadConfig();
                #endregion
                System.Console.WriteLine(GenericShaders.Log);
                vertexArrayObjectLightSource = GenerateBufferlessVAO();

                #region Identifying GPU and their associated shortcomings
                string GPUString = glGetString(GL_RENDERER);

                if (GPUString.IndexOf('/') != -1)
                    GPU = GPUString[..GPUString.IndexOf('/')];
                else if (GPUString.Contains("HD Graphics")) //intel iGPU
                    GPU = GPUString;
                else
                    GPU = "Not Recognized";
                #endregion

                debugText = new((uint)(monitorHeight * 0.01333), $"{BaseDirectory}\\Fonts\\baseFont.ttf");

                #region Initializing the GUI, and setting the appriopriate values
                TitleBar tb = new();

                modelList = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.004f),(int)(monitorHeight * 0.004f));
                Div submodelList = new((int)(monitorWidth * 0.117f),(int)(monitorHeight * 0.974f - 25),(int)(monitorWidth * 0.004f),(int)(monitorHeight * 0.004f));
                Div modelInformation = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.879f),(int)(monitorHeight * 0.004f));

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

                //modelList.RenderModelList();
                //submodelList.RenderSubmodelList();
                #endregion

                #region Framebuffer related events
                Framebuffer gui = GenerateFramebuffer(monitorWidth, monitorHeight);
                renderFramebuffer = GenerateFramebuffer(monitorWidth, monitorHeight);
                Framebuffer wrapperFBO = GenerateFramebuffer(monitorWidth, monitorHeight);
                IDFramebuffer = GenerateFramebuffer(monitorWidth, monitorHeight);
                Framebuffer computeShader = GenerateFramebuffer(monitorWidth, monitorHeight);

                //test
                renderFramebuffer.shader.SetBool("useVignette", useVignette);
                renderFramebuffer.shader.SetFloat("vignetteStrength", 0.1f);
                renderFramebuffer.shader.SetBool("useChromaticAberration", useChromAber);
                renderFramebuffer.shader.SetVector3("chromAberIntensities", 0.014f, 0.009f, 0.006f);
                #endregion

                #region Setting up the compute shader for ray-tracing
                comp = new($"{BaseDirectory}\\shaders\\test.comp");
                Texture texture = Texture.GenerateEmptyTexture(Width, Height);

                comp.Use();

                uint blockIndex = glGetProgramResourceIndex(comp.Handle, GL_SHADER_STORAGE_BLOCK, "VertexData");
                uint bindingIndex = 1;
                glShaderStorageBlockBinding(comp.Handle, blockIndex, bindingIndex);
                ssbo = glGenBuffer();
                GetError();
                glBindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
                glBufferData(GL_SHADER_STORAGE_BUFFER, 23620, (IntPtr)null, GL_DYNAMIC_DRAW);
                glBindBufferBase(GL_SHADER_STORAGE_BUFFER, bindingIndex, ssbo);
                //glBindBufferRange(GL_SHADER_STORAGE_BUFFER, 0, ssbo, 0, 0);
                //Console.WriteLine(GetError());
                comp.SetInt("imgOutput", GL_TEXTURE0);
                computeShader.Texture = texture.Handle;

                //Lamp lamp = new(new(0, 0, 0), new(1, 1, 1), 2);
                //lamp.BindTo(comp);
                #endregion

                //sets the default scene
                scenes.Add(new());
                SelectedScene = 0;

                previousTime = Glfw.Time;
                Process CPUProcess = Process.GetCurrentProcess();
                previousCPU = CPUProcess.TotalProcessorTime;

                Glfw.SetScrollCallback(window, ScrollCallback);
                Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);
                Glfw.SetKeyCallback(window, KeyCallback);
                Glfw.SetMouseButtonCallback(window, MouseCallback);
                
                Scene.EnableGLOptions();

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);
                arrows = new();

                scenes[0].OnLoad(args);

                if (LoadFile == ModelType.CRSFile)
                {
                    Scene local = scenes[0];
                    Readers.LoadCRS(args[0], ref local, out string _);
                }

                //Rendering.Camera = CurrentScene.camera;
                Rendering.SetUniformBuffers();

                glViewport(0, 0, monitorWidth, monitorHeight);
                //-------------------------------------------------------------------------------------------
                if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
                    throw new GLFW.Exception("Framebuffer is not complete");
                glBindFramebuffer(GL_FRAMEBUFFER, 0);

                //splashScreen.WriteLine($"Initialised in {Math.Round(Glfw.Time, 1)} seconds");
                //Thread.Sleep(500); //allows user to read the printed text
                Console.WriteLine($"Initialised in {Glfw.Time} seconds");
                Console.WriteLine("Beginning render loop");
             
                double timeSinceLastFrame2 = Glfw.Time;
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

                button.changed = true; //cheap trick to make it think that its allowed to render
                button.RenderStatic();
                saveAsImage.RenderStatic();

                console.RenderEvenIfNotChanged();

                sceneManager.Render();
                #endregion
                //render loop
                while (!Glfw.WindowShouldClose(window)) //maybe better to let the render loop run in Rendering.cs
                {
                    try
                    {
                        newFrame = false;
                        timeSinceLastFrame = Glfw.Time - timeSinceLastFrame2;
                        timeSinceLastFrame2 = Glfw.Time;

                        Rendering.UpdateUniformBuffers();
                        UpdateRenderStatistics();
                        UpdateCursorLocation();

                        glViewport(0, 0, monitorWidth, monitorHeight);

                        #region GUI related events
                        {
                            gui.Bind();
                            tb.CheckForUpdate(mousePosX, mousePosY);
                            if (renderGUI)
                            {
                                //tb.Render();
                                
                                button.Render();
                                saveAsImage.Render();

                                console.Update();
                                if ((keyIsPressed || mouseIsPressed) && !Submenu.isOpen) //only draw new stuff if the app is actively being used
                                {
                                    button.Render();
                                    saveAsImage.Render();

                                    tab.Render();

                                    modelInformation.Render();
                                    modelInformation.RenderModelInformation();

                                    //modelList.RenderModelList(CurrentScene.models);

                                    sceneManager.Render();
                                }
                                
                                console.Render();
                                
                                if (saveAsImage.isPressed)
                                    Texture.WriteAsPNG($"{BaseDirectory}\\Renders\\render{DateTime.Now.ToString()[(DateTime.Now.ToString().IndexOf(' ') + 1)..].Replace(':', '-')}.png", renderFramebuffer.Texture, renderFramebuffer.width, renderFramebuffer.height);
                            }
                            Debugmenu.Render();

                            clearedGUI = false;
                        }
                        #endregion

                        #region Scene related events
                        try
                        {
                            if (Scene.IsCursorInFrame(mousePosX, mousePosY))
                                UpdateCamera(currentFrameTime);

                            #region Rendering related stuff if the app is actively being used
                            if (!destroyWindow || keyIsPressed || mouseIsPressed)
                            {
                                #region Generic OpenGL calls like cleaning the bits
                                IDFramebuffer.Bind();

                                glClearColor(1f, 1f, 1f, 1);
                                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

                                renderFramebuffer.Bind(); //bind the framebuffer for the 3D scene

                                glEnable(GL_BLEND);
                                glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
                                glEnable(GL_DEPTH_TEST);
                                SetClearColor(ClearColor);
                                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);
                                #endregion

                                if (Glfw.GetKey(window, Keys.Escape) == InputState.Press && fullscreen)
                                {
                                    fullscreen = false;
                                    menu.SetBool("fullscreen", false);
                                }

                                #region Add certain shapes based on GUI input
                                if (addCube)
                                {
                                    CurrentScene.models.Add(Model.Cube);
                                    addCube = false;
                                    menu.SetBool("  Cube", addCube);
                                    CurrentScene.currentObj = CurrentScene.models.Count - 1;
                                }
                                if (addCylinder)
                                {
                                    CurrentScene.models.Add(Model.Cylinder);
                                    addCylinder = false;
                                    menu.SetBool("  Cylinder", addCylinder);
                                }
                                #endregion

                                scenes[SelectedScene].RenderEveryFrame(currentFrameTime);

                                if (renderGrid)
                                    RenderGrid();

                                glClear(GL_DEPTH_BUFFER_BIT); //clear the buffer bit so that the arrows are always visible
                                arrows.Render();
                            }
                            else if (!mouseIsPressed)
                                Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
                            #endregion

                            CurrentScene.EveryFrame(window, currentFrameTime);

                            #region Checks if a directory wants to be loaded, last part checks whether it is done loading by checking for a null value
                            if (renderEntireDir)
                            {
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
                            #endregion
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteError($"Scene error: {ex}");
                        }
                        #endregion

                        #region Compute shader related events
                        {
                            /*glActiveTexture(GL_TEXTURE1); //buffer overflow
                            glBindTexture(GL_TEXTURE_2D, renderFramebuffer.Texture);

                            int error = GetError();
                            comp.Use();
                            comp.SetFloat("frametime", currentFrameTime);
                            comp.SetVector3("cameraPos", CurrentScene.camera.position);
                            comp.SetVector3("lookAt", CurrentScene.camera.front);
                            comp.SetVector3("right", CurrentScene.camera.right);
                            comp.SetVector3("up", CurrentScene.camera.up);
                            comp.SetVector3("forward", CurrentScene.camera.front);

                            comp.SetVector3("position", new(0, 20, 0));
                            comp.SetVector3("color", new(1, 0, 1));
                            comp.SetFloat("radius", 1);

                            comp.SetInt("backgroundImage", GL_TEXTURE1);

                            glBindBufferBase(GL_SHADER_STORAGE_BUFFER, bindingIndex, ssbo);
                            glBindImageTexture(0, texture.Handle, 0, false, 0, GL_READ_WRITE, GL_RGBA32F);

                            texture.Use(GL_TEXTURE0);

                            glDispatchCompute((uint)Math.Ceiling((double)Width / 8), (uint)Math.Ceiling((double)Height / 8), 1);
                            glMemoryBarrier(GL_ALL_BARRIER_BITS);*/
                        }
                        #endregion

                        #region Assembling the screen shown on the monitor
                        gui.RenderFramebuffer();

                        if (!fullscreen)
                            glViewport(viewportX, viewportY, renderWidth, renderHeight); //make screen smaller for GUI space

                        //check for mouse picking
                        IDFramebuffer.RenderFramebuffer();
                        UpdateSelectedID();
                        arrows.UpdateArrowsMovement();

                        //computeShader.RenderFramebuffer();
                        
                        //glViewport((int)(viewportX + renderWidth * 0.75f), (int)(viewportY + renderHeight * 0.75f) + 1, (int)(renderWidth * 0.25f), (int)(renderHeight * 0.25f));
                        renderFramebuffer.RenderFramebuffer();
                        
                        if (renderIDFramebuffer)
                        {
                            glViewport((int)(viewportX + renderWidth * 0.75f), (int)(viewportY + renderHeight * 0.75f), (int)(renderWidth * 0.25f), (int)(renderHeight * 0.25f));
                            IDFramebuffer.RenderFramebuffer();
                        }

                        glViewport(0, 0, monitorWidth, monitorHeight);
                        #endregion

                        scrollWheelMoved = false;
                        scrollWheelMovedAmount = 0;
                        totalFrameCount++;
                        appIsHealthy = true;
                        newFrame = true;
                    }
                    catch (System.Exception err)
                    {
                        Console.WriteError(err);
                        errorsCaught++;
                        appIsHealthy = false;
                    }
                    Glfw.SwapBuffers(window);
                    Glfw.PollEvents();

                    //destroys the splashscreen after the first render loop to present it more "professionally"
                    if (!destroyWindow)
                    {
                        splashScreen.Dispose();

                        if (loadInfoOnstartup)
                            console.ShowInfo();

                        destroyWindow = true;
                    }
                }
                DeleteAllBuffers();
                Glfw.Terminate();
            }
            catch (System.Exception err)
            {
                LogError(err);
                Thread.Sleep(1000);
                return -1;
            }
            return 0;
        }

        public static bool KeyIsPressed(Keys key) => Glfw.GetKey(window, key) == InputState.Press;
        public static bool MouseButtonIsPressed(MouseButton button) => Glfw.GetMouseButton(window, button) == InputState.Press;

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

        private static void LoadConfig()
        {
            if (!File.Exists($"{BaseDirectory}\\config"))
            {
                Console.WriteError($"Couldn't locate config, generating new config");
                GenerateConfig();
                return;
            }

            using (FileStream fs = File.Open($"{BaseDirectory}\\config", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                string text = sr.ReadLine();

                if (VERSION != text[(text.IndexOf('=') + 1)..])
                {
                    Console.WriteError($"Config is outdated, generating new config");
                    GenerateConfig();
                    return;
                }

                text = sr.ReadLine();

                if (text.Contains("Lighting"))
                    shaderConfig = ShaderType.Lighting;
                else if (text.Contains("PathTracing"))
                    shaderConfig = ShaderType.PathTracing;
                else if (text.Contains("FullBright"))
                    shaderConfig = ShaderType.FullBright;

                text = sr.ReadLine();

                Camera.cameraSpeed = float.Parse(text[(text.IndexOf('=') + 1)..]);
                GUI.Console.writeDebug = sr.ReadLine().Contains("True");
                GUI.Console.writeError = sr.ReadLine().Contains("True");
                loadInfoOnstartup = sr.ReadLine().Contains("True");

                Console.WriteDebug("Loaded config file");
            }
        }

        public static void GenerateConfig()
        {
            using (StreamWriter sw = File.CreateText($"{BaseDirectory}\\config"))
            {
                sw.WriteLine($"version={VERSION}");
                sw.WriteLine($"shaders={shaderConfig}");
                sw.WriteLine($"cameraSpeed={Camera.cameraSpeed}");
                sw.WriteLine($"writedebug={GUI.Console.writeDebug}");
                sw.WriteLine($"writeerror={GUI.Console.writeError}");
                sw.WriteLine($"loadinfo={loadInfoOnstartup}");
            }
            Console.WriteDebug("Generated new config");
        }

        public static void LoadDir(string dir)
        {
            string[] allFiles = Directory.GetFiles(dir);
            foreach (string file in allFiles)
                if (file != LoadFilePath && Path.GetExtension(file) == ".obj")
                    CurrentScene.models.Add(new(file));
        }

        private struct ModelInfo
        {
            public string path;
            public string mtllib;
            public List<string> mtlNames;
            public List<List<Vertex>> vertices;
            public List<List<uint>> indices;
            public List<Vector3> offsets;
            public Vector3 extents;
            public Vector3 center;

            public ModelInfo(string path, string mtllib, List<string> mtlNames, List<List<Vertex>> vertices, List<List<uint>> indices, List<Vector3> offsets, Vector3 extents, Vector3 center)// List<Material> materials,
            {
                this.path = path;
                this.mtllib = mtllib;
                this.mtlNames = mtlNames;
                this.vertices = vertices;
                this.indices = indices;
                this.offsets = offsets;
                this.extents = extents;
                this.center = center;
            }
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
            if (Glfw.GetMouseButton(window, MouseButton.Right) != InputState.Press && Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Press && !arrows.isBeingUsed)
                selectedID = data[0] + data[1] * 256 + data[2] * 256 * 256;
            if (Glfw.GetMouseButton(window, MouseButton.Left) != InputState.Press && selectedID < 9)
                selectedID = NoIDSelected;
                
        }

        //zoom in or out
        public static void ScrollCallback(Window window, double x, double y)
        {
            scrollWheelMoved = true;
            scrollWheelMovedAmount = y;
        }

        public static bool CheckAABBCollision(int x, int y, int width, int height) =>
            mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

        public static bool CheckAABBCollisionWithClick(int x, int y, int width, int height) =>
            Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Press && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

        public static bool CheckAABBCollisionWithRelease(int x, int y, int width, int height) =>
            Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Release && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;

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
            if (Glfw.GetMouseButton(window, MouseButton.Right) != InputState.Press)
                return;

            Rendering.Camera.UpdatePosition(mousePosX, mousePosY, delta);
        }

        private static void UpdateCursorLocation()
        {
            Glfw.GetCursorPosition(window, out double mousePosXD, out double mousePosYD);
            mousePosX = (float)mousePosXD;
            mousePosY = (float)mousePosYD;
        }

        private static void KeyCallback(Window window, Keys key, int scancode, InputState action, ModifierKeys mods)
        {   //saves a lot of energy by only updating if input is detected
            keyIsPressed = action == InputState.Press;
            pressedKey = key;
        }

        private static void MouseCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
        {
            pressedButton = button;
            mouseIsPressed = state == InputState.Press;
        }

            private static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            glViewport(0, 0, width, height);
            monitorWidth = width;
            monitorHeight = height;

            renderWidth = (int)(width * 0.75f);
            renderHeight = (int)(height * 0.727f);

            scenes[SelectedScene].camera.AspectRatio = (float)renderWidth / (float)renderHeight;
        }

        private static void DeleteAllBuffers()
        {
        }

        private static void SetRenderMode(string[] arg)
        {
            if (arg.Length <= 0)
                return;

            LoadFile = GetRenderMode(arg[0]);
            LoadFilePath = arg[0];
        }

        public static ModelType GetRenderMode(string arg)
        {
            string extension = Path.GetExtension(arg);
            //System.Console.WriteLine(extension);
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