using COREMath;
using CORERenderer.GUI;
using CORERenderer.OpenGL;
using static CORERenderer.Main.COREMain;
using static CORERenderer.OpenGL.Rendering;

namespace CORERenderer.Main
{
    public class Debugmenu
    {
        public static bool isVisible = false;
        private static bool firstTime = true;

        private static Div debugHolder;
        private static Graph renderingTicks;
        private static Graph debugFSGraph;

        public static void Render()
        {
            if (!isVisible)
                return;

            if (firstTime)
            {
                int debugWidth = (int)debugText.GetStringWidth("Ticks spent depth sorting: timeSpentDepthSorting", 0.7f);
                debugHolder = new((int)(monitorWidth * 0.496 - monitorWidth * 0.125f), (int)(monitorHeight * 0.242f - 25), viewportX, (int)(monitorHeight * 0.004f));
                renderingTicks = new(0, debugWidth, (int)(debugText.characterHeight * 2), viewportX - (int)(debugWidth * 0.045f), (int)(debugHolder.Height - debugText.characterHeight * 12));
                debugFSGraph = new(0, debugWidth, (int)(debugText.characterHeight * 2), (int)(monitorWidth * 0.5f - debugWidth * 1.00f), (int)(debugHolder.Height - debugText.characterHeight * 7));
                debugFSGraph.showValues = false;
                renderingTicks.showValues = false;
                firstTime = false;
            }
            debugHolder.Render();

            renderingTicks.UpdateConditionless(TicksSpent3DRenderingThisFrame);
            if (debugFSGraph.MaxValue > 70) debugFSGraph.MaxValue = (int)(timeSinceLastFrame * 1000 * 1.5f);
            debugFSGraph.color = 1 / timeSinceLastFrame < refreshRate / 2 ? new(1, 0, 0) : new(1, 0, 1);
            debugFSGraph.UpdateConditionless((float)(timeSinceLastFrame * 1000));
            renderingTicks.RenderConditionless();
            debugFSGraph.RenderConditionless();

            string[] results = RenderStatistics;
            debugText.drawWithHighlights = true;
            for (int i = 0; i < results.Length; i++)
            {
                string result = results[i];
                debugHolder.Write(result, 0, debugHolder.Height - debugText.characterHeight * (i + 1), 0.7f, new(1, 1, 1));
            }
            debugHolder.Write($"Camera position: {MathC.Round(Rendering.Camera.position, 2)}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 5), 0.7f, new(1, 1, 1));
            debugHolder.Write($"Camera front: {MathC.Round(Rendering.Camera.front, 2)}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 6), 0.7f, new(1, 1, 1));
            debugHolder.Write($"Selected scene: {SelectedScene}", 0, debugHolder.Height - debugText.characterHeight * (results.Length + 7), 0.7f, new(1, 1, 1));

            string msg = $"Threads reserved: {Job.ReservedThreads + 1}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight, 0.7f, new(1, 1, 1));
            msg = $"CPU usage: {CPUUsage}%";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 2, 0.7f, new(1, 1, 1));
            msg = $"Framecount: {totalFrameCount}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 3, 0.7f, new(1, 1, 1));
            msg = $"Frametime: {Math.Round(timeSinceLastFrame * 1000, 3)} ms";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 4, 0.7f, new(1, 1, 1));
            msg = $"FPS: {(int)(1 / timeSinceLastFrame)}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 5, 0.7f, new(1, 1, 1));
            msg = $"Errors caught: {errorsCaught}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 8, 0.7f, new(1, 1, 1));
            string status = AppIsHealthy ? "OK" : "BAD";
            msg = $"App status: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 9, 0.7f, new(1, 1, 1));
            status = keyIsPressed ? pressedKey.ToString() : mouseIsPressed ? pressedButton.ToString() : "None";
            msg = $"Input callback: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 11, 0.7f, new(1, 1, 1));
            msg = $"Render IDs: {renderToIDFramebuffer}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 13, 0.7f, new(1, 1, 1));
            status = renderToIDFramebuffer ? 1 / timeSinceLastFrame > refreshRate / 2 ? "OK" : "BAD" : "Unknown";
            msg = $"ID rendering performance: {status}";
            debugHolder.Write(msg, (int)(debugHolder.Width * 0.99f - debugText.GetStringWidth(msg, 0.7f)), debugHolder.Height - debugText.characterHeight * 14, 0.7f, new(1, 1, 1));
            debugText.drawWithHighlights = false;
        }

        public static void RenderVRAMStatistics()
        {
            if (firstTime)
            {
                int debugWidth = (int)debugText.GetStringWidth("Ticks spent depth sorting: timeSpentDepthSorting", 0.7f);
                debugFSGraph = new(0, debugWidth, (int)(debugText.characterHeight * 2), 0, (int)(monitorHeight - debugText.characterHeight * 0.7f * 4 - debugText.characterHeight * 2));
                debugFSGraph.showValues = false;
                firstTime = false;
            }

            string appStatus = AppIsHealthy ? "OK" : "BAD";
            debugText.RenderText($"Renderer: OpenGL {OpenGLVersion}     VRAM used: {Globals.FormatSizeToMB(UsedVRAM)} out of {Globals.FormatSizeToMB(AvaibleVRAM)}    resolution: {monitorWidth}x{monitorHeight}    status: {appStatus}", -monitorWidth / 2, monitorHeight / 2 - debugText.characterHeight, 1, new Vector3(0, 1, 0.1f));
            debugText.RenderText($"Protected rendering: {renderProtected}    Shadows: {renderShadows}    Quality: {Rendering.ShadowQuality}    Reflections: {renderReflections}    Quality: {Rendering.ReflectionQuality}    Texture quality: {Rendering.TextureQuality}", -monitorWidth / 2, monitorHeight / 2 - debugText.characterHeight * 2, 1, new Vector3(0, 1, 0.1f));
            debugText.RenderText($"FPS: {Math.Round(1 / FrameTime, 2)}", -monitorWidth / 2, monitorHeight / 2 - debugText.characterHeight * 3, 1f, new Vector3(0, 1, 0.1f));
            debugText.RenderText($"Frametime: {Math.Round(FrameTime * 1000, 2)} ms", -monitorWidth / 2 + debugText.characterHeight * 9, monitorHeight / 2 - debugText.characterHeight * 3, 1f, new Vector3(0, 1, 0.1f));
            debugText.RenderText($"Draw calls: {drawCallsPerFrame}", -monitorWidth / 2 + debugText.characterHeight * 22, monitorHeight / 2 - debugText.characterHeight * 3, 1f, new Vector3(0, 1, 0.1f));
            GL.glLineWidth(1);
            if (debugFSGraph.MaxValue > 70) debugFSGraph.MaxValue = (int)(timeSinceLastFrame * 1000 * 1.5f);
            debugFSGraph.color = 1 / timeSinceLastFrame < refreshRate / 2 ? new(1, 0, 0) : new(1, 0, 1);
            debugFSGraph.UpdateConditionless((float)(timeSinceLastFrame * 1000));
            debugFSGraph.RenderConditionless();
        }
    }
}
