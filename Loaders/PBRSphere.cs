using COREMath;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.Loaders
{
    public class PBRSphere
    {
        public PBRMaterial material;
        private List<Vertex> vertices;
        private Transform transform;
        public bool CanBeCulled { get => !transform.BoundingBox.IsInFrustum(Rendering.Camera.Frustum, transform); }

        private uint VBO, VAO;
        private readonly Shader shader = GenericShaders.PBR;

        public PBRSphere(PBRMaterial material)
        {
            this.material = material;
            Readers.LoadOBJ($"{COREMain.BaseDirectory}\\OBJs\\sphere.obj", out _, out List<List<Vertex>> vertices, out _, out Vector3 center, out Vector3 extents);
            this.transform = new(Vector3.Zero, Vector3.Zero, new(1, 1, 1), extents, center);
            this.vertices = vertices[0];

            Rendering.GenerateFilledBuffer(out VBO, out VAO, vertices[0].ToArray());

            shader.ActivateAttributes();

            shader.Use();
            shader.SetInt("albedoMap", 0);
            shader.SetInt("normalMap", 1);
            shader.SetInt("metallicMap", 2);
            shader.SetInt("roughnessMap", 3);
            shader.SetInt("aoMap", 4);
            shader.SetInt("heightMap", 5);
        }

        public void Render()
        {
            if (CanBeCulled)
                return;

            shader.Use();

            shader.SetVector3("viewPos", Rendering.Camera.position);
            shader.SetVector3("lightPos", new(1, 2, 1));
            shader.SetMatrix("model", Matrix.IdentityMatrix);

            material.albedo.Use(ActiveTexture.Texture0);
            material.normal.Use(ActiveTexture.Texture1);
            material.metallic.Use(ActiveTexture.Texture2);
            material.roughness.Use(ActiveTexture.Texture3);
            material.AO.Use(ActiveTexture.Texture4);
            material.height.Use(ActiveTexture.Texture5);

            glBindVertexArray(VAO);
            Rendering.glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
        }
    }
}
