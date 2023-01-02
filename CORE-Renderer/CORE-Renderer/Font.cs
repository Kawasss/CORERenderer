using SharpFont;
using static CORERenderer.OpenGL.GL;
using CORERenderer.Main;
using CORERenderer.shaders;
using COREMath;

namespace CORERenderer
{
    public unsafe class Font
    {
        private uint VAO;
        private uint VBO;

        private Dictionary<uint, Character> characters = new();

        private readonly Shader shader = new($"{CORERenderContent.pathRenderer}\\shaders\\font.vert", $"{CORERenderContent.pathRenderer}\\shaders\\font.frag");

        public unsafe Font(uint pixelHeight)
        {
            Library lib = new();
            Face face = new(lib, $"{CORERenderContent.pathRenderer}\\Fonts\\baseFont.ttf");
            face.SetPixelSizes(0, pixelHeight);

            glPixelStoref(GL_UNPACK_ALIGNMENT, 1);

            for (uint c = 0; c < 128; c++)
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
                    glTexImage2D(GL_TEXTURE_2D, 0, GL_R8, bitmap.Width, bitmap.Rows, 0, GL_RED, GL_UNSIGNED_INT, bitmap.Buffer);

                    glTexParameterf((int)texObj, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
                    glTexParameterf((int)texObj, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
                    glTexParameterf((int)texObj, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
                    glTexParameterf((int)texObj, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);

                    // add character
                    Character ch = new();
                    ch.textureID = texObj;
                    ch.size = new(bitmap.Width, bitmap.Rows);
                    ch.bearing = new(glyph.BitmapLeft, glyph.BitmapTop);
                    ch.advance = (int)glyph.Advance.X.Value;
                    characters.Add(c, ch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            glBindTexture(GL_TEXTURE_2D, 0);
            glPixelStoref(GL_UNPACK_ALIGNMENT, 4);

            float[] vertices =
            {
                    0.0f, -1.0f,   0.0f, 0.0f,
                    0.0f,  0.0f,   0.0f, 1.0f,
                    1.0f,  0.0f,   1.0f, 1.0f,
                    0.0f, -1.0f,   0.0f, 0.0f,
                    1.0f,  0.0f,   1.0f, 1.0f,
                    1.0f, -1.0f,   1.0f, 0.0f
                };

            /*VBO = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, VBO);

            fixed (float* temp = &vertices[0])
            {
                IntPtr intptr = new(temp);
                glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), temp, GL_STATIC_DRAW);
            }*/

            glBindBuffer(GL_ARRAY_BUFFER, 0);
            VAO = glGenVertexArray();
            glBindVertexArray(VAO);

            /*int vertexLocation = shader.GetAttribLocation("aPos");
            glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
            glEnableVertexAttribArray((uint)vertexLocation);

            vertexLocation = shader.GetAttribLocation("aTexCoords");
            glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
            glEnableVertexAttribArray((uint)vertexLocation);*/
        }

        public void RenderText(string text, float x, float y, float scale, Vector2 direction)
        {
            glActiveTexture(GL_TEXTURE0);
            glBindVertexArray(VAO);

            shader.Use();
            shader.SetVector3("textColor", 1, 1, 1);

            float angle = MathF.Atan2(direction.y, direction.x);
            Matrix rotation = MathC.GetRotationZMatrix(MathC.RadToDeg(angle));
            Matrix translation = MathC.GetTranslationMatrix(x, y, 0);

            float charX = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (!characters.ContainsKey(c))
                    continue;
                Character ch = characters[c];

                float w = ch.size.x * scale;
                float h = ch.size.y * scale;
                float xrel = charX + ch.bearing.x * scale;
                float yrel = (ch.size.y - ch.bearing.y) * scale;

                charX += (ch.advance >> 6) * scale;

                Matrix trans = MathC.GetTranslationMatrix(xrel, yrel, 0);

                shader.SetMatrix("model", Matrix.IdentityMatrix
                * new Matrix(true, w, h, 1)
                * trans
                * rotation
                * translation);
                
                glBindTexture(GL_TEXTURE_2D, ch.textureID);
                shader.SetInt("Texture", GL_TEXTURE0);
                glDrawArrays(GL_TRIANGLES, 0, 6);
            }
        }
    }
}
