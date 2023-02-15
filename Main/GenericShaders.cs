using CORERenderer.shaders;
using System.Net.Http.Headers;

namespace CORERenderer.Main
{
    public class GenericShaders
    {
        public static Shader image2DShader;
        public static Shader lightingShader;
        public static Shader backgroundShader;
        public static Shader gridShader;
        public static Shader GenericLightingShader;
        public static Shader solidColorQuadShader;
        public static Shader arrowShader;
        public static Shader pickShader;

        public static void SetShaders()
        {
            image2DShader = new($"{COREMain.pathRenderer}\\shaders\\2DImage.vert", $"{COREMain.pathRenderer}\\shaders\\plane.frag");
            lightingShader = new($"{COREMain.pathRenderer}\\shaders\\lightSource.vert", $"{COREMain.pathRenderer}\\shaders\\lightSource.frag");
            backgroundShader = new($"{COREMain.pathRenderer}\\shaders\\skybox.vert", $"{COREMain.pathRenderer}\\shaders\\Background.frag");
            gridShader = new($"{COREMain.pathRenderer}\\shaders\\grid.vert", $"{COREMain.pathRenderer}\\shaders\\grid.frag");
            GenericLightingShader = new($"{COREMain.pathRenderer}\\shaders\\shader.vert", $"{COREMain.pathRenderer}\\shaders\\lighting.frag");
            solidColorQuadShader = new($"{COREMain.pathRenderer}\\shaders\\Basic.vert", $"{COREMain.pathRenderer}\\shaders\\SolidColor.frag");
            arrowShader = new($"{COREMain.pathRenderer}\\shaders\\Arrow.vert", $"{COREMain.pathRenderer}\\shaders\\Arrow.frag");
            pickShader = new($"{COREMain.pathRenderer}\\shaders\\shader.vert", $"{COREMain.pathRenderer}\\shaders\\SolidColor.frag");
        }
    }
}
