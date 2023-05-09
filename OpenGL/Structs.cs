using COREMath;
using CORERenderer.Fonts;
using CORERenderer.Loaders;
using CORERenderer.Main;
using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.OpenGL
{
    public class Vertex //class so that it gets passed by reference
    {
        public float x = 0;
        public float y = 0;
        public float z = 0;

        public float uvX = 0;
        public float uvY = 0;

        public float normalX = 0;
        public float normalY = 0;
        public float normalZ = 0;

        public int[] boneIDs = new int[8] { -1, -1, -1, -1, -1, -1, -1, -1 };
        public float[] boneWeights = new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

        public Vertex() { }

        public void AddBone(int ID)
        {
            for (int i = 0; i < boneIDs.Length; i++)
                if (boneIDs[i] == -1)
                {
                    boneIDs[i] = ID;
                    for  (int j = 0; j < boneWeights.Length; j++)
                        boneWeights[j] = 1 / (i + 1);
                }
        }

        public static List<List<float>> GetFloatList(List<List<Vertex>> v2)
        {
            List<List<float>> list = new();
            for (int i = 0; i < v2.Count; i++)
                list.Add(GetFloatList(v2[i]));
            return list;
        }

        /// <summary>
        /// !!THIS MAY NOT INCLUDE CORRECT BONES
        /// </summary>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static List<float> GetFloatList(List<Vertex> v2)
        {
            List<float> list = new();
            for (int i = 0; i < v2.Count; i++)
            {
                list.Add(v2[i].x);
                list.Add(v2[i].y);
                list.Add(v2[i].z);

                list.Add(v2[i].uvX);
                list.Add(v2[i].uvY);

                list.Add(v2[i].normalX);
                list.Add(v2[i].normalY);
                list.Add(v2[i].normalZ);

                foreach (int ID in v2[i].boneIDs)
                    list.Add(ID);
                foreach (float weight in v2[i].boneWeights)
                    list.Add(weight);
            }
            return list;
        }

        public static List<List<Vertex>> GetVertices(List<List<float>> l)
        {
            List<List<Vertex>> l2 = new();
            foreach (List<float> l3 in l)
                l2.Add(GetVertices(l3));
            return l2;
        }

        public static List<Vertex> GetVertices(List<float> l)
        {
            List<Vertex> v = new();
            for (int i = 0; i < l.Count; i += 8)
            {
                v.Add(new() { x = l[i], y = l[i + 1], z = l[i + 2], uvX = l[i + 3], uvY = l[i + 4], normalX = l[i + 5], normalY = l[i + 6], normalZ = l[i + 7] });
            }
            return v;
        }
    }

    public struct Plane
    {
        public Vector3 normal = Vector3.UnitVectorY;
        public float distance = 0;

        public Plane(Vector3 point, Vector3 normal)
        {
            this.normal = MathC.Normalize(normal);
            this.distance = MathC.GetDotProductOf(normal, point);
        }

        public float GetDistanceToPlane(Vector3 point) => MathC.GetDotProductOf(normal, point) - distance;
    }

    public struct Frustum
    {
        public Plane topFace;
        public Plane bottomFace;
        public Plane rightFace;
        public Plane leftFace;
        public Plane farFace;
        public Plane nearFace;

        public Frustum(Camera camera)
        {
            float halfWidth = camera.FarPlane * MathC.Tan(camera.Fov * 0.5f);
            float halfHeight = halfWidth * camera.AspectRatio;
            Vector3 positionFarPlane = camera.FarPlane * camera.front;

            nearFace = new(camera.position + camera.NearPlane * camera.front, camera.front);
            farFace = new(camera.position + positionFarPlane, -camera.front);
            rightFace = new(camera.position, MathC.GetCrossProduct(positionFarPlane - camera.right * halfHeight, camera.up));
            leftFace = new(camera.position, MathC.GetCrossProduct(camera.up, positionFarPlane + camera.right * halfHeight));
            topFace = new(camera.position, MathC.GetCrossProduct(camera.right, positionFarPlane - camera.up * halfWidth));
            bottomFace = new(camera.position, MathC.GetCrossProduct(positionFarPlane + camera.up * halfWidth, camera.right));
        }
    }

    public interface IBoundingVolume
    {
        public virtual bool IsInFrustum(Frustum frustum, Transform transform) { throw new NotImplementedException("Method \"IsInFrustum\" is not implemented"); }
        public virtual bool IsInForwardPlane(Plane plane) { throw new NotImplementedException("Method \"IsInForwardPlane\" is not implemented"); }
        bool isInFrustum(Frustum frustum)
        {
            return
                IsInForwardPlane(frustum.leftFace) &&
                IsInForwardPlane(frustum.rightFace) &&
                IsInForwardPlane(frustum.topFace) &&
                IsInForwardPlane(frustum.bottomFace) &&
                IsInForwardPlane(frustum.nearFace) &&
                IsInForwardPlane(frustum.farFace);
        }
}

    public struct AABB : IBoundingVolume
    {
        public Vector3 center = Vector3.Zero;
        public Vector3 extents = Vector3.Zero;
        public Vector3 min = Vector3.Zero, max = Vector3.Zero;

        public AABB(Vector3 min, Vector3 max)
        {
            this.min = min;
            this.max = max;
            this.center = (min + max) * 0.5f;
            this.extents = max - center;
        }

        public AABB(Vector3 center, float iI, float iJ, float iK)
        {
            this.center = center;
            this.extents = new(iI, iJ, iK);

            this.max = extents + center;
            this.min = 2 * center - max;
        }

        public bool IsInForwardPlane(Plane plane)
        {
            float r = extents.x * MathC.Abs(plane.normal.x) + extents.y * MathC.Abs(plane.normal.y) + extents.z * MathC.Abs(plane.normal.z);
            return -r <= plane.GetDistanceToPlane(center);
        }

        public bool IsInFrustum(Frustum frustum, Transform transform)
        {
            AABB aabb = GetNewAABB(transform);

            return
                aabb.IsInForwardPlane(frustum.leftFace) &&
                aabb.IsInForwardPlane(frustum.rightFace) &&
                aabb.IsInForwardPlane(frustum.topFace) &&
                aabb.IsInForwardPlane(frustum.bottomFace) &&
                aabb.IsInForwardPlane(frustum.nearFace) &&
                aabb.IsInForwardPlane(frustum.farFace);
        }

        public AABB GetNewAABB(Transform transform)
        {
            Vector3 globalCenter = (new Vector4(center, 1) * transform.ModelMatrix).xyz;
            Vector3 right = transform.Right * extents.x;
            Vector3 up = transform.Up * extents.y;
            Vector3 forward = transform.Forward * extents.z;

            float Ii = MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorX, right)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorX, up)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorX, forward));
            float Ij = MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorY, right)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorY, up)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorY, forward));
            float Ik = MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorZ, right)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorZ, up)) + MathC.Abs(MathC.GetDotProductOf(Vector3.UnitVectorZ, forward));

            return new(globalCenter, Ii, Ij, Ik);
        }
    }

    public struct Framebuffer
    {
        public uint FBO; //FrameBufferObject
        public uint VAO; //VertexArrayObject
        public uint Texture;
        public uint RBO; //RenderBufferObject
        public Shader shader;

        public int width, height;

        public uint VBO; //VBO isnt really needed, but just in case

        public void Bind() => glBindFramebuffer(this);

        public void RenderFramebuffer()
        {
            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE_2D, 0);

            glBindFramebuffer(GL_FRAMEBUFFER, 0);
            glClear(GL_DEPTH_BUFFER_BIT);
            glDisable(GL_DEPTH_TEST);

            glClearColor(1, 1, 1, 1);

            this.shader.Use();

            glBindVertexArray(this.VAO);
            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_2D, this.Texture);

            glDrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }
}