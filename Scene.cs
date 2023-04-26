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

namespace CORERenderer
{
    public class Scene : Overrides
    {
        public Camera camera;

        public List<Model> models;
        public List<Light> allLights;

        public bool loaded = false;

        public int currentObj = -1;

        private Vector2 lastPos = null;

        public override void OnLoad(string[] args)
        {
            models = new();
            allLights = new();
            camera = new(new(0, 1, 5), (float)renderWidth / (float)renderHeight);

            if (args.Length != 0)
            {
                if (LoadFile != RenderMode.CRSFile)
                {
                    loaded = true;
                    models.Add(new(args[0]));
                    currentObj = 0;
                }
                else
                {
                    Readers.LoadCRS(args[0], out models, out _);
                    currentObj = models.Count > 0 ? 0 : -1;
                }
                    
            }
            for (int i = 0; i < models.Count; i++)
                if (models[i].terminate)
                {
                    console.WriteError($"Couldn't create model: {models[i].error}");
                    models.RemoveAt(i);
                }
        }

        public override void RenderEveryFrame(float delta)
        {
            RenderAllModels(models);
        }

        public override void EveryFrame(Window window, float delta)
        {
            loaded = models.Count > 0;
            
            for (int i = 0; i < models.Count; i++) //inefficient, better to have a dictionary to look it up
            {
                if (selectedID == models[i].ID)
                {
                    models[i].highlighted = true;
                    currentObj = i;
                }
            }

            if (loaded && models[^1].terminate)
            {
                console.WriteError($"Couldn't create model: {models[^1].error}");
                models.RemoveAt(models.Count - 1);
            }

            if (shaderConfig == ShaderType.PathTracing)
            {
                GenericShaders.GenericLighting.SetVector3("RAY.origin", CurrentScene.camera.position);
                GenericShaders.GenericLighting.SetVector3("RAY.direction", CurrentScene.camera.front);
                GenericShaders.GenericLighting.SetInt("isReflective", 0);
                GenericShaders.GenericLighting.SetVector3("emission", new(1, 1, 1));
                GenericShaders.GenericLighting.SetVector3("lights.color", new(1, 1, 1));
                GenericShaders.GenericLighting.SetVector3("lights.position", new(0, 1, 1));
            }
            else if (shaderConfig == ShaderType.Lighting)
            {
                GenericShaders.GenericLighting.SetVector3("viewPos", CurrentScene.camera.position);
                GenericShaders.GenericLighting.SetVector3("pointLights[0].position", CurrentScene.camera.position);
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
                //!!temporary debug movement for obj files !!rewrite
                if (state2 == InputState.Press && state != InputState.Press)
                {
                    //code below is checking if the current is selected and moves, transforms or rotates the object
                    if (arrows.wantsToRotateYAxis && loaded)
                        CurrentModel.rotation.y -= deltaX / 30;
                    if (arrows.wantsToRotateXAxis && loaded)
                        CurrentModel.rotation.x += deltaX / 30;
                    if (arrows.wantsToRotateZAxis && loaded)
                        CurrentModel.rotation.z += -deltaX / 30;

                    if (arrows.wantsToMoveYAxis && loaded)
                        CurrentModel.translation.y += deltaY / 150;

                    if (arrows.wantsToMoveXAxis && loaded)
                        CurrentModel.translation.x -= deltaX / 150;

                    if (arrows.wantsToMoveZAxis && loaded)
                        CurrentModel.translation.z += -deltaX / 150;


                    if (arrows.wantsToScaleYAxis && loaded)
                        CurrentModel.scaling.y -= deltaY / 200;

                    if (arrows.wantsToScaleXAxis && loaded)
                        CurrentModel.scaling.x += deltaX / 200;

                    if (arrows.wantsToScaleZAxis && loaded)
                        CurrentModel.scaling.z += (deltaX + deltaY) / 400;
                }
            }
            if (state != InputState.Press && state2 != InputState.Press)
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
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