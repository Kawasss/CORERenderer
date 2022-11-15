using CORERenderer.Main;

namespace CORERenderer
{
    internal class EnginePresets : EngineProperties
    {
        static public void SetPresets()
        {
            EngineProperties.showFrameTime = true;
            EngineProperties.showFPS = true;
            EngineProperties.maxFPS = 1000;
        }
    }
}
