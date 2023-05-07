#region using statements
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
using System.Reflection.Metadata.Ecma335;
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
        public static int selectedScene = 0;

        public static int selectedID = 0x00FFFF, nextAvaibleID = 9; //white (background) //first 9 IDs are used by Arrows
        public static int NewAvaibleID { get { nextAvaibleID++; return nextAvaibleID - 1; } } //automatically generates a new ID whenever its asked for one
        public static int GetCurrentObjFromScene { get => scenes[selectedScene].currentObj; }
        public static int FrameCount { get { return totalFrameCount; } }
        public const int NoIDSelected = 0x00FFFF;

        public static int refreshRate = 0;
        private static int errorsCaught = 0;

        //uints
        public static uint vertexArrayObjectLightSource, vertexArrayObjectGrid;

        //doubles
        private static double previousTime = 0;
        public static double CPUUsage = 0;
        public static double scrollWheelMovedAmount = 0;
        private static double timeSinceLastFrame = 0;
        private static TimeSpan previousCPU;

        //floats
        public static float mousePosX, mousePosY;

        private static float currentFrameTime = 0;
        public static float FrameTime { get { return currentFrameTime; } }

        //strings
        public static string LoadFilePath = null;
        public static string GPU = "Not Recognized";
        public const string VERSION = "v0.6";
        public static string pathRenderer;
        public static List<string> consoleCache = new();

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
        private static bool appIsHealthy = true;

        //enums
        public static RenderMode LoadFile = RenderMode.CRSFile;
        public static Keys pressedKey;
        public static MouseButton pressedButton;

        //classes
        public static List<Light> lights = new();
        public static List<Scene> scenes = new();

        public static SplashScreen splashScreen;
        public static Font debugText;
        public static COREConsole console = null;
        public static Arrows arrows;
        public static Div modelList;
        public static Submenu menu;

        public static Model CurrentModel { get => scenes[selectedScene].models[GetCurrentObjFromScene]; }
        public static Scene CurrentScene { get => scenes[selectedScene]; }

        private static Thread mainThread;
        public static Thread MainThread { get { return mainThread; } }

        //structs
        public static Window window;
        public static Framebuffer IDFramebuffer;
        public static Framebuffer renderFramebuffer;

        private static List<ModelInfo> dirLoadedModels = null;


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
                int MathCIndex = directory.IndexOf("CORERenderer");
                
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

                vertexArrayObjectLightSource = GenerateBufferlessVAO();
                vertexArrayObjectGrid = GenerateBufferlessVAO();

                #region Starting other processes
                Overrides.AlwaysLoad();
                LoadConfig();
                Rendering.Init();
                #endregion

                #region Identifying GPU and their associated shortcomings
                string GPUString = glGetString(GL_RENDERER);

                if (GPUString.IndexOf('/') != -1)
                    GPU = GPUString[..GPUString.IndexOf('/')];
                else if (GPUString.Contains("HD Graphics")) //intel iGPU
                    GPU = GPUString;
                else
                    GPU = "Not Recognized";
                #endregion

                debugText = new((uint)(monitorHeight * 0.01333), $"{pathRenderer}\\Fonts\\baseFont.ttf");

                #region Initializing the GUI, and setting the appriopriate values
                TitleBar tb = new();

                modelList = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.004f),(int)(monitorHeight * 0.004f));
                Div submodelList = new((int)(monitorWidth * 0.117f),(int)(monitorHeight * 0.974f - 25),(int)(monitorWidth * 0.004f),(int)(monitorHeight * 0.004f));
                Div modelInformation = new((int)(monitorWidth * 0.117f), (int)(monitorHeight * 0.974f - 25), (int)(monitorWidth * 0.879f),(int)(monitorHeight * 0.004f));
                Div debugHolder = new((int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.242f - 25), viewportX, (int)(monitorHeight * 0.004f));
                //Graph graph = new(0, (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.224f - 25), viewportX, (int)(monitorHeight * 0.004f));
                //Graph frametimeGraph = new(0, (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.224f - 25),viewportX, (int)(monitorHeight * 0.004f));
                //Graph drawCallsPerFrameGraph = new(0,(int)(monitorWidth * 0.496 - monitorWidth * 0.125f),(int)(monitorHeight * 0.224f - 25),viewportX,(int)(monitorHeight * 0.004f));

                int debugWidth = (int)debugText.GetStringWidth("Ticks spent depth sorting: timeSpentDepthSorting", 0.7f);
                Graph renderingTicks = new(0, debugWidth, (int)(debugText.characterHeight * 2), viewportX - (int)(debugWidth * 0.045f), (int)(debugHolder.Height - debugText.characterHeight * 12));
                Graph debugFSGraph = new(0, debugWidth, (int)(debugText.characterHeight * 2), (int)(monitorWidth * 0.5f - debugWidth * 1.00f), (int)(debugHolder.Height - debugText.characterHeight * 7));
                debugFSGraph.showValues = false;
                renderingTicks.showValues = false;

                console = new((int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.242f - 25),monitorWidth - viewportX - (int)(monitorWidth * 0.496 - monitorWidth * 0.125f),(int)(monitorHeight * 0.004f));
                console.GenerateConsoleErrorLog(pathRenderer);

                TabManager tab = new(new string[] { "Models", "Submodels" });
                //TabManager graphManager = new(new string[] { "FT", "CPU %", "DCPF" });
                TabManager sceneManager = new("Scene");

                Button button = new("Scene", 5, monitorHeight - 25);
                Button saveAsImage = new("Save as PNG", 10 + 5 * (int)debugText.GetStringWidth("Scene", 1), monitorHeight - 25);
                menu = new(new string[] { "Render Grid", "Render Background", "Render Wireframe", "Render Normals", "Render GUI", "Render IDFramebuffer", "Render to ID framebuffer", "Render orthographic", "  ", "Cull Faces", " ", "Add Object:", "  Cube", "  Cylinder", "   ", "Load entire directory", "Allow alpha override", "Use chrom. aber.", "Use vignette", "fullscreen" });
                
                tab.AttachTo(modelList);
                tab.AttachTo(submodelList);
                //graphManager.AttachTo(frametimeGraph);
                //graphManager.AttachTo(graph);
                //graphManager.AttachTo(drawCallsPerFrameGraph);
                menu.AttachTo(ref button);
                button.OnClick(menu.Render);
                menu.SetBool("Render Grid", renderGrid);
                menu.SetBool("Render Background", renderBackground);
                menu.SetBool("Render GUI", renderGUI);
                menu.SetBool("Render IDFramebuffer", renderIDFramebuffer);
                menu.SetBool("Render to ID framebuffer", renderToIDFramebuffer);
                menu.SetBool("Render orthographic", Rendering.renderOrthographic);
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
                comp = new($"{pathRenderer}\\shaders\\test.comp");
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
                selectedScene = 0;

                #region Restoring any console commands from the previous session and start up
                if (File.Exists($"{pathRenderer}\\consoleCache"))
                foreach (string s in consoleCache)
                {
                    if (s.StartsWith("ERROR "))
                        console.WriteError(s);
                    else if (s.StartsWith("DEBUG "))
                        console.WriteDebug(s);
                    else
                        console.WriteLine(s);
                }
                #endregion

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

                scenes[0].OnLoad(args);

                Rendering.SetCamera(CurrentScene.camera);
                Rendering.SetUniformBuffers();

                glViewport(0, 0, monitorWidth, monitorHeight);
                //-------------------------------------------------------------------------------------------
                if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
                    throw new GLFW.Exception("Framebuffer is not complete");
                glBindFramebuffer(GL_FRAMEBUFFER, 0);

                //splashScreen.WriteLine($"Initialised in {Math.Round(Glfw.Time, 1)} seconds");
                //Thread.Sleep(500); //allows user to read the printed text
                console.WriteLine($"Initialised in {Glfw.Time} seconds");
                console.WriteLine("Beginning render loop");

                arrows = new();
                double timeSinceLastFrame2 = Glfw.Time;
                #region First time rendering
                Rendering.UpdateUniformBuffers();
                UpdateRenderStatistics();
                UpdateCursorLocation();

                gui.Bind();
                tb.Render();

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

                //secondPassed = true; //cheap trick to make it think that its allowed to render
                //graphManager.Render();
                //secondPassed = false;

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
                            tb.Render();
                            if (renderGUI)
                            {
                                button.Render();
                                saveAsImage.Render();

                                //graph.Update((float)CPUUsage); //update even without any input because the data always changes
                                //graph.MaxValue = 100;

                                //drawCallsPerFrameGraph.Update(drawCallsPerFrame);

                                //frametimeGraph.Update(currentFrameTime * 1000);
                                console.Update();
                                if ((keyIsPressed || mouseIsPressed) && !Submenu.isOpen) //only draw new stuff if the app is actively being used
                                {
                                    tab.Render();

                                    modelInformation.Render();
                                    modelInformation.RenderModelInformation();

                                    //modelList.RenderModelList(CurrentScene.models);

                                    sceneManager.Render();
                                }
                                
                                console.Render();
                                
                                if (saveAsImage.isPressed)
                                    Texture.WriteAsPNG($"{pathRenderer}\\Renders\\test.png", computeShader.Texture, renderFramebuffer.width, renderFramebuffer.height);
                            }

                            debugHolder.Render();
                            renderingTicks.UpdateConditionless(TicksSpent3DRenderingThisFrame);
                            if (debugFSGraph.MaxValue > 70) debugFSGraph.MaxValue = (int)(timeSinceLastFrame * 1000 * 1.5f);
                            debugFSGraph.color = 1 / timeSinceLastFrame < refreshRate / 2 ? new(1, 0, 0) : new(1, 0, 1);
                            debugFSGraph.UpdateConditionless((float)(timeSinceLastFrame * 1000));
                            renderingTicks.RenderConditionless();
                            debugFSGraph.RenderConditionless();
                            glDisable(GL_CULL_FACE);
                            ShowRenderStatistics(debugHolder);

                            //graphManager.Render();
                            tb.CheckForUpdate(mousePosX, mousePosY);


                            clearedGUI = false;
                        }
                        #endregion

                        #region Scene related events
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
                                glClearColor(0.3f, 0.3f, 0.3f, 1);
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

                                scenes[selectedScene].RenderEveryFrame(currentFrameTime);

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
                                    console.WriteError("Can't load directory since one isn't given");
                                else
                                {
                                    string dir = Path.GetDirectoryName(LoadFilePath);
                                    LoadDir(dir);
                                }
                                renderEntireDir = false;
                                menu.SetBool("Load entire directory", false);
                            }
                            if (dirLoadedModels != null) //dirLoadedModels isnt null when all models from a directory are done loading (this needs to be checked because theyre imported via a seperate thread)
                            {
                                foreach (ModelInfo model in dirLoadedModels)
                                {
                                    Readers.LoadMTL(model.mtllib, model.mtlNames, out List<Material> materials); //has to load the .mtl's here, otherwise it results in black textures, since in the Task.Run from LoadDir() takes in another context, could be fixed by rerouting the opengl calls in LoadMTL to this context instead of doing the calls inisde LoadMTL
                                    CurrentScene.models.Add(new(model.path, model.vertices, model.indices, materials, model.offsets, model.center, model.extents));
                                }
                                dirLoadedModels = null;
                                timeSinceLastFrame2 = Glfw.Time;
                            }
                            #endregion
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
                        console.WriteError(err);
                        errorsCaught++;
                        appIsHealthy = false;
                        //Console.WriteLine(err);
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

        private static void ShowRenderStatistics(Div debugHolder)
        {
            string[] results = RenderStatistics;
            debugText.drawWithHighlights = true;
            for (int i = 0; i < results.Length; i++)
            {
                string result = results[i];
                debugHolder.Write(result, 0, debugHolder.Height - debugText.characterHeight * (i + 1), 0.7f, new(1, 1, 1));
            }
            debugHolder.Write($"Camera position: {MathC.Round(CurrentScene.camera.position, 2)}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 5), 0.7f, new(1, 1, 1));
            debugHolder.Write($"Camera front: {MathC.Round(CurrentScene.camera.front, 2)}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 6), 0.7f, new(1, 1, 1));
            debugHolder.Write($"Selected scene: {selectedScene}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 7), 0.7f, new(1, 1, 1));

            string msg = $"Threads used: {Job.usedThreads + 1}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight, 0.7f, new(1, 1, 1));
            msg = $"CPU usage: {CPUUsage}%";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 2, 0.7f, new(1, 1, 1));
            msg = $"Framecount: {totalFrameCount}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 3, 0.7f, new(1, 1, 1));
            msg = $"Frametime: {Math.Round(timeSinceLastFrame * 1000, 3)} ms";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 4, 0.7f, new(1, 1, 1));
            msg = $"FPS: {(int)(1 / timeSinceLastFrame)}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 5, 0.7f, new(1, 1, 1));
            msg = $"Errors caught: {errorsCaught}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 8, 0.7f, new(1, 1, 1));
            string status = appIsHealthy ? "OK" : "BAD";
            msg = $"App status: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 9, 0.7f, new(1, 1, 1));
            status = keyIsPressed ? pressedKey.ToString() : mouseIsPressed ? pressedButton.ToString() : "None";
            msg = $"Input callback: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 11, 0.7f, new(1, 1, 1));
            msg = $"Render IDs: {renderToIDFramebuffer}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 13, 0.7f, new(1, 1, 1));
            status = renderToIDFramebuffer ?  1 / timeSinceLastFrame > refreshRate / 2 ? "OK" : "BAD" : "Unknown";
            msg = $"ID rendering performance: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 14, 0.7f, new(1, 1, 1));
            debugText.drawWithHighlights = false;
        }

        public static void MergeAllModels(out List<List<float>> vertices, out List<Vector3> offsets)
        {
            vertices = new();
            offsets = new();
            foreach (Model model in CurrentScene.models)
                for (int i = 0; i < model.Vertices.Count; i++)
                {
                    vertices.Add(model.Vertices[i]);
                    offsets.Add(model.Offsets[i]);
                }
        }

        private static void LoadConfig()
        {
            if (!File.Exists($"{pathRenderer}\\config"))
            {
                consoleCache.Add($"ERROR Couldn't locate config, generating new config");
                GenerateConfig();
                return;
            }

            using (FileStream fs = File.Open($"{pathRenderer}\\config", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                string text = sr.ReadLine();

                if (VERSION != text[(text.IndexOf('=') + 1)..])
                {
                    consoleCache.Add($"ERROR Config is outdated, generating new config");
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
                COREConsole.writeDebug = sr.ReadLine().Contains("True");
                COREConsole.writeError = sr.ReadLine().Contains("True");
                loadInfoOnstartup = sr.ReadLine().Contains("True");

                consoleCache.Add("DEBUG Loaded config file");
            }
        }

        public static void GenerateConfig()
        {
            using (StreamWriter sw = File.CreateText($"{pathRenderer}\\config"))
            {
                sw.WriteLine($"version={VERSION}");
                sw.WriteLine($"shaders={shaderConfig}");
                sw.WriteLine($"cameraSpeed={Camera.cameraSpeed}");
                sw.WriteLine($"writedebug={COREConsole.writeDebug}");
                sw.WriteLine($"writeerror={COREConsole.writeError}");
                sw.WriteLine($"loadinfo={loadInfoOnstartup}");
            }
            consoleCache.Add("DEBUG Generated new config");
        }

        public static void LoadDir(string dir)
        {
            bool loaded = false;
            string[] allFiles = Directory.GetFiles(dir);
            List<ModelInfo> localVersion = new();
            new Job(() =>
            {
                Job.ParallelForEach(allFiles, file =>
                {
                    if (file[^4..].ToLower() == ".obj" && file != LoadFilePath) //loads every obj in given directory except for the one already read// && !readFiles.Contains(file)
                    {
                        //readFiles.Add(file);

                        Error error = Readers.LoadOBJ(file, out List<string> mtlNames, out List<List<float>> vertices, out List<List<uint>> indices, out List<Vector3> offsets, out Vector3 center, out Vector3 extents, out string mtllib);
                        if (error != Error.None)
                            console.WriteError($"Couldn't read {Path.GetFileName(file)}: {error}");
                        else
                        {
                            localVersion.Add(new(file, dir + '\\' + mtllib, mtlNames, vertices, indices, offsets, extents, center));
                        }
                    }
                });
                loaded = true;
                if (loaded)
                    dirLoadedModels = localVersion;
            }).Start();
        }

        private struct ModelInfo
        {
            public string path;
            public string mtllib;
            public List<string> mtlNames;
            public List<List<float>> vertices;
            public List<List<uint>> indices;
            public List<Vector3> offsets;
            public Vector3 extents;
            public Vector3 center;

            public ModelInfo(string path, string mtllib, List<string> mtlNames, List<List<float>> vertices, List<List<uint>> indices, List<Vector3> offsets, Vector3 extents, Vector3 center)// List<Material> materials,
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

            scenes[selectedScene].camera.UpdatePosition(mousePosX, mousePosY, delta);
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

            scenes[selectedScene].camera.AspectRatio = (float)renderWidth / (float)renderHeight;
        }

        private static void DeleteAllBuffers()
        {
        }

        private static void SetRenderMode(string[] arg)
        {
            if (arg.Length <= 0)
                return;

            LoadFile = SetRenderMode(arg[0]);
            LoadFilePath = arg[0];
        }

        public static RenderMode SetRenderMode(string arg)
        {
            return RenderModeLookUpTable[arg[^4..].ToLower()];
        }

        public static void LogError(string msg)
        {
            if (!File.Exists($"{pathRenderer}\\ErrorLog.txt"))
            {
                FileStream fs =  File.Create($"{pathRenderer}\\ErrorLog.txt");
                fs.Close();
            }

            using StreamWriter sw = File.AppendText($"{pathRenderer}\\ErrorLog.txt");
            sw.WriteLine($"Error occured at {DateTime.Now:h:mm:ss tt}: \n    {msg}");
        }

        public static void LogError(System.Exception msg) => LogError(msg.ToString());
    }
}