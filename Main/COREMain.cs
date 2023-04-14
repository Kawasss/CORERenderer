﻿#region using statements
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
        public static int fps = 0, frameCount = 0;
        public static int selectedScene = 0;

        public static int selectedID = 0x00FFFF, nextAvaibleID = 3; //white (background) //first 3 IDs are used by Arrows
        public static int NewAvaibleID { get { nextAvaibleID++; return nextAvaibleID - 1; } } //automatically generates a new ID whenever its asked for one
        public static int GetCurrentObjFromScene { get => scenes[selectedScene].currentObj; }

        //uints
        public static uint vertexArrayObjectLightSource, vertexArrayObjectGrid;

        //doubles
        private static double previousTime = 0;
        public static double CPUUsage = 0;
        private static TimeSpan previousCPU;

        //floats
        public static float mousePosX, mousePosY;

        private static float currentFrameTime = 0;

        //strings
        public static string LoadFilePath = null;
        public static string GPU = "Not Recognized";
        public const string VERSION = "v0.4.P";
        public static string pathRenderer;
        private static List<string> consoleCache = new();

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

        public static bool keyIsPressed = false, mouseIsPressed = false;
        public static bool clearedGUI = false;

        public static bool subMenuOpenLastFrame = false, submenuOpen = false;

        private static bool loadInfoOnstartup = true;

        //enums
        public static RenderMode LoadFile = RenderMode.CRSFile;
        public static Keys pressedKey;

        //classes
        public static List<Light> lights = new();
        public static List<Scene> scenes = new();

        public static SplashScreen splashScreen;
        public static Font debugText;
        public static COREConsole console;
        public static Arrows arrows;
        public static Div modelList;
        public static Submenu menu;

        public static Model GetCurrentModelFromCurrentScene { get => scenes[selectedScene].allModels[GetCurrentObjFromScene]; }
        public static Scene GetCurrentScene { get => scenes[selectedScene]; }

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
                //get the root folder of the renderer by removing the .exe folders from the path (\bin\Debug\...)
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string directory = Path.GetDirectoryName(root);
                int MathCIndex = directory.IndexOf("CORERenderer");
                
                pathRenderer = directory.Substring(0, MathCIndex) + "CORERenderer";
                //-------------------------------------------------------------------------------------------
                Glfw.Init();

                splashScreen = new();

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
                Graph graph = new(0, (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.224f - 25), viewportX, (int)(monitorHeight * 0.004f));
                Graph frametimeGraph = new(0, (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.224f - 25),viewportX, (int)(monitorHeight * 0.004f));
                Graph drawCallsPerFrameGraph = new(0,(int)(monitorWidth * 0.496 - monitorWidth * 0.125f),(int)(monitorHeight * 0.224f - 25),viewportX,(int)(monitorHeight * 0.004f));

                console = new((int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.242f - 25),monitorWidth - viewportX - (int)(monitorWidth * 0.496 - monitorWidth * 0.125f),(int)(monitorHeight * 0.004f));

                TabManager tab = new(new string[] { "Models", "Submodels" });
                TabManager graphManager = new(new string[] { "FT", "CPU %", "DCPF" });
                TabManager sceneManager = new("Scene");

                Button button = new("Scene", 5, monitorHeight - 25);
                Button saveAsImage = new("Save as PNG", 10 + 5 * (int)debugText.GetStringWidth("Scene", 1), monitorHeight - 25);
                menu = new(new string[] { "Render Grid", "Render Background", "Render Wireframe", "Render Normals", "Render GUI", "Render IDFramebuffer", "Render to ID framebuffer", "Render orthographic", "  ", "Cull Faces", " ", "Add Object:", "  Cube", "  Cylinder", "   ", "Load entire directory", "Allow alpha override", "Use chrom. aber.", "Use vignette", "fullscreen" });
                
                tab.AttachTo(modelList);
                tab.AttachTo(submodelList);
                graphManager.AttachTo(frametimeGraph);
                graphManager.AttachTo(graph);
                graphManager.AttachTo(drawCallsPerFrameGraph);
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
                glBindBuffer(GL_SHADER_STORAGE_BUFFER, ssbo);
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
                scenes[0].OnLoad(args);
                selectedScene = 0;

                #region Restoring any console commands from the previous session and start up
                if (File.Exists($"{pathRenderer}\\consoleCache"))
                    console.LoadCacheFile(pathRenderer);

                foreach (string s in consoleCache)
                {
                    if (s.Length > 5 && s[..5] == "ERROR")
                        console.WriteError(s);
                    else if (s.Length > 5 && s[..5] == "DEBUG")
                        console.WriteDebug(s);
                    else
                        console.WriteLine(s);
                }
                #endregion

                previousTime = Glfw.Time;
                using (Process process = Process.GetCurrentProcess())
                    previousCPU = process.TotalProcessorTime;

                Glfw.SetScrollCallback(window, ScrollCallback);
                Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);
                Glfw.SetKeyCallback(window, KeyCallback);
                Glfw.SetMouseButtonCallback(window, MouseCallback);
                
                Scene.EnableGLOptions();

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);

                Rendering.SetCamera(GetCurrentScene.camera);
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

                secondPassed = true; //cheap trick to make it think that its allowed to render
                graphManager.Render();
                secondPassed = false;

                sceneManager.Render();
                #endregion

                //render loop
                while (!Glfw.WindowShouldClose(window)) //maybe better to let the render loop run in Rendering.cs
                {
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

                            graph.Update((float)CPUUsage); //update even without any input because the data always changes
                            graph.MaxValue = 100;

                            drawCallsPerFrameGraph.Update(drawCallsPerFrame);

                            frametimeGraph.Update(currentFrameTime * 1000);
                            console.Update();

                            if ((keyIsPressed || mouseIsPressed) && !Submenu.isOpen) //only draw new stuff if the app is actively being used
                            {
                                tab.Render();

                                modelInformation.Render();

                                sceneManager.Render();
                            }
                            console.Render();

                            if (saveAsImage.isPressed)
                                Texture.WriteAsPNG($"{pathRenderer}\\Renders\\test.png", computeShader.Texture, renderFramebuffer.width, renderFramebuffer.height);
                        }
                        
                        graphManager.Render();
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
                                scenes[selectedScene].allModels.Add(new($"{pathRenderer}\\OBJs\\cube.obj"));
                                addCube = false;
                                menu.SetBool("  Cube", addCube);
                            }
                            if (addCylinder)
                            {
                                scenes[selectedScene].allModels.Add(new($"{pathRenderer}\\OBJs\\cylinder.obj"));
                                addCylinder = false;
                                menu.SetBool("  Cylinder", addCylinder);
                            }
                            #endregion

                            scenes[selectedScene].RenderEveryFrame(currentFrameTime);

                            if (renderGrid)
                                RenderGrid();

                            //glClear(GL_DEPTH_BUFFER_BIT); //clear the buffer bit so that the arrows are always visible
                            //arrows.Render();
                        }
                        else if (!mouseIsPressed)
                            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
                        #endregion


                        scenes[selectedScene].EveryFrame(window, currentFrameTime);

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
                                Readers.LoadMTL(model.mtllib, model.mtlNames, out List<Material> materials, out int error); //has to load the .mtl's here, otherwise it results in black textures, since in the Task.Run from LoadDir() takes in another context, could be fixed by rerouting the opengl calls in LoadMTL to this context instead of doing the calls inisde LoadMTL
                                GetCurrentScene.allModels.Add(new(model.path, model.vertices, model.indices, materials, model.offsets));
                            }
                            dirLoadedModels = null;
                        }
                        #endregion
                    }
                    #endregion

                    #region Compute shader related events
                    {
                        glActiveTexture(GL_TEXTURE1);
                        glBindTexture(GL_TEXTURE_2D, renderFramebuffer.Texture);

                        int error = GetError();
                        comp.Use();

                        comp.SetVector3("cameraPos", GetCurrentScene.camera.position);
                        comp.SetVector3("lookAt", GetCurrentScene.camera.front);
                        comp.SetVector3("right", GetCurrentScene.camera.right);
                        comp.SetVector3("up", GetCurrentScene.camera.up);
                        comp.SetVector3("forward", GetCurrentScene.camera.front);

                        comp.SetVector3("position", new(0, 20, 0));
                        comp.SetVector3("color", new(1, 0, 1));
                        comp.SetFloat("radius", 1);

                        comp.SetInt("backgroundImage", GL_TEXTURE1);

                        glBindBufferBase(GL_SHADER_STORAGE_BUFFER, bindingIndex, ssbo);
                        glBindImageTexture(0, texture.Handle, 0, false, 0, GL_READ_WRITE, GL_RGBA32F);

                        texture.Use(GL_TEXTURE0);

                        glDispatchCompute((uint)Math.Ceiling((double)Width / 8), (uint)Math.Ceiling((double)Height / 8), 1);
                        glMemoryBarrier(GL_ALL_BARRIER_BITS);
                    }
                    #endregion

                    #region Assembling the screen shown on the monitor
                    gui.RenderFramebuffer();

                    if (!fullscreen)
                        glViewport(viewportX, viewportY, renderWidth, renderHeight); //make screen smaller for GUI space

                    //check for mouse picking
                    IDFramebuffer.RenderFramebuffer();
                    UpdateSelectedID();

                    computeShader.RenderFramebuffer();

                    glViewport((int)(viewportX + renderWidth * 0.75f), (int)(viewportY + renderHeight * 0.75f) + 1, (int)(renderWidth * 0.25f), (int)(renderHeight * 0.25f));
                    renderFramebuffer.RenderFramebuffer();

                    if (renderIDFramebuffer)
                    {
                        glViewport((int)(viewportX + renderWidth * 0.75f), (int)(viewportY + renderHeight * 0.75f), (int)(renderWidth * 0.25f), (int)(renderHeight * 0.25f));
                        IDFramebuffer.RenderFramebuffer();
                    }

                    glViewport(0, 0, monitorWidth, monitorHeight);
                    #endregion

                    Glfw.SwapBuffers(window);
                    Glfw.PollEvents();

                    //destroys the splashscreen after the first render loop to present it more "professionally"
                    if (!destroyWindow)
                    {
                        destroyWindow = true;
                        splashScreen.Dispose();

                        if (loadInfoOnstartup)
                            console.ShowInfo();
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

        public static void MergeAllModels(out List<List<float>> vertices, out List<List<uint>> indices, out List<Vector3> offsets)
        {
            vertices = new();
            indices = new();
            offsets = new();
            foreach (Model model in GetCurrentScene.allModels)
                for (int i = 0; i < model.vertices.Count; i++)
                {
                    vertices.Add(model.vertices[i]);
                    indices.Add(model.indices[i]);
                    offsets.Add(model.offsets[i]);
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

                text = sr.ReadLine();
                COREConsole.writeDebug = text.Contains("True");

                text = sr.ReadLine();
                COREConsole.writeError = text.Contains("True");

                text = sr.ReadLine();
                loadInfoOnstartup = text.Contains("True");

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
            List<string> readFiles = new(), mtllibs = new();
            List<List<List<float>>> allVertices = new();
            List<List<List<uint>>> allIndices = new();
            List<List<Vector3>> allOffsets = new();
            List<List<string>> mtlnames = new();
            List<Model> models = scenes[selectedScene].allModels;
            List<ModelInfo> localVersion = new();
            Task.Run(() =>
            {
                Parallel.ForEach(allFiles, file =>
                {
                    if (file[^4..].ToLower() == ".obj" && file != LoadFilePath) //loads every obj in given directory except for the one already read// && !readFiles.Contains(file)
                    {
                        readFiles.Add(file);
                        Readers.LoadOBJ(file, out List<string> mtlNames, out List<List<float>> vertices, out List<List<uint>> indices, out List<Vector3> offsets, out string mtllib);

                        allVertices.Add(vertices);
                        allIndices.Add(indices);
                        allOffsets.Add(offsets);
                        mtllibs.Add(dir + '\\' + mtllib);
                        mtlnames.Add(mtlNames);
                    }
                });
                loaded = true;
                if (loaded)
                    for (int i = 0; i < readFiles.Count; i++)
                        localVersion.Add(new(readFiles[i], mtllibs[i], mtlnames[i], allVertices[i], allIndices[i], allOffsets[i]));
                dirLoadedModels = localVersion;
            });
        }

        private struct ModelInfo
        {
            public string path;
            public string mtllib;
            public List<string> mtlNames;
            public List<List<float>> vertices;
            public List<List<uint>> indices;
            public List<Vector3> offsets;

            public ModelInfo(string path, string mtllib, List<string> mtlNames, List<List<float>> vertices, List<List<uint>> indices, List<Vector3> offsets)// List<Material> materials,
            {
                this.path = path;
                this.mtllib = mtllib;
                this.mtlNames = mtlNames;
                this.vertices = vertices;
                this.indices = indices;
                this.offsets = offsets;
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
            //convert color id to single int for uses
            if (Glfw.GetMouseButton(window, MouseButton.Right) != InputState.Press)
                selectedID = data[0] + data[1] * 256 + data[2] * 256 * 256;
        }

        //zoom in or out
        public static void ScrollCallback(Window window, double x, double y) =>
            scenes[selectedScene].camera.Fov -= (float)y * 1.5f;

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

        private static void MouseCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers) => mouseIsPressed = state == InputState.Press;

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