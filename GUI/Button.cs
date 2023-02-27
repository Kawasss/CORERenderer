using CORERenderer.Main;
using COREMath;

namespace CORERenderer.GUI
{
    public class Button
    {
        private string name;

        public int x, y;

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

            width = Name.Length * (int)(COREMain.debugText.characterHeight);
            height = (int)(COREMain.debugText.characterHeight);
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
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 1, 1));
                Submenu.isOpen = false;
                firstRender = false;
                return;
            }

            if (!isPressed && changed)// && changed //button wont show if it isnt pressed in its optimised state
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 1, 1));
                Submenu.isOpen = false;
            }

            else if (isPressed && changed)
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 0, 1));
                Submenu.isOpen = true;
            }
            if (isPressed)
                onClick?.Invoke();
        }

        public void RenderStatic()
        {
            if (!isPressed)// && changed //button wont show if it isnt pressed in its optimised state
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 1, 1));
                Submenu.isOpen = false;
            }

            else if (isPressed)
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 0, 1));
                Submenu.isOpen = true;
            }
            if (isPressed)
                onClick?.Invoke();
        }

        public void Update()
        {
            bool previousState = isPressed;
            changed = true;
            if (attachedSubmenu == null)
                isPressed = !isPressed && COREMain.CheckAABBCollisionWithClick(x, y, width, height);
            else if (isPressed) //is not pressed if cursor is outside button hitbox and submenu hitbox
                isPressed = COREMain.CheckAABBCollision(x, y, width, height) || COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);
            else if (!isPressed)
                isPressed = COREMain.CheckAABBCollisionWithClick(x, y, width, height);// || COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);

            changed = !(previousState == isPressed);
        }
    }
}