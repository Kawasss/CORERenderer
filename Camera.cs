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

        private float nearPlane = 0.1f, farPlane = 5000;

        public static float cameraSpeed = 3f;
        private const float SENSITIVITY = 0.1f;

        private bool firstMove = true;

        private Vector2 lastPos;

        public float AspectRatio;

        public Vector3 position;

        public Vector3 front = new(0, 0, -1);

        public Vector3 up = Vector3.UnitVectorY;
        public Vector3 right = Vector3.UnitVectorX;

        public Frustum Frustum { get { return GenerateFrustum(); } }

        public Camera(Vector3 Position, float aspectRatio)
        {
            position = Position;
            AspectRatio = aspectRatio;
        }

        public float NearPlane
        {
            get => nearPlane;
            set
            {
                if (value <= 0)
                    value = 1;
                nearPlane = value;
            }
        }

        public float FarPlane
        {
            get => farPlane;
            set
            {
                if (value <= 0)
                    value = 1;
                farPlane = value;
            }
        }

        public float Pitch
        {
            get => MathC.RadToDeg(pitch);
            set
            {
                float angle = MathC.Clamp(value, -90, 90);
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

        public Matrix TranslationlessViewMatrix
        {
            get
            {
                Matrix temp = MathC.LookAt(position, position + front, up);
                temp.matrix4x4[3, 3] = 0;
                return temp;
            }
        }

        public Matrix ProjectionMatrix { get => Matrix.CreatePerspectiveFOV(fov, AspectRatio, nearPlane, farPlane); }

        public Matrix OrthographicProjectionMatrix { get => Matrix.CreateOrthographicOffCenter(-Main.COREMain.Width / 9, Main.COREMain.Width / 9, -Main.COREMain.Height / 9, Main.COREMain.Height / 9, -farPlane, farPlane); }

        public Matrix ViewMatrix { get => MathC.LookAt(position, position + front, up); }

        private Frustum GenerateFrustum()
        {
            Frustum frustum;
            float halfWidth = farPlane * MathC.Tan(fov * 0.5f);
            float halfHeight = halfWidth * AspectRatio;
            Vector3 positionFarPlane = farPlane * front;

            frustum.nearFace = new(position + nearPlane * front, front);
            frustum.farFace = new(position + positionFarPlane, -front);
            frustum.rightFace = new(position, MathC.GetCrossProduct(positionFarPlane - right * halfHeight, up));
            frustum.leftFace = new(position, MathC.GetCrossProduct(up, positionFarPlane + right * halfHeight));
            frustum.topFace = new(position, MathC.GetCrossProduct(right, positionFarPlane - up * halfWidth));
            frustum.bottomFace = new(position, MathC.GetCrossProduct(positionFarPlane + up * halfWidth, right));

            return frustum;
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
            Glfw.SetInputMode(Main.COREMain.window, InputMode.Cursor, (int)CursorMode.Disabled);

            if (Glfw.GetKey(Main.COREMain.window, Keys.W) == InputState.Press)
                position += front * (cameraSpeed * delta);

            if (Glfw.GetKey(Main.COREMain.window, Keys.S) == InputState.Press)
                position -= front * (cameraSpeed * delta);

            if (Glfw.GetKey(Main.COREMain.window, Keys.A) == InputState.Press)
                position -= right * (cameraSpeed * delta);

            if (Glfw.GetKey(Main.COREMain.window, Keys.D) == InputState.Press)
                position += right * (cameraSpeed * delta);

            if (Glfw.GetKey(Main.COREMain.window, Keys.Space) == InputState.Press)
                position += up * (cameraSpeed * delta);

            if (Glfw.GetKey(Main.COREMain.window, Keys.LeftShift) == InputState.Press)
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
