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
            Readers.LoadOBJ($"{COREMain.pathRenderer}\\OBJs\\sphere.obj", out _, out List<List<Vertex>> vertices, out _, out Vector3 center, out Vector3 extents);
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

            GenericShaders.NormalVisualisation.Use();

            GenericShaders.NormalVisualisation.ActivateAttributes();
        }

        public void Render()
        {
            if (CanBeCulled)
                return;

            shader.Use();

            shader.SetVector3("viewPos", COREMain.CurrentScene.camera.position);
            shader.SetVector3("lightPos", new(1, 2, 1));
            shader.SetMatrix("model", Matrix.IdentityMatrix);

            material.albedo.Use(GL_TEXTURE0);
            material.normal.Use(GL_TEXTURE1);
            material.metallic.Use(GL_TEXTURE2);
            material.roughness.Use(GL_TEXTURE3);
            material.AO.Use(GL_TEXTURE4);

            glBindVertexArray(VAO);
            Rendering.glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count);

            GenericShaders.NormalVisualisation.Use();
            GenericShaders.NormalVisualisation.SetMatrix("model", Matrix.IdentityMatrix);
            Rendering.glDrawArrays(PrimitiveType.Triangles, 0, vertices.Count);
        }
    }
}
