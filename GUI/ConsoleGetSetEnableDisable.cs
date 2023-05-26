using CORERenderer.Loaders;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using COREMath;
using System.Globalization;

namespace CORERenderer.GUI
{
    public partial class Console
    {
        private static void CameraGetCommand(string[] input)
        {
            switch (input[1])
            {
                case "speed":
                    WriteLine($"{Camera.cameraSpeed}");
                    break;
                case "fov":
                    WriteLine($"{COREMain.CurrentScene.camera.Fov}");
                    break;
                case "yaw":
                    WriteLine($"{COREMain.CurrentScene.camera.Yaw}");
                    break;
                case "pitch":
                    WriteLine($"{COREMain.CurrentScene.camera.Pitch}");
                    break;
                case "farplane":
                    WriteLine($"{COREMain.CurrentScene.camera.FarPlane}");
                    break;
                case "nearplane":
                    WriteLine($"{COREMain.CurrentScene.camera.NearPlane}");
                    break;
                case "position":
                    WriteLine($"{COREMain.CurrentScene.camera.position}");
                    break;
                default:
                    WriteError($"Unknown variable: \"{input[5..]}\"");
                    break;
            }
        }

        private static void CameraSetCommand(string[] input)
        {
            switch (input[1])
            {
                case "speed":
                    ChangeValue(ref Camera.cameraSpeed, input[2]);
                    break;
                case "fov":
                    Rendering.Camera.Fov = float.TryParse(input[2], NumberStyles.Any, null, out float result) ? result : Rendering.Camera.Fov; //set the variable to the given value. if the value can be parsed dont change the variable
                    break;
                case "yaw":
                    Rendering.Camera.Yaw = float.TryParse(input[2], NumberStyles.Any, null, out float result2) ? result2 : Rendering.Camera.Yaw;
                    break;
                case "pitch":
                    Rendering.Camera.Pitch = float.TryParse(input[2], NumberStyles.Any, null, out float result3) ? result3 : Rendering.Camera.Pitch;
                    break;
                case "farplane":
                    Rendering.Camera.FarPlane = float.TryParse(input[2], NumberStyles.Any, null, out float result4) ? result4 : Rendering.Camera.FarPlane;
                    break;
                case "nearplane":
                    Rendering.Camera.NearPlane = float.TryParse(input[2], NumberStyles.Any, null, out float result5) ? result5 : Rendering.Camera.NearPlane;
                    break;
                default:
                    WriteError($"Unknown variable: \"{input[1]}\"");
                    break;
            }
        }

        private static void SceneGetCommand(string[] input)
        {
            switch (input[1])
            {
                case "model count":
                    WriteLine($"{COREMain.CurrentScene.models.Count}");
                    break;
                case "submodel count":
                    int count = 0;
                    foreach (Model model in COREMain.CurrentScene.models)
                        count += model.submodels.Count;
                    WriteLine($"{count}");
                    break;
                case "draw calls":
                    WriteLine($"{COREMain.drawCallsPerFrame}");
                    break;
                case "shader":
                    uint handle = uint.Parse(input[2]);
                    if (input[3] != "log")
                    {
                        WriteError($"invalid argument after \"shader {handle}\", this only accepts \"log\"");
                        return;
                    }
                    WriteLine(shaders.Shader.HandleShaderPair[handle].StartLog);
                    break;
                case "texture":
                    int index = int.Parse(input[2]);
                    WriteLine(GetTextureInfo(input[3], Globals.usedTextures[index]));
                    break;
            }
        }

        private static void SceneSetCommand(string[] input)
        {
            if (input[1].Contains("lights")) //cant fit in switch case because lights is only part of the parsed input
            {
                if (input[1].Contains("position"))
                {
                    Vector3 pos = new(input[2], input[3], input[4]);
                    COREMain.CurrentScene.lights[Readers.GetOneIntWithRegEx(input[1])].SetPosition(pos);
                }
                else if (input[1].Contains("color"))
                {
                    Vector3 color = new(input[2], input[3], input[4]);
                    COREMain.CurrentScene.lights[Readers.GetOneIntWithRegEx(input[1])].SetColor(color);
                }
                return;
            }

            switch (input[1])
            {
                case "reflectionQuality":
                    Rendering.ShadowQuality = Readers.GetOneFloatWithRegEx(input[2]);
                    WriteLine($"Set reflection quality to {Rendering.ShadowQuality}");
                    break;
                case "textureQuality":
                    Rendering.TextureQuality = Readers.GetOneFloatWithRegEx(input[2]);
                    WriteLine($"Set texture quality to {Rendering.TextureQuality}");
                    break;

                default:
                    WriteError($"Couldn't parse input {input[1]}");
                    break;
            }
        }
        private static void SceneEnableDisable(string[] input)
        {
            switch (input[1])
            {
                case "arrows":
                    Arrows.disableArrows = input[0] == "enable"; //if the command is enable the value must be set to true, so if it isnt enable (aka disable) its false. value also becomes values if the 2nd input is gibberish but that issue is too minor to care about
                    WriteLine("Disabled arrows");
                    break;
                case "lights":
                    Rendering.renderLights = input[0] == "enable";
                    WriteLine("Lights aren't being rendered anymore");
                    break;
                case "grid":
                    COREMain.renderGrid = input[0] == "enable";
                    WriteLine("Grid isn't being rendered anymore");
                    break;
            }
        }
    }
}
