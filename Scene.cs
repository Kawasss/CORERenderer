using COREMath;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Main;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW.Enums;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using Console = CORERenderer.GUI.Console;
using CORERenderer.textures;

namespace CORERenderer
{
    public class Scene : Overrides
    {
        public Camera camera;

        public List<Model> models = new();
        public List<Light> lights = new();

        public Skybox skybox = null;

        public bool loaded = false;

        public int currentObj = -1;

        private Vector2 lastPos = null;

        public override void OnLoad(string[] args)
        {
            models = new();
            lights = new();
            camera = new(new(0, 1, 5), (float)renderWidth / (float)renderHeight);
            Rendering.Camera = camera;

            if (args.Length != 0 && LoadFile != ModelType.None && LoadFile != ModelType.CRSFile)
            {
                if (LoadFile != ModelType.HDRFile && LoadFile != ModelType.CPBRFile)
                {
                    loaded = true;
                    models.Add(new(args[0]));
                    currentObj = 0;
                }
                else if (LoadFile == ModelType.HDRFile)
                    skybox = Skybox.ReadFromFile(args[0], Rendering.TextureQuality);
                else if (LoadFile == ModelType.CPBRFile)
                {
                    models.Add(Model.Sphere);
                    models[^1].submodels[0].material = Readers.LoadCPBR(args[0]);
                    currentObj = 0;
                }
            }
            for (int i = 0; i < models.Count; i++)
                if (models[i].terminate)
                {
                    Console.WriteError($"Deleting terminated model {i}: {models[i].error}");
                    models.RemoveAt(i);
                }
            if (lights.Count == 0)
                lights.Add(new() { position = new(1, 2, 1) });
            lights.Add(new() { position = new(0, 1, 2) });
            skybox ??= DefaultSkybox;
            /*skybox = HDRTexture.ReadFromFile("C:\\Users\\wveen\\Downloads\\highres.hdr", Rendering.TextureQuality);

            models.Add(new($"{pathRenderer}\\OBJs\\sphere.obj"));
            models[^1].Transform.translation = new(3, 3, 3);

            models.Add(new($"{pathRenderer}\\OBJs\\sphere.obj"));
            models[^1].Transform.translation = new(2, 1, 0);
            models[^1].Transform.scale = new(.1f, .1f, .1f);

            models.Add(new($"{pathRenderer}\\OBJs\\sphere.obj"));

            lights.Add(new() { position = new(1, 2, 1) });
            camera.position = new(-1.37f, -0.65f, -1.28f);
            camera.front = new(.9f, .35f, 0.25f);
            camera.right = MathC.Normalize(MathC.GetCrossProduct(camera.front, Vector3.UnitVectorY));
            camera.up = MathC.Normalize(MathC.GetCrossProduct(camera.right, camera.front));*/
        }

        public override void RenderEveryFrame(float delta)
        {
            try
            {
                for (int i = 0; i < (Bone.bones.Count > 128 ? 128 : Bone.bones.Count); i++) //the max amount of bones is 128
                    GenericShaders.Lighting.SetMatrix($"boneMatrices[{i}]", Bone.bones[i].ModelMatrix);

                if (shaderConfig == ShaderType.PathTracing)
                {
                    GenericShaders.Lighting.SetVector3("RAY.origin", Rendering.Camera.position);
                    GenericShaders.Lighting.SetVector3("RAY.direction", Rendering.Camera.front);
                    GenericShaders.Lighting.SetInt("isReflective", 0);
                    GenericShaders.Lighting.SetVector3("emission", new(1, 1, 1));
                    GenericShaders.Lighting.SetVector3("lights.color", new(1, 1, 1));
                    GenericShaders.Lighting.SetVector3("lights.position", new(0, 1, 1));
                }
                else if (shaderConfig == ShaderType.PBR)
                {
                    GenericShaders.Lighting.SetVector3("viewPos", Rendering.Camera.position);
                    GenericShaders.Lighting.SetVector3("lightPos[0]", lights[0].position);
                    GenericShaders.Lighting.SetVector3("lightPos[1]", lights[1].position);
                    //GenericShaders.GenericLighting.SetInt("skybox", 6);
                }
                RenderScene(this);
            }
            catch (System.Exception err)
            {
                Console.WriteError($"Rendering error: {err}");
            }
        }

