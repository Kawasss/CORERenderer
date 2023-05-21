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

        private Shader shaderC = new($"{Main.COREMain.BaseDirectory}\\shaders\\Font.vert", $"{Main.COREMain.BaseDirectory}\\shaders\\Cross.frag");

        private bool isSelected = false;

        private Texture cross;

        public Div div;

        private Shader shader = GenericShaders.Quad;

        public TitleBar()
        {
            float width = Main.COREMain.Width / 2;
            float height = Main.COREMain.Height / 2;

            div = new(Main.COREMain.monitorWidth, 25, 0, Main.COREMain.monitorHeight - 25);
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

            shader.ActivateAttributes();

            shader.SetMatrix("projection", GetOrthograpicProjectionMatrix(Main.COREMain.Width, Main.COREMain.Height));
            shader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            GenerateFilledBuffer(out VBOC, out VAOC, crossVertices);

            shaderC.ActivateAttributes();

            shader.Use();
            shader.SetMatrix("projection", GetOrthograpicProjectionMatrix(Main.COREMain.Width, Main.COREMain.Height));
            
            shaderC.Use();

            shaderC.SetMatrix("projection", GetOrthograpicProjectionMatrix(Main.COREMain.Width, Main.COREMain.Height));
            shaderC.SetInt("Texture", 0);
            shaderC.SetBool("isSelected", isSelected);

            cross = Texture.ReadFromFile($"{Main.COREMain.BaseDirectory}\\GUI\\exitCross.png");
            cross.Use(ActiveTexture.Texture0);

            glBindBuffer(BufferTarget.ArrayBuffer, 0);
            glBindVertexArray(0);
        }

        public void Render() => div.Render();

        public void render()
        {   
            //draws the X to exit the app
            shaderC.Use();

            glBindVertexArray(VAOC);
            cross.Use(ActiveTexture.Texture0);
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
                cross.Use(ActiveTexture.Texture0);
                glDrawArrays(PrimitiveType.Triangles, 0, 6);
            }
            previousState = isSelected;
            if (Main.COREMain.monitorHeight - mouseY >= Main.COREMain.monitorHeight - 25 && Main.COREMain.monitorHeight - mouseY <= Main.COREMain.monitorHeight && mouseX >= Main.COREMain.monitorWidth - 50 && mouseX <= Main.COREMain.Width)
            {
                if (Glfw.GetMouseButton(Main.COREMain.window, GLFW.Enums.MouseButton.Left) == GLFW.Enums.InputState.Press)
                    Glfw.SetWindowShouldClose(Main.COREMain.window, true);

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