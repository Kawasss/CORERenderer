using CORERenderer.OpenGL;
using COREMath;

namespace CORERenderer.GUI
{
    public class Button
    {
        private string name;

        public int x, y;
        private float renderX, renderY;

        public int width, height;

        public Submenu attachedSubmenu = null;

        public bool isPressed = false;

        private Action onClick = null;

        public bool changed = true;

        private bool firstRender = true;

        public Button(string Name, int X, int Y)
        {
            name = Name;

            x = X;
            y = Y;

            width = Name.Length * (int)(Main.COREMain.debugText.characterHeight);
            height = (int)(Main.COREMain.debugText.characterHeight);

            renderX = -(Main.COREMain.monitorWidth / 2) + x;
            renderY = -(Main.COREMain.monitorHeight / 2) + y + 4;
        }

        public void OnClick(Action action)
        {
            onClick = action;
        }

        public void Render() //better to make render once for a bitmap and then be reused till updated
        {
            Update();

            if (firstRender)
            {
                Main.COREMain.debugText.RenderText(name, renderX, renderY, 1, Color.White);
                Submenu.isOpen = false;
                firstRender = false;
                return;
            }

            if (!isPressed && changed)// && changed //button wont show if it isnt pressed in its optimised state
            {
                Main.COREMain.debugText.RenderText(name, renderX, renderY, 1, Color.White);
                Submenu.isOpen = false;
            }

            else if (isPressed && changed)
            {
                Main.COREMain.debugText.RenderText(name, renderX, renderY, 1, new Vector3(1, 0, 1));
                Submenu.isOpen = true;
            }
            if (isPressed)
                onClick?.Invoke();
        }

        public void RenderStatic()
        {
            if (!isPressed)
            {
                Main.COREMain.debugText.RenderText(name, renderX, renderY, 1, Color.White);
                Submenu.isOpen = false;
            }

            else if (isPressed)
            {
                Main.COREMain.debugText.RenderText(name, renderX, renderY, 1, new Vector3(1, 0, 1));
                Submenu.isOpen = true;
            }
            if (isPressed)
                onClick?.Invoke();
        }

        public void RenderConditionless()
        {
            changed = true;
            RenderStatic();
        }

        public void Update()
        {
            bool previousState = isPressed;
            changed = true;
            if (attachedSubmenu == null)
                isPressed = !isPressed && Main.COREMain.CheckAABBCollisionWithClick(x, y, width, height);
            else if (isPressed) //is not pressed if cursor is outside button hitbox and submenu hitbox
                isPressed = Main.COREMain.CheckAABBCollision(x, y, width, height) || Main.COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);
            else if (!isPressed)
                isPressed = Main.COREMain.CheckAABBCollisionWithClick(x, y, width, height);// || COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);

            changed = !(previousState == isPressed);
        }
    }
}