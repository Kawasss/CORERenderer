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

        public unsafe Font(uint pixelHeight, string fontPath)
        {
            shader = new($"{COREMain.pathRenderer}\\shaders\\Font.vert", $"{COREMain.pathRenderer}\\shaders\\Font.frag");

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
                    glTexImage2D(GL_TEXTURE_2D, 0, GL_RED, bitmap.Width, bitmap.Rows, 0, GL_RED, GL_UNSIGNED_BYTE, bitmap.Buffer);
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
            glBindBuffer(GL_ARRAY_BUFFER, VBO);

            glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 6 * 4, (IntPtr)null, GL_DYNAMIC_DRAW);

            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            int vertexLocation = shader.GetAttribLocation("vertex");
            glVertexAttribPointer((uint)vertexLocation, 4, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
            glEnableVertexAttribArray((uint)vertexLocation);

            shader.Use();
            shader.SetInt("Texture", GL_TEXTURE0);
            shader.SetMatrix("projection", Rendering.GetOrthograpicProjectionMatrix());
        }

        public unsafe void RenderText(string text, float x, float y, float scale, Vector2 direction) => RenderText(text, x, y, scale, direction, new Vector3(1, 1, 1));

        public unsafe void RenderText(string text, float x, float y, float scale, Vector2 direction, Vector3 color)
        {
            shader.Use();
            shader.SetVector3("textColor", color);

            glActiveTexture(GL_TEXTURE0);
            glBindVertexArray(VAO);

            //float angle = MathF.Atan2(direction.y, direction.x);
            //Matrix rotation = MathC.GetRotationZMatrix(MathC.RadToDeg(angle));
            //Matrix translation = MathC.GetTranslationMatrix(x, y, 0);

            for (int i = 0; i < text.Length; i++)
            {
                byte c = (byte)text[i];
                Character ch = characters[c];

                if (text[i] == ' ')
                {
                    x += (ch.advance >> 6) * scale;
                    continue;
                }

                float xpos = x + ch.bearing.x * scale;
                float ypos = y - (ch.size.y - ch.bearing.y) * scale;

                float w = ch.size.x * scale;
                float h = ch.size.y * scale;

                float[] vertices = new float[] {
                     xpos,     ypos + h,     0.0f, 0.0f,
                     xpos,     ypos,         0.0f, 1.0f,
                     xpos + w, ypos,         1.0f, 1.0f,
                     xpos,     ypos + h,     0.0f, 0.0f,
                     xpos + w, ypos,         1.0f, 1.0f,
                     xpos + w, ypos + h,     1.0f, 0.0f
                };
                glBindTexture(GL_TEXTURE_2D, ch.textureID);

                glBindBuffer(GL_ARRAY_BUFFER, VBO);
                fixed (float* temp = &vertices[0])
                {
                    IntPtr intptr = new(temp);
                    glBufferSubData(GL_ARRAY_BUFFER, 0, vertices.Length * sizeof(float), intptr);
                }
                glBindBuffer(GL_ARRAY_BUFFER, 0);

                glDrawArrays(PrimitiveType.Triangles, 0, 6);
                x += (ch.advance >> 6) * scale;
            }
            glBindVertexArray(0);
            glBindTexture(GL_TEXTURE0, 0);
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
