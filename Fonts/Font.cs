using SharpFont;
using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.Main;
using CORERenderer.shaders;
using COREMath;
using System.Reflection;
using System.ComponentModel;
using CORERenderer.OpenGL;

namespace CORERenderer.Fonts
{
    public unsafe class Font
    {
        private uint VAO;
        private uint VBO;

        public int characterHeight;

        public Dictionary<byte, Character> characters = new(); //private

        private readonly Shader shader;

        public bool drawWithHighlights = false;

        public unsafe Font(uint pixelHeight, string fontPath)
        {
            shader = new($"{Main.COREMain.pathRenderer}\\shaders\\Font.vert", $"{Main.COREMain.pathRenderer}\\shaders\\Font.frag");

            characterHeight = (int)pixelHeight;

            Library lib = new();
            Face face = new(lib, fontPath);
            face.SetPixelSizes(0, pixelHeight);

            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

            glActiveTexture(GL_TEXTURE0);

            for (byte c = 0; c < 128; c++)
            {
                try
                {
                    // load glyph
                    face.LoadChar(c, LoadFlags.Render, LoadTarget.Normal);
                    GlyphSlot glyph = face.Glyph;
                    FTBitmap bitmap = glyph.Bitmap;

                    // create glyph texture
                    uint texObj = glGenTexture();
                    glBindTexture(GL_TEXTURE_2D, texObj);
                    glTexImage2D(Image2DTarget.Texture2D, 0, GL_RED, bitmap.Width, bitmap.Rows, 0, GL_RED, GL_UNSIGNED_BYTE, bitmap.Buffer);
                    //(int)texObj
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

                    // add character
                    Character ch = new();
                    ch.textureID = texObj;
                    ch.size = new(bitmap.Width, bitmap.Rows);
                    ch.bearing = new(glyph.BitmapLeft, glyph.BitmapTop);
                    ch.advance = glyph.Advance.X.Value;
                    characters.Add(c, ch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            
            glBindTexture(GL_TEXTURE_2D, 0);
            glPixelStorei(GL_UNPACK_ALIGNMENT, 4);

            VBO = glGenBuffer();
            glBindBuffer(BufferTarget.ArrayBuffer, VBO);

            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 6 * 4, (IntPtr)null, GL_DYNAMIC_DRAW);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            shader.ActivateAttributes();

            shader.Use();
            shader.SetInt("Texture", 0);
            shader.SetMatrix("projection", Rendering.GetOrthograpicProjectionMatrix(Main.COREMain.Width, Main.COREMain.Height));
        }

        public unsafe void RenderText(string text, float x, float y, float scale, Vector2 direction) => RenderText(text, x, y, scale, direction, new Vector3(1, 1, 1));

        public unsafe void RenderText(string text, float x, float y, float scale, Vector2 direction, Vector3 color)
        {
            shader.SetVector3("textColor", color);

            glActiveTexture(GL_TEXTURE0);
            glBindVertexArray(VAO);

            //float angle = MathF.Atan2(direction.y, direction.x);
            //Matrix rotation = MathC.GetRotationZMatrix(MathC.RadToDeg(angle));
            //Matrix translation = MathC.GetTranslationMatrix(x, y, 0);

            int indexOfTrue = text.IndexOf("True");
            int indexOfFalse = text.IndexOf("False");
            int indexOfOK = text.IndexOf("OK");
            int indexOfBAD = text.IndexOf("BAD");

            for (int i = 0; i < text.Length; i++)
            {
                byte c = (byte)text[i];
                Character ch = characters[c];

                if (text[i] == ' ')
                {
                    x += (ch.advance >> 6) * scale;
                    continue;
                }
                if (IsColorWhite(color) && (indexOfTrue != -1 && (i == indexOfTrue || i == indexOfTrue + 1 || i == indexOfTrue + 2 || i == indexOfTrue + 3)) || (indexOfOK != -1 && (i == indexOfOK || i == indexOfOK + 1)))
                    shader.SetVector3("textColor", new(0, 1, 0));
                else if (IsColorWhite(color) && (indexOfFalse != -1 && (i == indexOfFalse || i == indexOfFalse + 1 || i == indexOfFalse + 2 || i == indexOfFalse + 3 || i == indexOfFalse + 4)) || (indexOfBAD != -1 && (i == indexOfBAD || i == indexOfBAD + 1 || i == indexOfBAD + 2)))
                    shader.SetVector3("textColor", new(1, 0, 0));
                else if (drawWithHighlights && IsCharUsedNumerical(text, i))
                    shader.SetVector3("textColor", new(0.78f, 0.89f, 0.45f));
                else if (drawWithHighlights && int.TryParse($"{text[i]}", out _) && IsIntUsedNumerical(text, i))
                        shader.SetVector3("textColor", new(0.78f, 0.89f, 0.45f));
                else
                    shader.SetVector3("textColor", color);

                float xpos = x + ch.bearing.x * scale;
                float ypos = y - (ch.size.y - ch.bearing.y) * scale;

                float w = ch.size.x * scale;
                float h = ch.size.y * scale;

                float[] vertices = new float[]
                {
                     xpos,     ypos + h,     0.0f, 0.0f,
                     xpos,     ypos,         0.0f, 1.0f,
                     xpos + w, ypos,         1.0f, 1.0f,
                     xpos,     ypos + h,     0.0f, 0.0f,
                     xpos + w, ypos,         1.0f, 1.0f,
                     xpos + w, ypos + h,     1.0f, 0.0f
                };
                glBindTexture(GL_TEXTURE_2D, ch.textureID);

                glBindBuffer(BufferTarget.ArrayBuffer, VBO);
                fixed (float* temp = &vertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), intptr);
                }
                glBindBuffer(BufferTarget.ArrayBuffer, 0);

                glDrawArrays(PrimitiveType.Triangles, 0, 6);
                x += (ch.advance >> 6) * scale;
            }
            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE0, 0);
        }

