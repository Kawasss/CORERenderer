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

        private bool pressed = false;

        private Action onClick = null;

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
            
            if (!pressed)
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 1, 1));
                Submenu.isOpen = false;
            }
                
            if (pressed)
            {
                COREMain.debugText.RenderText(name, -(COREMain.monitorWidth / 2) + x, -(COREMain.monitorHeight / 2) + y + 4, 1, new Vector2(1, 0), new Vector3(1, 0, 1));
                Submenu.isOpen = true;
                onClick?.Invoke();
            }
        }

        private void Update()
        {
            if (attachedSubmenu == null)
                pressed = !pressed && COREMain.CheckAABBCollisionWithClick(x, y, width, height);
            else if (pressed) //is not pressed if cursor is outside button hitbox and submenu hitbox
                pressed = COREMain.CheckAABBCollision(x, y, width, height) || COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);
            else if (!pressed)
                pressed = COREMain.CheckAABBCollisionWithClick(x, y, width, height);// || COREMain.CheckAABBCollision(attachedSubmenu.x, attachedSubmenu.y - attachedSubmenu.height, attachedSubmenu.width, attachedSubmenu.height);
        }
    }
}