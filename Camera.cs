using COREMath;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW;
using CORERenderer.Main;
using SharpFont;
using CORERenderer.OpenGL;

namespace CORERenderer
{
    public class Camera
    {


        private float pitch;
        private float yaw = -(MathC.PiF / 2);
        private float fov = MathC.PiF / 2;

        private static float cameraSpeed = 3f;
        private const float SENSITIVITY = 0.1f;

        private bool firstMove = true;

        private Vector2 lastPos;

        public float AspectRatio;

        public Vector3 position;

        public Vector3 front = new(0, 0, -1);

        public Vector3 up = Vector3.UnitVectorY;
        public Vector3 right = Vector3.UnitVectorX;

        public Camera(Vector3 Position, float aspectRatio)
        {
            position = Position;
            AspectRatio = aspectRatio;
        }

        public float Pitch
        {
            get => MathC.RadToDeg(pitch);
            set
            {
                float angle = MathC.Clamp(value, -89, 89);
                pitch = MathC.DegToRad(angle);
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathC.RadToDeg(yaw);
            set
            {
                yaw = MathC.DegToRad(value);
                UpdateVectors();
            }
        }

        public float Fov
        {
            get => MathC.RadToDeg(fov);
            set
            {
                float angle = MathC.Clamp(value, 1, 90);
                fov = MathC.DegToRad(angle);
            }
        }

        public void SetPosition(float x, float y, float z)
        {
            position = new Vector3(x, y, z);
        }

        public Matrix GetProjectionMatrix()
        {
            return Matrix.CreatePerspectiveFOV(fov, AspectRatio, 0.1f, 5000f);
        }

        public Matrix GetOrthographicProjectionMatrix()
        {
            return Matrix.CreateOrthographicOffCenter(-COREMain.Width / 3, COREMain.Width / 3, -COREMain.Height / 3, COREMain.Height / 3, -10000, 10000);
        }

        public Matrix GetViewMatrix()
        {
            return MathC.LookAt(position, position + front, up);
        }

        public Matrix GetTranslationlessViewMatrix()
        {
            Matrix temp = MathC.LookAt(position, position + front, up);
            Matrix newtemp = new(new float[4, 4] { {temp.matrix4x4[0,0], temp.matrix4x4[0, 1], temp.matrix4x4[0, 2], 0 },
                                                   {temp.matrix4x4[1,0], temp.matrix4x4[1, 1], temp.matrix4x4[1, 2], 0 },
                                                   {temp.matrix4x4[2,0], temp.matrix4x4[2, 1], temp.matrix4x4[2, 2], 0 },
                                                   {0                  , 0                   , 0                   , 0 }});
            return newtemp;
        }

        private void UpdateVectors()
        {
            front.x = MathC.Cos(pitch) * MathC.Cos(yaw);
            front.y = MathC.Sin(pitch);
            front.z = MathC.Cos(pitch) * MathC.Sin(yaw);

            front = MathC.Normalize(front);

            right = MathC.Normalize(MathC.GetCrossProduct(front, Vector3.UnitVectorY));
            up = MathC.Normalize(MathC.GetCrossProduct(right, front));
        }

        public void Debug()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 4);
            Console.WriteLine($"pitch: {pitch}        " +
                $"\n yaw: {yaw}        " +
                $"\n position: ({position.x}, {position.y}, {position.z})         " +
                $"\n front: ({front.x}, {front.y}, {front.z})        ");
        }

        public void UpdatePosition(float mousePosX, float mousePosY, float delta)
        {
            Glfw.SetInputMode(COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

            if (Glfw.GetKey(COREMain.window, Keys.LeftControl) == InputState.Press)
                cameraSpeed = 60;
            else
                cameraSpeed = 3;

            if (Glfw.GetKey(COREMain.window, Keys.W) == InputState.Press)
                position += front * (cameraSpeed * delta);

            if (Glfw.GetKey(COREMain.window, Keys.S) == InputState.Press)
                position -= front * (cameraSpeed * delta);

            if (Glfw.GetKey(COREMain.window, Keys.A) == InputState.Press)
                position -= right * (cameraSpeed * delta);

            if (Glfw.GetKey(COREMain.window, Keys.D) == InputState.Press)
                position += right * (cameraSpeed * delta);

            if (Glfw.GetKey(COREMain.window, Keys.Space) == InputState.Press)
                position += up * (cameraSpeed * delta);

            if (Glfw.GetKey(COREMain.window, Keys.LeftShift) == InputState.Press)
                position -= up * (cameraSpeed * delta);
            
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

                Yaw += deltaX * SENSITIVITY;
                Pitch += deltaY * SENSITIVITY;
            }
        }
    }
}
