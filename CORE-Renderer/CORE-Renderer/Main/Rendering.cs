using COREMath;
using CORERenderer.CRSFile;
using static CORERenderer.OpenGL.GL;

namespace CORERenderer.Main
{
    public class Rendering
    {
        public static void RenderAllObjects(CRS crs)
        {
            for (int i = 0; i < crs.allOBJs.Count; i++)
                crs.allOBJs[i].Render();
        }

        public static void RenderLights(List<Light> locations)
        {
            CORERenderContent.lightShader.Use();

            glBindVertexArray(CORERenderContent.vertexArrayObjectLightSource);

            for (int i = 0; i < locations.Count; i++)
            {
                CORERenderContent.lightShader.SetMatrix("model", Matrix.IdentityMatrix * MathC.GetTranslationMatrix(locations[i].position) * MathC.GetScalingMatrix(0.2f));
                glDrawArrays(GL_TRIANGLES, 0, 36);
            }
        }

        public static void RenderGrid()
        {
            CORERenderContent.gridShader.Use();

            CORERenderContent.gridShader.SetMatrix("model", Matrix.IdentityMatrix * new Matrix(true, 100 * MathC.GetLengthOf(CORERenderContent.camera.position)));

            CORERenderContent.gridShader.SetVector3("playerPos", CORERenderContent.camera.position);

            glBindVertexArray(CORERenderContent.vertexArrayObjectGrid);
            glDrawArrays(GL_TRIANGLES, 0, 6);
        }

        public static void RenderCubemap(Cubemap cubemap)
        {
            glDisable(GL_CULL_FACE);
            glDepthFunc(GL_LEQUAL);
            cubemap.shader.Use();

            glBindVertexArray(cubemap.VAO);

            glActiveTexture(GL_TEXTURE0);
            glBindTexture(GL_TEXTURE_CUBE_MAP, cubemap.textureID);

            glDrawArrays(GL_TRIANGLES, 0, 36);
            glEnable(GL_CULL_FACE);

            glBindVertexArray(0);
            glDepthFunc(GL_LESS);
        }

        public static void RenderAllObjectsAsPBRDebugs(CRS crs)
        {
            for (int i = 0; i < crs.allOBJs.Count; i++)
                crs.allOBJs[i].PBRDebugRender();
        }
    }
}
