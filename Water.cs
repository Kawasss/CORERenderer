using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.Main;
using COREMath;
using CORERenderer.textures;
using CORERenderer.shaders;

namespace CORERenderer
{
    public class Water
    {
        private VertexBuffer vb;
        private Transform transform;
        private Texture normal1;
        private Texture normal2;
        private float size;
        private int vertexCount = 0;
        public Shader shader = new($"{COREMain.BaseDirectory}\\water.vert", $"{COREMain.BaseDirectory}\\water.frag");
        private Vector3 position;

        public Water(VertexBuffer vb, Transform transform, Texture normal1, Texture normal2)
        {
            this.vb = vb;
            this.transform = transform;
            this.normal1 = normal1;
            this.normal2 = normal2;
        }

        public static Water GenerateWater(float size)
        {
            Readers.LoadOBJ($"{COREMain.BaseDirectory}\\OBJs\\plane.obj", out string _, out List<List<Vertex>> vertices, out _, out Vector3 center, out Vector3 extents, out Vector3 offset);
            VertexBuffer vb = new(vertices[0]);

            Transform t = new(offset, Vector3.Zero, new(size), extents, center);

            Texture normal1 = Texture.ReadFromFile($"{COREMain.BaseDirectory}\\textures\\waterNormal2.jpg");
            Texture normal2 = normal1;

            Water w = new(vb, t, normal1, normal2);
            w.position = offset;
            w.vertexCount = vertices[0].Count;
            w.size = size;
            vb.Bind();
            w.shader.ActivateAttributes();
            w.shader.SetInt("normal1", 0);
            w.shader.SetInt("normal2", 1);
            w.shader.SetInt("reflection", 2);
            w.shader.SetInt("depthMap", 8);
            w.shader.SetVector3("absorbance", new(.8f, 0.06f, 0f));

            return w;
        }
        private float speed = 0;
        public void Render()
        {
            shader.Use();

            shader.SetFloat("time", speed);
            shader.SetVector3("lightPos", COREMain.CurrentScene.lights[0].position);
            //shader.SetVector3("lightPos[1]", COREMain.CurrentScene.lights[1].position);
            shader.SetVector3("viewPos", Rendering.Camera.position);
            shader.SetFloat("farPlane", Rendering.Camera.FarPlane);
            shader.SetMatrix("model", MathC.GetScalingMatrix(size) * MathC.GetTranslationMatrix(position));

            normal1.Use(ActiveTexture.Texture0);
            normal2.Use(ActiveTexture.Texture1);

            Rendering.reflectionCubemap.Use(GL.GL_TEXTURE2);

            vb.Draw(PrimitiveType.Triangles, 0, vertexCount);
            speed += 0.1f;
        }
    }
}
