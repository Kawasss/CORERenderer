using CORERenderer.Main;
using CORERenderer.shaders;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.GLFW;
using CORERenderer.textures;
using CORERenderer.OpenGL;

namespace CORERenderer.GUI
{
    public class TitleBar
    {
        private uint VBOC;
        private uint VAOC;

        private Shader shaderC = new($"{COREMain.pathRenderer}\\shaders\\Font.vert", $"{COREMain.pathRenderer}\\shaders\\Cross.frag");

        private bool isSelected = false;

        private Texture cross;

        public Div div;

        public TitleBar()
        {
            float width = COREMain.Width / 2;
            float height = COREMain.Height / 2;

            div = new(COREMain.monitorWidth, 25, 0, COREMain.monitorHeight - 25);
            div.SetRenderCallBack(render);

            float[] crossVertices = new float[]
            {
                width - 50, height, 0, 0,
                width - 50, height - 25, 0, 1,
                width, height - 25, 1, 1,

                width - 50, height, 0, 0, 
                width, height - 25, 1, 1,
                width, height, 1, 0
            };

            int vertexLocation = GenericShaders.solidColorQuadShader.GetAttribLocation("aPos");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 2 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);
            
            GenericShaders.solidColorQuadShader.SetMatrix("projection", GetOrthograpicProjectionMatrix());
            GenericShaders.solidColorQuadShader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            GenerateFilledBuffer(out VBOC, out VAOC, crossVertices);

            vertexLocation = shaderC.GetAttribLocation("vertex");
            unsafe { glVertexAttribPointer((uint)vertexLocation, 4, GL_FLOAT, false, 4 * sizeof(float), (void*)0); }
            glEnableVertexAttribArray((uint)vertexLocation);

            GenericShaders.solidColorQuadShader.Use();
            GenericShaders.solidColorQuadShader.SetMatrix("projection", GetOrthograpicProjectionMatrix());
            
            shaderC.Use();

            shaderC.SetMatrix("projection", GetOrthograpicProjectionMatrix());
            shaderC.SetInt("Texture", GL_TEXTURE0);
            shaderC.SetBool("isSelected", isSelected);

            cross = Texture.ReadFromFile($"{COREMain.pathRenderer}\\GUI\\exitCross.png");
            cross.Use(GL_TEXTURE0);

            glBindBuffer(GL_ARRAY_BUFFER, 0);
            glBindVertexArray(0);
        }

        public void Render() => div.Render();

        public void render()
        {   
            //draws the X to exit the app
            shaderC.Use();

            glBindVertexArray(VAOC);
            cross.Use(GL_TEXTURE0);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);

        }

        public void RenderStatic() => Render();

        private bool previousState = false;

        public void CheckForUpdate(float mouseX, float mouseY)
        {
            if (!(previousState == isSelected))
            {
                shaderC.Use();

                glBindVertexArray(VAOC);
                cross.Use(GL_TEXTURE0);
                glDrawArrays(PrimitiveType.Triangles, 0, 6);
            }
            previousState = isSelected;
            if (COREMain.monitorHeight - mouseY >= COREMain.monitorHeight - 25 && COREMain.monitorHeight - mouseY <= COREMain.monitorHeight && mouseX >= COREMain.monitorWidth - 50 && mouseX <= COREMain.Width)
            {
                if (Glfw.GetMouseButton(COREMain.window, GLFW.Enums.MouseButton.Left) == GLFW.Enums.InputState.Press)
                    Glfw.SetWindowShouldClose(COREMain.window, true);

                isSelected = true;
                shaderC.SetBool("isSelected", isSelected);
                
                return;
            }
            else if (isSelected)
            {
                shaderC.SetBool("isSelected", false);
                isSelected = false;
            }
        }
    }
}
