using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.OpenGL;

namespace CORERenderer.Main
{
    internal class Callbacks
    {
        internal static void SetCallbacks()
        {
            Glfw.SetScrollCallback(COREMain.window, ScrollCallback);
            Glfw.SetFramebufferSizeCallback(COREMain.window, FramebufferSizeCallBack);
            Glfw.SetKeyCallback(COREMain.window, KeyCallback);
            Glfw.SetMouseButtonCallback(COREMain.window, MouseCallback);
        }

        private static void ScrollCallback(Window window, double x, double y)
        {
            COREMain.scrollWheelMoved = true;
            COREMain.scrollWheelMovedAmount = y;
        }

        private static void KeyCallback(Window window, Keys key, int scancode, InputState action, ModifierKeys mods)
        {   //saves a lot of energy by only updating if input is detected
            COREMain.keyIsPressed = action == InputState.Press;
            COREMain.pressedKey = key;
        }

        private static void MouseCallback(Window window, MouseButton button, InputState state, ModifierKeys modifiers)
        {
            COREMain.pressedButton = button;
            COREMain.mouseIsPressed = state == InputState.Press;
        }

        private static void FramebufferSizeCallBack(Window window, int width, int height)
        {
            GL.glViewport(0, 0, width, height);
            COREMain.monitorWidth = width;
            COREMain.monitorHeight = height;

            COREMain.renderWidth = (int)(width * 0.75f);
            COREMain.renderHeight = (int)(height * 0.727f);

            COREMain.CurrentScene.camera.AspectRatio = (float)COREMain.renderWidth / (float)COREMain.renderHeight;
        }
    }
}
