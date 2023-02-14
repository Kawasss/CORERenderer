﻿using COREMath;
using CORERenderer.Fonts;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.GUI;
using CORERenderer.Loaders;
using System;
using System.Diagnostics.SymbolStore;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.Main
{
    public class COREMain
    {
        [NotNull]
        //ints
        public static int Width, Height;
        public static int monitorWidth, monitorHeight;
        public static int renderWidth, renderHeight;
        public static int viewportX, viewportY;

        public static int drawCallsPerSecond = 0;
        public static int drawCallsPerFrame = 0;
        public static int fps = 0;
        public static int selectedScene = 0;
        
        private static int frameCount = 0;

        //uints
        public static uint vertexArrayObjectLightSource, vertexArrayObjectGrid;

        private static uint uboMatrices;

        //doubles
        private static double previousTime = 0;

        //floats
        public static float mousePosX, mousePosY;

        private static float currentFrameTime = 0;

        //strings
        public static string LoadFilePath;
        public static string GPU;
        public const string VERSION = "v0.1.P";

        //bools
        public static bool renderGrid = true;
        public static bool renderBackground = true;
        public static bool secondPassed = true;
        public static bool renderGUI = false;

        public static bool destroyWindow = false;
        public static bool addCube = false;
        public static bool addCylinder = false;

        private static bool keyIsPressed = false;
        private static bool mouseIsPressed = false;

        //enums
        public static RenderMode LoadFile = RenderMode.CRSFile;

        //classes
        public static List<Light> lights = new();
        public static List<Scene> scenes = new();

        //public static Camera camera;
        public static SplashScreen splashScreen;
        public static Font debugText;
        public static COREConsole console;
        public static Mouse mouse;

        //structs
        public static Window window;

        //misc.
        //get the root folder of the renderer by removing the .exe folders from the path (\bin\Debug\...)
        private static string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
        private static string directory = Path.GetDirectoryName(root);

        private static int MathCIndex = directory.IndexOf("COREPrototype");

        public static string pathRenderer = directory.Substring(0, MathCIndex) + "COREPrototype\\COREPrototype";

        public static int Main(string[] args)
        {
            try //primitive error handling, could be better
            {
                //-------------------------------------------------------------------------------------------
                Glfw.Init();

                splashScreen = new();

                //sets the width for the window that shows the 3D space
                Width = monitorWidth;
                Height = monitorHeight;

                //sets the translation for the 3D space window
                viewportX = (int)(monitorWidth * 0.125f);
                viewportY = (int)(monitorHeight * 0.25f - 25);

                renderWidth = (int)(monitorWidth * 0.75f);
                renderHeight = (int)(monitorHeight * 0.727f);

                SetRenderMode(args);

                vertexArrayObjectLightSource = GenerateBufferlessVAO();
                vertexArrayObjectGrid = GenerateBufferlessVAO();

                Overrides.AlwaysLoad();
                GenericShaders.SetShaders();

                if (glGetString(GL_RENDERER).IndexOf('/') != -1)
                    GPU = glGetString(GL_RENDERER)[..glGetString(GL_RENDERER).IndexOf('/')];
                else
                    GPU = "Not Recognized";//glGetString(GL_RENDERER);
                
                debugText = new((uint)(monitorHeight * 0.01333f), $"{pathRenderer}\\Fonts\\baseFont.ttf");

                //seperate into own method for easier reading------------------------------------------------
                TitleBar tb = new();

                Div modelList = new
                (
                    (int)(monitorWidth * 0.117f), 
                    (int)(monitorHeight * 0.725f), 
                    (int)(monitorWidth * 0.004f), 
                    (int)(monitorHeight * 0.25f - 25 + 2.5f)
                );
                Div submodelList = new
                (
                    (int)(monitorWidth * 0.117f),
                    (int)(monitorHeight * 0.725f),
                    (int)(monitorWidth * 0.004f),
                    (int)(monitorHeight * 0.25f - 25 + 2.5f)
                );
                Div modelInformation = new
                (
                    (int)(monitorWidth * 0.117f), 
                    (int)(monitorHeight * 0.725f), 
                    (int)(monitorWidth * 0.879f),
                    (int)(monitorHeight * 0.25f - 25 + 2.5f)
                );
                Div GPUInfo = new
                (
                    (int)(monitorWidth * 0.117f),
                    (int)(monitorHeight * 0.242f - 25),
                    (int)(monitorWidth * 0.879f),
                    (int)(monitorHeight * 0.004f)
                );
                Graph graph = new
                (
                    0, 
                    (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), 
                    (int)(monitorHeight * 0.224f - 25), 
                    viewportX, 
                    (int)(monitorHeight * 0.004f)
                );
                Graph frametimeGraph = new
                (
                    0, 
                    (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), 
                    (int)(monitorHeight * 0.224f - 25),
                    viewportX, 
                    (int)(monitorHeight * 0.004f)
                );

                console = new
                (
                    (int)(monitorWidth * 0.496 - monitorWidth * 0.125f), 
                    (int)(monitorHeight * 0.242f - 25),
                    monitorWidth - viewportX - (int)(monitorWidth * 0.496 - monitorWidth * 0.125f),//viewportX + (int)(monitorWidth * 0.375f * 0.008f) + (int)(monitorWidth * 0.5 - monitorWidth * 0.125f), 
                    (int)(monitorHeight * 0.004f)
                );
                TabManager tab = new(new string[] { "Models", "Submodels" });
                TabManager graphManager = new(new string[] { "FPS", "FS" });
                TabManager sceneManager = new("Scene");

                Button button = new("Scene", 5, monitorHeight - 25);
                Submenu menu = new(new string[] { "Render Grid", "Render Background", "Render Wireframe", "Render Normals", "Render GUI", "  ", "Cull Faces", " ", "Add Object:", "  Cube", "  Cylinder" });

                tab.AttachTo(modelList);
                tab.AttachTo(submodelList);
                graphManager.AttachTo(graph);
                graphManager.AttachTo(frametimeGraph);
                menu.AttachTo(ref button);
                menu.SetBool("Render Grid", renderGrid);
                menu.SetBool("Render Background", renderBackground);
                menu.SetBool("Render GUI", renderGUI);
                menu.SetBool("Cull Faces", cullFaces);
                menu.SetBool("  Cube", addCube);
                menu.SetBool("  Cylinder", addCylinder);
                
                modelList.RenderModelList();
                submodelList.RenderSubmodelList();
                //-------------------------------------------------------------------------------------------

                scenes.Add(new());
                scenes[0].OnLoad();
                selectedScene = 0;

                Arrows arrows = new();
                mouse = new();

                SetUniformBuffers();

                Framebuffer gui = GenerateFramebuffer();
                Framebuffer fbo = GenerateFramebuffer();
                Framebuffer wrapperFBO = GenerateFramebuffer();

                Glfw.SetScrollCallback(window, ScrollCallback);
                Glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallBack);
                Glfw.SetKeyCallback(window, KeyCallback);
                Glfw.SetMouseButtonCallback(window, MouseCallback);

                Scene.EnableGLOptions();
                glViewport(0, 0, monitorWidth, monitorHeight);
                //-------------------------------------------------------------------------------------------
                if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
                    throw new GLFW.Exception("Framebuffer is not complete");
                glBindFramebuffer(GL_FRAMEBUFFER, 0);

                splashScreen.WriteLine($"Initialised in {Math.Round(Glfw.Time, 1)} seconds");
                Thread.Sleep(500); //allows user to read the printed text
                console.WriteLine($"Initialised in {Glfw.Time} seconds");
                console.WriteLine("Beginning render loop");

                previousTime = Glfw.Time;

                glStencilFunc(GL_ALWAYS, 1, 0xFF);
                glStencilMask(0xFF);

                //render loop
                while (!Glfw.WindowShouldClose(window))
                {
                    UpdateRenderStatistics();

                    UpdateUniformBuffers();
                    glViewport(0, 0, monitorWidth, monitorHeight);

                    wrapperFBO.Bind();

                    {
                        gui.Bind();
                        if (renderGUI)
                        {
                            if (!destroyWindow || keyIsPressed || mouseIsPressed || Submenu.isOpen)
                            {
                                glClearColor(0.085f, 0.085f, 0.085f, 1);
                                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

                                tb.CheckForUpdate(mousePosX, mousePosY);
                                tb.Render();

                                tab.Render();

                                modelInformation.Render();

                                //modelList.Render();

                                button.Render();

                                console.RenderEvenIfNotChanged();
                            }

                            graph.Update(fps); //update even without any input because the data always changes
                            frametimeGraph.Update(currentFrameTime * 1000);
                            graphManager.Render();

                            sceneManager.Render();

                            console.Render();

                            GPUInfo.Render();

                            //GPUInfo.Write($"DCPS: {drawCallsPerSecond}", 5, (int)(GPUInfo.Height - debugText.characterHeight * 0.75f - 5), 0.8f);
                            //GPUInfo.Write($"DCPF: {drawCallsPerFrame}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 2), 0.8f);

                            GPUInfo.Write($"GPU: {GPU}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 4), 0.8f);
                            GPUInfo.Write($"OpenGL {glGetString(GL_VERSION)}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 5), 0.8f);
                            GPUInfo.Write($"GLSL {glGetString(GL_SHADING_LANGUAGE_VERSION)}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 6), 0.8f);
                            GPUInfo.Write($"CORE Renderer {VERSION}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 7), 0.8f);
                            GPUInfo.Write($"COREMath {MathC.VERSION}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 8), 0.8f);
                        }
                        else
                        {
                            glClearColor(0.085f, 0.085f, 0.085f, 1);
                            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);
                        }
                        if (!renderGUI)
                            button.Render();
                        GPUInfo.Write($"DCPS: {drawCallsPerSecond}", 5, (int)(GPUInfo.Height - debugText.characterHeight * 0.75f - 5), 0.8f);
                        GPUInfo.Write($"DCPF: {drawCallsPerFrame}", 5, (int)(GPUInfo.Height - (debugText.characterHeight * 0.75f + 5) * 2), 0.8f);
                        gui.RenderFramebuffer();
                    }

                    UpdateCursorLocation();

                    if (Scene.IsCursorInFrame(mousePosX, mousePosY))
                        UpdateCamera(currentFrameTime);

                    {
                        fbo.Bind(); //bind the framebuffer for the 3D scene
                        if (!destroyWindow || keyIsPressed || mouseIsPressed)
                        {
                            glEnable(GL_DEPTH_TEST);
                            glClearColor(0.3f, 0.3f, 0.3f, 1);
                            glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);

                            scenes[selectedScene].EveryFrame(window, currentFrameTime);

                            if (addCube)
                            {
                                scenes[selectedScene].allModels.Add(new($"{pathRenderer}\\Loaders\\testOBJ\\cube.obj"));
                                addCube = false;
                                menu.SetBool("  Cube", addCube);
                            }
                            if (addCylinder)
                            {
                                scenes[selectedScene].allModels.Add(new($"{pathRenderer}\\Loaders\\testOBJ\\cylinder.obj"));
                                addCylinder = false;
                                menu.SetBool("  Cylinder", addCylinder);
                            }

                            scenes[selectedScene].RenderEveryFrame(currentFrameTime);

                            if (renderGrid)
                                RenderGrid();

                            glClear(GL_DEPTH_BUFFER_BIT);

                            arrows.Render();

                            mouse.Pick((int)mousePosX, Height - (int)mousePosY);
                        }
                        else
                            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
                        
                    }

                    glViewport(viewportX, viewportY, renderWidth, renderHeight); //make screen smaller for GUI space
                    fbo.RenderFramebuffer();
                    glViewport(0, 0, monitorWidth, monitorHeight);

                    //wrapperFBO.Bind();

                    Glfw.SwapBuffers(window);
                    Glfw.PollEvents();

                    //destroys the splashscreen after the first render loop to present it more "professionally"
                    if (!destroyWindow)
                    {
                        destroyWindow = true;
                        splashScreen.Dispose();
                    }
                }
                DeleteAllBuffers();
                Glfw.Terminate();
            }
            catch (System.Exception err)
            {
                //console.WriteError(err);
                //console.Render();
                //Glfw.SwapBuffers(window);
                LogError(err);
                Thread.Sleep(1000);
                return -1;
            }
            return 1;
        }

        //zoom in or out
        public static void ScrollCallback(Window window, double x, double y)
        {
            scenes[selectedScene].camera.Fov -= (float)y * 1.5f;
        }

        public static bool CheckAABBCollision(int x, int y, int width, int height)
        {
            return mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;
        }

        public static bool CheckAABBCollisionWithClick(int x, int y, int width, int height)
        {
            return Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Press && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;
        }

        public static bool CheckAABBCollisionWithRelease(int x, int y, int width, int height)
        {
            return Glfw.GetMouseButton(window, MouseButton.Left) == InputState.Release && mousePosX >= x && mousePosX <= x + width && monitorHeight - mousePosY >= y && monitorHeight - mousePosY <= y + height;
        }

        private static void UpdateRenderStatistics()
        {
            double currentTime = Glfw.Time;
            frameCount++;
            // If a second has passed.
            if (currentTime - previousTime >= 1.0)
            {
                // Display the frame count here any way you want.
                fps = frameCount;
                currentFrameTime = 1.0f / frameCount;

                drawCallsPerSecond = drawCalls;
                drawCallsPerFrame = drawCallsPerSecond / frameCount;
                drawCalls = 0;

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
            if (Glfw.GetMouseButton(window, MouseButton.Middle) != InputState.Press)
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
        }

        private static void MouseCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
        {
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
            foreach(Scene scene in scenes)
                if (LoadFile == RenderMode.CRSFile)
                {
                    if (scene.allModels.Count > 0)
                        Console.Write("\nDeleting buffers");

                    for (int i = 0; i < scene.allModels.Count; i++)
                    {
                        if (scene.allModels[i].type == RenderMode.ObjFile)
                        {
                            glDeleteBuffers(scene.allModels[i].GeneratedBuffers.ToArray());
                            glDeleteBuffers(scene.allModels[i].elementBufferObject.ToArray());
                            glDeleteVertexArrays(scene.allModels[i].GeneratedVAOs.ToArray());
                            glDeleteShader(scene.allModels[i].shader.Handle);
                            Console.Write($"..{i}");
                        }
                    }
            }
            Console.WriteLine();
        }

        private static void SetRenderMode(string[] arg)
        {
            if (arg.Length <= 0)
                return;
            else if (arg[0][^4..].ToLower() == ".obj")
            {
                LoadFile = RenderMode.ObjFile; LoadFilePath = arg[0];
            }
            else if (arg[0][^4..].ToLower() == ".png")
            {
                LoadFile = RenderMode.PNGImage; LoadFilePath = arg[0];
            }
            else if (arg[0][^4..].ToLower() == ".jpg")
            {
                LoadFile = RenderMode.JPGImage; LoadFilePath = arg[0];
            }
            else if (arg[0][^4..].ToLower() == ".crs")
            {
                LoadFile = RenderMode.CRSFile; LoadFilePath = arg[0];
            }
            else if (arg[0][^4..].ToLower() == ".hdr")
            {
                LoadFile = RenderMode.HDRFile; LoadFilePath = arg[0];
            }
            else
                LoadFile = RenderMode.CRSFile;
        }

        public static RenderMode SetRenderMode(string arg)
        {
            if (arg[^4..].ToLower() == ".obj")
                return RenderMode.ObjFile;

            else if (arg[^4..].ToLower() == ".png")
                return RenderMode.PNGImage;

            else if (arg[^4..].ToLower() == ".jpg")
                return RenderMode.JPGImage;

            else if (arg[^4..].ToLower() == ".crs")
                return RenderMode.CRSFile;

            else if (arg[^4..].ToLower() == ".hdr")
                return RenderMode.HDRFile;

            return RenderMode.CRSFile;
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

        private unsafe static void SetUniformBuffers()
        {
            uboMatrices = glGenBuffer();

            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            glBufferData(GL_UNIFORM_BUFFER, 3 * GL_MAT4_FLOAT_SIZE, NULL, GL_STATIC_DRAW);
            glBindBuffer(GL_UNIFORM_BUFFER, 0);
            glBindBufferRange(GL_UNIFORM_BUFFER, 0, uboMatrices, 0, 3 * GL_MAT4_FLOAT_SIZE);

            //assigns values to the freed up gpu memory for global uniforms
            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            MatrixToUniformBuffer(scenes[selectedScene].camera.GetProjectionMatrix(), 0);
            //MatrixToUniformBuffer(Rendering.GetOrthograpicProjectionMatrix(), 0);
            glBindBuffer(GL_UNIFORM_BUFFER, 0);
        }

        private static void UpdateUniformBuffers()
        {
            //assigns values to the freed up gpu memory for global uniforms
            glBindBuffer(GL_UNIFORM_BUFFER, uboMatrices);
            MatrixToUniformBuffer(scenes[selectedScene].camera.GetViewMatrix(), GL_MAT4_FLOAT_SIZE);
            MatrixToUniformBuffer(scenes[selectedScene].camera.GetTranslationlessViewMatrix(), GL_MAT4_FLOAT_SIZE * 2);
        }
    }
}
