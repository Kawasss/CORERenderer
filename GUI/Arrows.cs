using CORERenderer.Loaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public class Arrows
    {
        private uint VBO, VAO, EBO;

        private List<List<float>> vertices;
        private List<List<uint>> indices;

        private float maxScale = 0;
 
        public Arrows()
        {
            Readers.LoadOBJ($"{COREMain.pathRenderer}\\Loaders\\testOBJ\\arrow.obj", out _, out vertices, out indices, out _);

            GenerateFilledBuffer(out VBO, out VAO, vertices[0].ToArray());

            GenericShaders.arrowShader.Use();

            int vertexLocation = GenericShaders.arrowShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            //UV texture coordinates
            vertexLocation = GenericShaders.arrowShader.GetAttribLocation("aTexCoords");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float))); }
            glEnableVertexAttribArray((uint)vertexLocation);

            //normal coordinates
            vertexLocation = GenericShaders.arrowShader.GetAttribLocation("aNormal");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float))); }
            glEnableVertexAttribArray((uint)vertexLocation);

            GenerateFilledBuffer(out EBO, indices[0].ToArray());
            if (COREMain.scenes[COREMain.selectedScene].currentObj == -1)
                return;

            maxScale = MathC.GetLengthOf(COREMain.scenes[COREMain.selectedScene].camera.position - COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].translation[0]);
        }

        public void Render()
        {
            if (COREMain.scenes[COREMain.selectedScene].currentObj == -1)
                return;

            if (maxScale == 0)
                maxScale = MathC.GetLengthOf((COREMain.scenes[COREMain.selectedScene].camera.position - COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].translation[0]));

            GenericShaders.arrowShader.Use();

            Matrix model = Matrix.IdentityMatrix;

            //model matrix to place the arrows at the coordinates of the selected object, model * place of object * normalized size (to make the arrows always the same size)
            model *= Matrix.IdentityMatrix * MathC.GetTranslationMatrix(COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].translation[0]) * MathC.GetScalingMatrix((MathC.GetLengthOf(COREMain.scenes[COREMain.selectedScene].camera.position - COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].translation[0]) / maxScale) * 0.75f);

            GenericShaders.arrowShader.SetVector3("color", 0, 1, 0);

            glBindVertexArray(VAO);
            for (int i = 0; i < 3; i++)
            {
                Matrix local = model;
                if (i == 0)
                    local *= MathC.GetRotationXMatrix(90);
                if (i == 1)
                {
                    GenericShaders.arrowShader.SetVector3("color", 1, 0, 0);
                    local *= MathC.GetRotationYMatrix(-90);
                }
                else if (i == 2)
                {
                    GenericShaders.arrowShader.SetVector3("color", 0, 0, 1);
                    local *= MathC.GetRotationYMatrix(180);
                }

                GenericShaders.arrowShader.SetMatrix("model", local);
                unsafe { glDrawElements(PrimitiveType.Triangles, indices[0].Count, GLType.UnsingedInt, (void*)0); }
            }
        }
    }
}
