﻿using COREMath;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.Main.Globals;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Main;
using CORERenderer.shaders;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW.Enums;
using CORERenderer.Loaders;

namespace CORERenderer
{
    public class Scene : Overrides
    {
        public Camera camera;

        public List<Model> allModels;

        public bool loaded = false;

        public int currentObj = -1;

        public override void OnLoad()
        {
            allModels = new();
            camera = new(new(0, 1, 5), (float)renderWidth / (float)renderHeight);

            if (LoadFile == RenderMode.CRSFile) //CRS disabled cause it needs to be reworked
            {

            }
            else
            {
                loaded = true;
                allModels.Add(new(LoadFilePath));
                currentObj = 0;
            }
            //allModels.Add(new($"{pathRenderer}\\textures\\hdr\\newport_loft.hdr"));
        }

        public override void RenderEveryFrame(float delta)
        {
            RenderAllModels();
        }

        public override void EveryFrame(Window window, float delta)
        {
            if (Glfw.GetKey(window, Keys.Escape) == InputState.Press)
            {
                Glfw.SetWindowShouldClose(window, true);
                Console.WriteLine("Window closed");
            }

            InputState state = Glfw.GetMouseButton(window, MouseButton.Middle);

            InputState state2 = Glfw.GetMouseButton(window, MouseButton.Left);

            if (IsCursorInFrame(mousePosX, mousePosY))
            {
                //!!temporary debug movement for obj files !!rewrite
                if (state2 == InputState.Press && state != InputState.Press)
                {
                    //code below is checking if the current is selected and moves, transforms or rotates the object
                    if (Glfw.GetKey(window, Keys.Delete) == InputState.Press && loaded)
                        allModels[currentObj].rotation[allModels[currentObj].selectedSubmodel].x += 15f * delta;
                    if (Glfw.GetKey(window, Keys.End) == InputState.Press && loaded)
                            allModels[currentObj].rotation[allModels[currentObj].selectedSubmodel].y += 15f * delta;
                    if (Glfw.GetKey(window, Keys.PageDown) == InputState.Press && loaded)
                            allModels[currentObj].rotation[allModels[currentObj].selectedSubmodel].z += 15f * delta;

                    if (Glfw.GetKey(window, Keys.Minus) == InputState.Press && loaded) //need to support changing all axises
                            allModels[currentObj].Scaling[allModels[currentObj].selectedSubmodel].x -= 2f * delta;
                    if (Glfw.GetKey(window, Keys.Equal) == InputState.Press && loaded)
                            allModels[currentObj].Scaling[allModels[currentObj].selectedSubmodel].x += 2f * delta;


                    if (Glfw.GetKey(window, Keys.Up) == InputState.Press && loaded)
                            allModels[currentObj].translation[allModels[currentObj].selectedSubmodel] += new Vector3(0, 1f * delta, 0);

                    if (Glfw.GetKey(window, Keys.Down) == InputState.Press && loaded)
                            allModels[currentObj].translation[allModels[currentObj].selectedSubmodel] -= new Vector3(0, 1f * delta, 0);

                    if (Glfw.GetKey(window, Keys.Left) == InputState.Press && loaded)
                            allModels[currentObj].translation[allModels[currentObj].selectedSubmodel] -= new Vector3(1f * delta, 0, 0);

                    if (Glfw.GetKey(window, Keys.Right) == InputState.Press && loaded)
                            allModels[currentObj].translation[allModels[currentObj].selectedSubmodel] += new Vector3(1f * delta, 0, 0);
                }
            }
            if (state != InputState.Press && state2 != InputState.Press)
                Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Normal);
        }

        public static void EnableGLOptions()
        {
            glEnable(GL_BLEND);

            glEnable(GL_DEPTH_TEST);
            glDepthFunc(GL_LEQUAL);

            glEnable(GL_STENCIL_TEST);
            glStencilFunc(GL_NOTEQUAL, 1, 0xFF);
            glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);

            glEnable(GL_TEXTURE_2D);
            glEnable(GL_TEXTURE_CUBE_MAP);


            glEnable(GL_DEBUG_OUTPUT);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            glEnable(GL_CULL_FACE);
            glCullFace(GL_BACK);
            glFrontFace(GL_CCW);
        }

        

        private static bool enteredFrame = false;

        public static bool IsCursorInFrame(float mouseX, float mouseY)
        {
            if (((mouseX >= viewportX) && (mouseX <= monitorWidth - viewportX) && (monitorHeight - mouseY >= viewportY) && (monitorHeight - mouseY <= monitorHeight - 25)))
            {
                if (Glfw.GetMouseButton(window, MouseButton.Middle) == InputState.Press)
                    enteredFrame = true;
                return true;
            }
            if (Glfw.GetMouseButton(window, MouseButton.Middle) != InputState.Press)
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