using COREMath;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer
{
    public class Camera
    {


        private float pitch;
        private float yaw = -(MathC.PiF / 2);
        private float fov = MathC.PiF / 2;

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
            return Matrix.CreatePerspectiveFOV(fov, AspectRatio, 0.01f, 100f);
        }

        public Matrix GetViewMatrix()
        {
            return MathC.LookAt(position, position + front, up);
        }

        public Matrix GetArcBallViewMatrix()
        {
            return MathC.LookAt(position, position + front, up);
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
    }
}
