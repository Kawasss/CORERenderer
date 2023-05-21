using CORERenderer.Main;
using CORERenderer.OpenGL;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.shaders;

namespace CORERenderer.GUI
{
    public class Submenu
    {
        private Div div;

        public int width, height, x, y;

        private int buttonWidth, buttonHeight;

        private string[] list;

        private bool[] changedValue;

        private Dictionary<string, bool> isOptionTrue = new();

        public bool isAttached = false;

        public static bool isOpen = false;

        private Shader shader = GenericShaders.Quad;

        public Submenu(string[] options)
        {
            list = options;

            changedValue = new bool[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                isOptionTrue.Add(options[i], false);
                changedValue[i] = false;
            }

            width = (int)(Main.COREMain.monitorWidth * 0.075f);
            height = (Main.COREMain.debugText.characterHeight + 5) * options.Length;

        }

        public void SetBool(string optionName, bool option)
        {
            isOptionTrue[optionName] = option;
        }

        public void AttachTo(ref Button button)
        {
            button.attachedSubmenu = this;

            x = button.x;
            y = button.y;

            buttonWidth = button.width;
            buttonHeight = button.height;

            div = new(width, height, x, y - height);
            
            isAttached = true;
        }

        public void Render()
        {
            if (!isOpen)
                return;

            if (Main.COREMain.secondPassed && !isAttached)
            {
                Console.WriteError("Submenu not attached");
                return;
            }

            shader.SetVector3("color", 0.13f, 0.13f, 0.13f);
            div.Render();
            shader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            int offset = height - (int)(Main.COREMain.debugText.characterHeight * 0.7f) - 3;
            for (int i = 0; i < list.Length; i++, offset -= (int)(Main.COREMain.debugText.characterHeight * 0.7f) + 3)
            {
                if (Main.COREMain.CheckAABBCollisionWithClick(x, y - (i + 1) * (int)(Main.COREMain.debugText.characterHeight * 0.7f + 3), width, (int)(Main.COREMain.debugText.characterHeight * 0.7f) + 3))
                {
                    if (!isOptionTrue[list[i]] && !changedValue[i])
                    {
                        isOptionTrue[list[i]] = true;
                        changedValue[i] = true;
                    }
                    else if (!changedValue[i])
                    {
                        isOptionTrue[list[i]] = false;
                        changedValue[i] = true;
                    }
                }
                else
                    changedValue[i] = false;

                //hard coded part for assigning false or true because c# doesnt support dynamically changing given variables
                Main.COREMain.renderGrid = isOptionTrue[list[0]];
                Main.COREMain.renderBackground = isOptionTrue[list[1]];
                if (Main.COREMain.scenes[Main.COREMain.SelectedScene].currentObj != -1)
                {
                    Main.COREMain.CurrentModel.renderLines = isOptionTrue[list[2]];
                    Main.COREMain.CurrentModel.renderNormals = isOptionTrue[list[3]];
                }
                Main.COREMain.renderGUI = isOptionTrue[list[4]];
                Main.COREMain.renderIDFramebuffer = isOptionTrue[list[5]];
                Main.COREMain.renderToIDFramebuffer = isOptionTrue[list[6]];
                Rendering.renderOrthographic = isOptionTrue[list[7]];
                cullFaces = isOptionTrue[list[9]];
                isOptionTrue[list[11]] = false;
                Main.COREMain.addCube = isOptionTrue[list[12]];
                Main.COREMain.addCylinder = isOptionTrue[list[13]];
                Main.COREMain.renderEntireDir = isOptionTrue[list[15]];
                Main.COREMain.allowAlphaOverride = isOptionTrue[list[16]];
                Main.COREMain.useChromAber = isOptionTrue[list[17]];
                Main.COREMain.useVignette = isOptionTrue[list[18]];
                Main.COREMain.fullscreen = isOptionTrue[list[19]];

                if (!isOptionTrue[list[i]])
                    div.Write(list[i], 5, offset, 0.7f);
                else
                    div.Write(list[i], 5, offset, 0.7f, new COREMath.Vector3(1, 0, 1));
            }
        }
    }
}