        private int previousHighlighted = -1;
        private double previousTime = 0;
        public override void EveryFrame(Window window, float delta)
        {
            loaded = models.Count > 0;
            
            for (int i = 0; i < models.Count; i++) //inefficient, better to have a dictionary to look it up
            {
                if (models[i].terminate)
                    models.RemoveAt(i);

                else if (selectedID == models[i].ID && Glfw.Time - previousTime > 0.01)
                {
                    if (previousHighlighted != -1)
                    {
                        models[i].highlighted = !models[i].highlighted;
                        if (previousHighlighted != i)
                            models[previousHighlighted].highlighted = false;
                    }
                    else models[i].highlighted = true;
                    
                    currentObj = i;
                    previousHighlighted = i;
                    previousTime = Glfw.Time;
                }
            }

            if (loaded && models[^1].terminate)
            {
                Console.WriteError($"Terminating model {models.Count - 1}: {models[^1].error}");
                models.RemoveAt(models.Count - 1);
            }

            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press && Glfw.GetKey(window, Keys.LeftShift) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }

            lastPos ??= new(mousePosX, monitorHeight - mousePosY);
            
            float deltaX = lastPos.x - mousePosX;
            float deltaY = monitorHeight - mousePosY - lastPos.y;
            
            lastPos = new(mousePosX, monitorHeight - mousePosY);

            InputState state = Glfw.GetMouseButton(window, MouseButton.Right);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Left);

            if (IsCursorInFrame(mousePosX, mousePosY))
            {
                Rendering.Camera.Fov -= (float)scrollWheelMovedAmount * 1.5f;
                //!!temporary debug movement for obj files !!rewrite
                if (state2 == InputState.Press && state != InputState.Press)
                {
                    //code below is checking if the current is selected and moves, transforms or rotates the object
                    if (arrows.wantsToRotateYAxis && loaded)
                        CurrentModel.Transform.rotation.y -= deltaX / 30;
                    if (arrows.wantsToRotateXAxis && loaded)
                        CurrentModel.Transform.rotation.x += (deltaY + deltaX) / 30;
                    if (arrows.wantsToRotateZAxis && loaded)
                        CurrentModel.Transform.rotation.z += (deltaY + deltaX) / 30;

                    if (arrows.wantsToMoveYAxis && loaded)
                        CurrentModel.Transform.translation.y += deltaY / 150;

                    if (arrows.wantsToMoveXAxis && loaded)
                        CurrentModel.Transform.translation.x -= deltaX / 150;

                    if (arrows.wantsToMoveZAxis && loaded)
                        CurrentModel.Transform.translation.z += -deltaX / 150;


                    if (arrows.wantsToScaleYAxis && loaded)
                        CurrentModel.Transform.scale.y -= deltaY / 200;

                    if (arrows.wantsToScaleXAxis && loaded)
                        CurrentModel.Transform.scale.x += deltaX / 200;

                    if (arrows.wantsToScaleZAxis && loaded)
                        CurrentModel.Transform.scale.z += (deltaX + deltaY) / 400;
                }
            }
            if (state != InputState.Press && state2 != InputState.Press)
                Glfw.SetInputMode(CORERenderer.Main.COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
        }

        public static void EnableGLOptions()
        {
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glEnable(GL_DEPTH_TEST);
            glDepthFunc(GL_LEQUAL);

            glEnable(GL_STENCIL_TEST);
            glStencilFunc(GL_NOTEQUAL, 1, 0xFF);
            glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);

            glEnable(GL_DEBUG_OUTPUT);

            glEnable(GL_CULL_FACE);
            //glEnable(GL_TEXTURE_CUBE_MAP_SEAMLESS);
            glCullFace(GL_BACK);
            glFrontFace(GL_CCW);
        }

        

        private static bool enteredFrame = false;

        public static bool IsCursorInFrame(float mouseX, float mouseY)
        { //awfully written
            if (((mouseX >= viewportX) && (mouseX <= monitorWidth - viewportX) && (monitorHeight - mouseY >= viewportY) && (monitorHeight - mouseY <= monitorHeight - 25)))
            {
                if (Glfw.GetMouseButton(window, MouseButton.Right) == InputState.Press)
                    enteredFrame = true;
                return true;
            }
            if (Glfw.GetMouseButton(window, MouseButton.Right) != InputState.Press)
            {
                enteredFrame = false;
                return false;
            }

            if (enteredFrame)
                return true;

            return false;
        }
    }
}