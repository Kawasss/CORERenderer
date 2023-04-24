using CORERenderer.Loaders;
using CORERenderer.Main;
using static CORERenderer.OpenGL.Rendering;
using static CORERenderer.OpenGL.GL;
using COREMath;
using CORERenderer.OpenGL;
using CORERenderer.shaders;
using CORERenderer.GLFW.Enums;
using CORERenderer.GLFW.Structs;
using CORERenderer.GLFW;

namespace CORERenderer.GUI
{
    public class Arrows
    {
        private uint VBO, VAO, EBO;

        private List<List<float>> vertices;
        private List<List<uint>> indices;

        private float maxScale = 0;

        public int pickedID;

        private Shader pickShader = GenericShaders.IDPicking;
        private Shader shader = GenericShaders.Arrow;

        private Vector3[] rgbs = new Vector3[3];

        public bool wantsToMoveXAxis = false;
        public bool wantsToMoveYAxis = false;
        public bool wantsToMoveZAxis = false;

        public static bool disableArrows = false;

        public Arrows()
        {
            Readers.LoadOBJ($"{COREMain.pathRenderer}\\OBJs\\arrow.obj", out _, out vertices, out indices, out _, out _);

            GenerateFilledBuffer(out VBO, out VAO, vertices[0].ToArray());

            shader.Use();

            int vertexLocation = shader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            //UV texture coordinates
            vertexLocation = shader.GetAttribLocation("aTexCoords");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 8 * sizeof(float), (void*)(3 * sizeof(float))); }
            glEnableVertexAttribArray((uint)vertexLocation);

            //normal coordinates
            vertexLocation = shader.GetAttribLocation("aNormal");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 3, GL_FLOAT, false, 8 * sizeof(float), (void*)(5 * sizeof(float))); }
            glEnableVertexAttribArray((uint)vertexLocation);

            GenerateFilledBuffer(out EBO, indices[0].ToArray());

            for (int i = 0; i < 3; i++)
                rgbs[i] = COREMain.GenerateIDColor(i);
                
            if (COREMain.scenes[COREMain.selectedScene].currentObj == -1)
                return;

            maxScale = 0.6f;//MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.translation);
        }

        public void Render()
        {
            if (disableArrows)
                return;

            if (COREMain.CurrentScene.currentObj == -1)
                return;

            if (maxScale == 0)
                maxScale = (MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.translation)) / 2;

            shader.Use();

            Matrix model = Matrix.IdentityMatrix;

            //model matrix to place the arrows at the coordinates of the selected object, model * place of object * normalized size (to make the arrows always the same size)
            model *= Matrix.IdentityMatrix * MathC.GetTranslationMatrix(COREMain.CurrentModel.translation) * MathC.GetScalingMatrix((MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.translation) / maxScale) * 0.75f);

            shader.SetVector3("color", 0, 1, 0);

            glBindVertexArray(VAO);
            for (int i = 0; i < 3; i++)
            {
                Matrix local = model;
                if (i == 0)
                    local *= MathC.GetRotationXMatrix(90);
                if (i == 1)
                {
                    shader.SetVector3("color", 1, 0, 0);
                    local *= MathC.GetRotationYMatrix(-90);
                }
                else if (i == 2)
                {
                    shader.SetVector3("color", 0, 0, 1);
                    local *= MathC.GetRotationYMatrix(180);
                }

                shader.SetMatrix("model", local);
                unsafe { glDrawElements(PrimitiveType.Triangles, indices[0].Count, GLType.UnsingedInt, (void*)0); }
            }
            RenderIDVersion();        
        }

        private void RenderIDVersion()
        {
            COREMain.IDFramebuffer.Bind();

            glClear(GL_DEPTH_BUFFER_BIT);

            if (maxScale == 0)
                maxScale = (MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.translation)) / 2;

            Matrix model = Matrix.IdentityMatrix;

            //model matrix to place the arrows at the coordinates of the selected object, model * place of object * normalized size (to make the arrows always the same size)
            model *= Matrix.IdentityMatrix * MathC.GetTranslationMatrix(COREMain.CurrentModel.translation) * MathC.GetScalingMatrix((MathC.GetLengthOf(COREMain.CurrentScene.camera.position - COREMain.CurrentModel.translation) / maxScale) * 0.75f);//model *= Matrix.IdentityMatrix * MathC.GetTranslationMatrix(COREMain.CurrentModel.translation);

            glBindVertexArray(VAO);
            for (int i = 0; i < 3; i++)
            {
                pickShader.Use();
                Matrix local = model;
                if (i == 0)
                {
                    pickShader.SetVector3("color", rgbs[0]);
                    local *= MathC.GetRotationXMatrix(90);
                }
                if (i == 1)
                {
                    pickShader.SetVector3("color", rgbs[1]);
                    local *= MathC.GetRotationYMatrix(-90);
                }
                else if (i == 2)
                {
                    pickShader.SetVector3("color", rgbs[2]);
                    local *= MathC.GetRotationYMatrix(180);
                }

                pickShader.SetMatrix("model", local);
                unsafe { glDrawElements(PrimitiveType.Triangles, indices[0].Count, GLType.UnsingedInt, (void*)0); }
            }
            COREMain.renderFramebuffer.Bind();
        }

        public void UpdateArrowsMovement()
        {
            if (COREMain.selectedID == 0 && wantsToMoveXAxis == false && wantsToMoveZAxis == false)
                wantsToMoveYAxis = true;
            else if (Glfw.GetMouseButton(COREMain.window, MouseButton.Left) != InputState.Press)
                wantsToMoveYAxis = false;
            if (COREMain.selectedID == 1 && wantsToMoveYAxis == false && wantsToMoveZAxis == false)
                wantsToMoveXAxis = true;
            else if (Glfw.GetMouseButton(COREMain.window, MouseButton.Left) != InputState.Press)
                wantsToMoveXAxis = false;
            if (COREMain.selectedID == 2 && wantsToMoveXAxis == false && wantsToMoveYAxis == false)
                wantsToMoveZAxis = true;
            else if (Glfw.GetMouseButton(COREMain.window, MouseButton.Left) != InputState.Press)
                wantsToMoveZAxis = false;
        }
    }
}