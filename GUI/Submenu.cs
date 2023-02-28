using CORERenderer.Main;
using CORERenderer.OpenGL;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;

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

        public Submenu(string[] options)
        {
            list = options;

            changedValue = new bool[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                isOptionTrue.Add(options[i], false);
                changedValue[i] = false;
            }

            width = (int)(COREMain.monitorWidth * 0.075f);
            height = (COREMain.debugText.characterHeight + 5) * options.Length;

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

            if (COREMain.secondPassed && !isAttached)
            {
                COREMain.console.WriteError("Submenu not attached");
                return;
            }

            GenericShaders.solidColorQuadShader.SetVector3("color", 0.13f, 0.13f, 0.13f);
            div.Render();
            GenericShaders.solidColorQuadShader.SetVector3("color", 0.15f, 0.15f, 0.15f);

            int offset = height - (int)(COREMain.debugText.characterHeight * 0.7f) - 3;
            for (int i = 0; i < list.Length; i++, offset -= (int)(COREMain.debugText.characterHeight * 0.7f) + 3)
            {
                if (COREMain.CheckAABBCollisionWithClick(x, y - (i + 1) * (int)(COREMain.debugText.characterHeight * 0.7f + 3), width, (int)(COREMain.debugText.characterHeight * 0.7f) + 3))
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
                COREMain.renderGrid = isOptionTrue[list[0]];
                COREMain.renderBackground = isOptionTrue[list[1]];
                if (COREMain.scenes[COREMain.selectedScene].currentObj != -1)
                {
                    COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].renderLines = isOptionTrue[list[2]];
                    COREMain.scenes[COREMain.selectedScene].allModels[COREMain.scenes[COREMain.selectedScene].currentObj].renderNormals = isOptionTrue[list[3]];
                }
                COREMain.renderGUI = isOptionTrue[list[4]];
                COREMain.renderIDFramebuffer = isOptionTrue[list[5]];
                COREMain.renderToIDFramebuffer = isOptionTrue[list[6]];
                COREMain.renderOrthographic = isOptionTrue[list[7]];
                cullFaces = isOptionTrue[list[9]];
                isOptionTrue[list[11]] = false;
                COREMain.addCube = isOptionTrue[list[12]];
                COREMain.addCylinder = isOptionTrue[list[13]];
                COREMain.renderEntireDir = isOptionTrue[list[15]];
                COREMain.allowAlphaOverride = isOptionTrue[list[16]];
                COREMain.useChromAber = isOptionTrue[list[17]];
                COREMain.useVignette = isOptionTrue[list[18]];

                if (!isOptionTrue[list[i]])
                    div.Write(list[i], 5, offset, 0.7f);
                else
                    div.Write(list[i], 5, offset, 0.7f, new COREMath.Vector3(1, 0, 1));
            }
        }
    }
}