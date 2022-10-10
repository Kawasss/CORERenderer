using COREMath;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORE_Renderer
{
    public class Camera
    {
        
        
        private float pitch;
        private float yaw = -(MathC.PiF / 2);
        private float fov = MathC.PiF / 2;

        public float AspectRatio;

        public Vector3 position; // = new(0, 0, 5);
        //public static Vector3 target = Vector3.Zero;
        //public static Vector3 direction = MathC.Normalize(position.Subtract(target));

        public Vector3 front = new(0, 0, -1);

        public Vector3 up = Vector3.UnitVectorY;
        public Vector3 right = Vector3.UnitVectorX; //MathC.Normalize(MathC.GetCrossProduct(up, direction));

        //static Vector3 cameraUp = MathC.GetCrossProduct(direction, right);

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
                float angle = MathC.Clamp(value, -fov, fov);
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
            return Matrix.CreatePerspectiveFOV(fov, AspectRatio, 0.1f, 100f);
        }

        public Matrix GetViewMatrix()
        {
            return MathC.LookAt(position, position.Add(front), up);
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
    }
}
