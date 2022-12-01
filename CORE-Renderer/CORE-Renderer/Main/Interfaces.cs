using CORERenderer.shaders;

namespace CORERenderer.Main
{
    public interface EngineProperties
    {
        public static bool showFPS = false;
        public static bool showFrameTime = false;
        public static int maxFPS = 1000;
    }

    /// <summary>
    /// contains all methods for working with the engine properties
    /// </summary>
    public class EPL : EngineProperties
    {
        /// <summary>
        /// Runs the logic on the interface EngineProperties
        /// </summary>
        /// <returns>The minimum amount of frametime needed for a frame to finish rendering as a double</returns>
        public static double RunEngineLogic()
        {
            if (EngineProperties.maxFPS > 1000)
                EngineProperties.maxFPS = 1000;
            return (double)1 / EngineProperties.maxFPS;

        }
    }
}
