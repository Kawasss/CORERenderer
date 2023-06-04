using CORERenderer.OpenGL;
using CORERenderer.shaders;
using static CORERenderer.Main.COREMain;
using Console = CORERenderer.GUI.Console;

namespace CORERenderer.Main
{
    internal static class Config
    {
        internal static void LoadConfig()
        {
            if (!File.Exists($"{BaseDirectory}\\config"))
            {
                Console.WriteError($"Couldn't locate config, generating new config");
                GenerateConfig();
                return;
            }

            using (FileStream fs = File.Open($"{BaseDirectory}\\config", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                string text = sr.ReadLine();

                if (VERSION != text[(text.IndexOf('=') + 1)..])
                {
                    Console.WriteError($"Config is outdated, generating new config");
                    GenerateConfig();
                    return;
                }

                text = sr.ReadLine();

                if (text.Contains("Lighting"))
                    Rendering.shaderConfig = ShaderType.PBR;
                else if (text.Contains("PathTracing"))
                    Rendering.shaderConfig = ShaderType.PathTracing;
                else if (text.Contains("FullBright"))
                    Rendering.shaderConfig = ShaderType.FullBright;

                text = sr.ReadLine();

                Camera.cameraSpeed = float.Parse(text[(text.IndexOf('=') + 1)..]);
                Console.writeDebug = sr.ReadLine().Contains("True");
                Console.writeError = sr.ReadLine().Contains("True");
                loadInfoOnstartup = sr.ReadLine().Contains("True");
                Shader.WriteErrors = sr.ReadLine().Contains("False");

                Console.WriteDebug("Loaded config file");
            }
        }

        internal static void GenerateConfig()
        {
            using (StreamWriter sw = File.CreateText($"{BaseDirectory}\\config"))
            {
                sw.WriteLine($"version={VERSION}");
                sw.WriteLine($"shaders={Rendering.shaderConfig}");
                sw.WriteLine($"cameraSpeed={Camera.cameraSpeed}");
                sw.WriteLine($"writedebug={Console.writeDebug}");
                sw.WriteLine($"writeerror={Console.writeError}");
                sw.WriteLine($"loadinfo={loadInfoOnstartup}");
                sw.WriteLine($"MuteShaderErrors={!Shader.WriteErrors}");
            }
            Console.WriteDebug("Generated new config");
        }
    }
}
