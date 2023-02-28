using CORERenderer.Main;

namespace CORERenderer.GUI
{
    public class COREConsole
    {
        private Div quad;

        private int linesPrinted = 0;
        private int maxLines = 0;
        
        //if this needs to be optimised make it reuse the previous frames as a texture so the previous lines dont have to reprinted, saving draw calls
        private string[] lines;

        private int Width;
        private int Height;

        public bool changed = true;

        public COREConsole(int width, int height, int x, int y)
        {
            quad = new(width, height, x, y);

            Width = width;
            Height = height;

            maxLines = height / (int)(COREMain.debugText.characterHeight * 0.7f + 2);

            lines = new string[maxLines];
        }

        public void Wipe()
        {
            lines = new string[maxLines];
            linesPrinted = 0;
            changed = true;
        }

        public void Render()
        {
            if (!changed)
                return;

            changed = false;

            quad.Render();
            
            int lineOffset = (int)(COREMain.debugText.characterHeight * 0.7f + 2); //scale = 1
            for (int i = 0; i < linesPrinted; i++, lineOffset += (int)(COREMain.debugText.characterHeight * 0.7f + 2))
            {
                if (lines[i].Length < 5 || lines[i][..5] != "ERROR")
                    quad.Write(lines[i], 0, Height - lineOffset, 0.7f);
                else
                    quad.WriteError(lines[i], 0, Height - lineOffset, 0.7f);
            }
        }

        public void WriteLine(string text)
        {
            changed = true;

            linesPrinted++;
            if (linesPrinted > maxLines)
            {
                linesPrinted = 1;
                lines = new string[maxLines];
            }
            //seperates the text if its longer than the quad
            if (text.Length < Height / (COREMain.debugText.characterHeight / 4))
            {
                lines[linesPrinted - 1] = text;
                return;
            }
            for (int i = 0; i < text.Length; i += (int)(Height / (COREMain.debugText.characterHeight / 4)))
            {
                if (text[i..].Length > 10000)//Height / (COREMain.debugText.characterHeight * 100))
                {
                    lines[linesPrinted - 1] = $"{text[i..(i + Height / (int)(COREMain.debugText.characterHeight / 4))]}";
                    linesPrinted++;
                }
                else
                {
                    if (linesPrinted > maxLines)
                    {
                        linesPrinted = 1;
                        lines = new string[maxLines];
                    }
                    lines[linesPrinted - 1] = text[i..^1];
                    return;
                }
            }
        }

        public void Write(string text)
        {
            linesPrinted--;
            WriteLine(text);
        }

        public void WriteError(Exception err) => WriteError(err.ToString());

        public void WriteError(string err)
        {
            changed = true;

            linesPrinted++;
            string text = "ERROR " + err;
            if (linesPrinted > maxLines)
            {
                linesPrinted = 1;
                lines = new string[maxLines];
            }
            //seperates the text if its longer than the quad
            if (text.Length < Height / (COREMain.debugText.characterHeight / 4))
            {
                lines[linesPrinted - 1] = text;
                return;
            }
            for (int i = 0; i < text.Length; i += (int)(Height / (COREMain.debugText.characterHeight / 3)))
            {
                if (text[i..^1].Length > Height / (COREMain.debugText.characterHeight / 3))
                {
                    lines[linesPrinted - 1] = $"ERROR {text[i..(i + Height / (int)(COREMain.debugText.characterHeight / 3))]}";
                    linesPrinted++;
                }
                else
                {
                    if (linesPrinted > maxLines)
                    {
                        linesPrinted = 1;
                        lines = new string[maxLines];
                    }
                    lines[linesPrinted - 1] = "ERROR " + text[i..^1];
                    return;
                }
            }
        }

        public void RenderEvenIfNotChanged()
        {
            changed = true;
            Render();
        }

        public void RenderStatic() => Render();
    }
}