        private bool IsCharUsedNumerical(string fullText, int index)
        {
            if (index + 1 >= fullText.Length)
                return false;
            if ((fullText[index] == ',' || fullText[index] == '.' || fullText[index] == '-') && int.TryParse($"{fullText[index + 1]}", out _))
                return true;
            return false;
        }

        private bool IsIntUsedNumerical(string fullText, int index)
        {
            if (fullText.Length == 1)
                return true;

            if (index - 1 < 0)
                return fullText[index + 1] == ' ' || fullText[index + 1] == '%' || int.TryParse($"{fullText[index + 1]}", out _);

            else if (index + 1 >= fullText.Length)
                return fullText[index - 1] == ' ' || fullText[index - 1] == '~' || fullText[index - 1] == '-' || int.TryParse($"{fullText[index - 1]}", out _);

            else
                return (int.TryParse($"{fullText[index - 1]}", out _) || fullText[index - 1] == '~' || fullText[index - 1] == ' ' || fullText[index - 1] == ',' || fullText[index - 1] == '.' || fullText[index - 1] == '-') && (int.TryParse($"{fullText[index + 1]}", out _) || fullText[index + 1] == ' ' || fullText[index + 1] == ',' || fullText[index + 1] == '.' || fullText[index + 1] == '%');
        }

        /// <summary>
        /// returns a float resembling the length of a given string, can be used to detect if a string is longer than something else
        /// </summary>
        /// <param name="text"></param>
        /// <param name="scale"></param>
        /// <returns>Width of a given string and scale</returns>
        public float GetStringWidth(string text, float scale)
        {
            float x = 0;

            for (int i = 0; i < text.Length; i++) //reuse RenderText() to return the width of a string by practically doing everything the same except for the GPU side of things
            {
                byte c = (byte)text[i];
                Character ch = characters[c];

                x += (ch.advance >> 6) * scale;
            }

            return x;
        }

        private bool IsColorWhite(Vector3 color)
        {
            return color.x == 1 && color.y == 1 && color.z == 1;
        }

        ~Font()
        {
            for (byte i = 0; i < 128; i++)
                glDeleteTexture(characters[i].textureID);
            glDeleteBuffer(VBO);
            glDeleteVertexArray(VAO);
        }
    }
}