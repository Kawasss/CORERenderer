using static CORERenderer.OpenGL.GL;
using static CORERenderer.OpenGL.Rendering;
using CORERenderer.shaders;
using COREMath;
using CORERenderer.Loaders;
using CORERenderer.OpenGL;
using CORERenderer.Main;

namespace CORERenderer.textures
{
    public class Image3D
    {
        public uint vao;
        public uint vbo;
        public uint Handle;

        private Texture texture;

        public Shader shader;

        public Vector3 translation;
        public float scale;
        public float rotationX;
        public float rotationY;
        public float rotationZ;

        public unsafe static Image3D LoadImageIn3D(RenderMode mode, string imagePath)
        {
            Texture texture;
            if (mode == RenderMode.PNGImage || mode == RenderMode.RPIFile)
                texture = Texture.ReadFromFile(imagePath);
            else if (mode == RenderMode.JPGImage)
                texture = Texture.ReadFromRGBFile(imagePath); //jpg doesnt use transparancy so should be using another color format
            else
                texture = Texture.ReadFromRGBFile(imagePath);

            Image3D i = new()
            {
                texture = texture,
                shader = new($"{COREMain.pathRenderer}\\shaders\\Plane.vert", $"{COREMain.pathRenderer}\\shaders\\Plane.frag"),
                translation = new(0, 0.01f, 0),
                scale = 1,
                rotationX = 180,
                rotationY = 0,
                rotationZ = 0
            };

            i.vbo = glGenBuffer();
            glBindBuffer(GL_ARRAY_BUFFER, i.vbo);

            //for optimal coordinates height is relative to the width of the image, this makes every image the same size, no matter the original size
            float height = (float)i.texture.height / (float)i.texture.width;

            float[] vertices = new float[]
            {
               -1, height, 0, 1,
               -1,      -1, 0, 0,
                1,      -1, 1, 0,

               -1, height, 0, 1,
                1,      -1, 1, 0,
                1, height, 1, 1
            };


            fixed (float* temp = &vertices[0])
            {
                IntPtr intptr = new(temp);
                glBufferData(GL_ARRAY_BUFFER, vertices.Length * sizeof(float), intptr, GL_STATIC_DRAW);
            }

            i.vao = glGenVertexArray();
            glBindVertexArray(i.vao);

            int vertexLocation = i.shader.GetAttribLocation("aPos");
            glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)0);
            glEnableVertexAttribArray((uint)vertexLocation);

            vertexLocation = i.shader.GetAttribLocation("aTexCoords");
            glVertexAttribPointer((uint)vertexLocation, 2, GL_FLOAT, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
            glEnableVertexAttribArray((uint)vertexLocation);

            i.shader.Use();
            i.shader.SetInt("Texture", GL_TEXTURE0);

            glBindBuffer(GL_ARRAY_BUFFER, 0);
            glBindVertexArray(0);

            return i;
        }

        public void Render()
        {
            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glStencilMask(0xFF);

            glDisable(GL_CULL_FACE);
            
            shader.Use();
            texture.Use(GL_TEXTURE0);

            shader.SetMatrix("model", Matrix.IdentityMatrix
            * new Matrix(scale, translation)
            * (MathC.GetRotationXMatrix(rotationX)
            * MathC.GetRotationYMatrix(rotationY)
            * MathC.GetRotationZMatrix(rotationZ)));

            glBindVertexArray(vao);
            glDrawArrays(PrimitiveType.Triangles, 0, 6);
            
            glEnable(GL_CULL_FACE);
        }
    }
}