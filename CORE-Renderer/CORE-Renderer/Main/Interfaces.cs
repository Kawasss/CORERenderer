using CORERenderer.shaders;

namespace CORERenderer.Main
{
    public interface EngineProperties
    {
        public static bool showFPS = false;
        public static bool showFrameTime = false;
        public static int maxFPS = 1000;
    }
    public class EPL : EngineProperties
    {
        /// <summary>
        /// Runs the logic on interface EngineProperties
        /// </summary>
        /// <returns>The minimum amount of frametime as a float</returns>
        public static double RunEngineLogic()
        {
            if (EngineProperties.maxFPS > 1000)
                EngineProperties.maxFPS = 1000;
            return (double)1 / EngineProperties.maxFPS;

        }
    }
}
