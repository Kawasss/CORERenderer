using COREMath;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.Main;
using System;

namespace CORERenderer.GUI
{
    public class COREConsole
    {
        private Div quad;

        private int linesPrinted = 0;
        private int maxLines = 0;
        
        //if this needs to be optimised make it reuse the previous frames as a texture so the previous lines dont have to reprinted, saving draw calls
        private string[] lines;

        private int Width;
        private int Height;
        private int x, y;

        public bool changed = true;

        public bool isInFocus = false;

        private Context currentContext = Context.Console;

        private double previousTime = 0;
        private double previousTime2 = 0;

        private bool isPressedPrevious = false;

        public COREConsole(int width, int height, int x, int y)
        {
            quad = new(width, height, x, y);

            Width = width;
            Height = height;

            this.x = x;
            this.y = y;

            maxLines = height / (int)(COREMain.debugText.characterHeight * 0.7f + 2);

            lines = new string[maxLines];
        }

        public void Update()
        {
            if (!isInFocus)
                isInFocus = COREMain.CheckAABBCollisionWithClick(x, y, Width, Height);
            if (isInFocus)
                isInFocus = COREMain.CheckAABBCollision(x, y, Width, Height);
            if (isInFocus)
            {
                if (Glfw.GetKey(COREMain.window, Keys.Backspace) == InputState.Press && lines[linesPrinted - 1][^2..] != "> " && !isPressedPrevious)
                {
                    lines[linesPrinted - 1] = lines[linesPrinted - 1][..^1];
                    changed = true;
                }

                if (Glfw.Time - previousTime > 0.06)
                {
                    previousTime = GLFW.Glfw.Time;

                    if (lines[linesPrinted - 1].Length >= 11)
                    {
                        if (lines[linesPrinted - 1][..11] != "COREConsole")
                            WriteLine($"COREConsole/{currentContext} > ");
                    }
                    else
                        WriteLine($"COREConsole/{currentContext} > ");
                    if (Glfw.GetKey(COREMain.window, Keys.Enter) == InputState.Press && lines[linesPrinted - 1][^2..] != "> ")
                        ParseInput(lines[linesPrinted - 1][(lines[linesPrinted - 1].IndexOf("> ") + 2)..]);
                }
                if (COREMain.keyIsPressed)
                {
                    char letter;
                    try
                    {
                        letter = Globals.keyCharBinding[(int)COREMain.pressedKey];
                        if (Glfw.GetKey(COREMain.window, Keys.LeftShift) == InputState.Press)
                            letter = letter.ToString().ToUpper()[0];
                        if ((letter != lines[linesPrinted - 1][^1] || Glfw.Time - previousTime2 > 0.15) && Glfw.Time - previousTime2 > 0.003)
                        {
                            Write($"{letter}");
                            previousTime2 = Glfw.Time;
                        }
                    }
                    catch (System.Exception)
                    {
                    }
                }
                isPressedPrevious = Glfw.GetKey(COREMain.window, Keys.Backspace) == InputState.Press;
            }
        }

        public void Wipe()
        {
            lines = new string[maxLines];
            linesPrinted = 0;
            changed = true;
        }

        public void Render()
        {
            if (!changed)
                return;

            changed = false;

            quad.Render();
            
            int lineOffset = (int)(COREMain.debugText.characterHeight * 0.7f + 2); //scale = 1
            for (int i = 0; i < linesPrinted; i++, lineOffset += (int)(COREMain.debugText.characterHeight * 0.7f + 2))
            {
                if (lines[i].Length < 5 || lines[i][..5] != "ERROR")
                    quad.Write(lines[i], 0, Height - lineOffset, 0.7f);
                else
                    quad.WriteError(lines[i], 0, Height - lineOffset, 0.7f);
            }
        }

        public void WriteLine(string text)
        {
            changed = true;

            linesPrinted++;
            if (linesPrinted > maxLines)
            {
                linesPrinted--;
                string[] oldLines = lines;
                lines = new string[maxLines];
                for (int i = 0; i < maxLines - 1; i++)
                    lines[i] = oldLines[i + 1];
            }
            //seperates the text if its longer than the quad
            if (text.Length < Height / (COREMain.debugText.characterHeight / 4))
            {
                lines[linesPrinted - 1] = text;
                return;
            }
            for (int i = 0; i < text.Length; i += (int)(Height / (COREMain.debugText.characterHeight / 4)))
            {
                if (text[i..].Length > Height / (COREMain.debugText.characterHeight * 0.7f))
                {
                    lines[linesPrinted - 1] = $"{text[i..(i + Height / (int)(COREMain.debugText.characterHeight / 4))]}";
                    linesPrinted++;
                }
                else
                {
                    if (linesPrinted > maxLines)
                    {
                        linesPrinted--;
                        string[] oldLines = lines;
                        lines = new string[maxLines];
                        for (int j = 0; j < maxLines - 1; j++)
                            lines[j] = oldLines[j + 1];
                    }
                    lines[linesPrinted - 1] = text[i..^1];
                    return;
                }
            }
        }

        public void Write(string text)
        {
            linesPrinted--;
            WriteLine(lines[linesPrinted] + text);
        }

        public void WriteError(System.Exception err) => WriteError(err.ToString());

        public void WriteError(string err)
        {
            changed = true;

            linesPrinted++;
            string text = "ERROR " + err;
            if (linesPrinted > maxLines)
            {
                linesPrinted--;
                string[] oldLines = lines;
                lines = new string[maxLines];
                for (int i = 0; i < maxLines - 1; i++)
                    lines[i] = oldLines[i + 1];
            }
            //seperates the text if its longer than the quad
            if (text.Length < Height / (COREMain.debugText.characterHeight / 4))
            {
                lines[linesPrinted - 1] = text;
                return;
            }
            for (int i = 0; i < text.Length; i += (int)(Height / (COREMain.debugText.characterHeight / 3)))
            {
                if (text[i..^1].Length > Height / (COREMain.debugText.characterHeight / 3))
                {
                    lines[linesPrinted - 1] = $"ERROR {text[i..(i + Height / (int)(COREMain.debugText.characterHeight / 3))]}";
                    linesPrinted++;
                }
                else
                {
                    if (linesPrinted > maxLines)
                    {
                        linesPrinted--;
                        string[] oldLines = lines;
                        lines = new string[maxLines];
                        for (int j = 0; j < maxLines - 1; j++)
                            lines[j] = oldLines[j + 1];
                    }
                    lines[linesPrinted - 1] = "ERROR " + text[i..^1];
                    return;
                }
            }
        }

        public void RenderEvenIfNotChanged()
        {
            changed = true;
            Render();
        }

        public void RenderStatic() => Render();

        public void ShowInfo()
        {
            int i = 0;
            Wipe();
            using (FileStream fs = File.Open($"{COREMain.pathRenderer}\\logos\\logo.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
                for (string n = sr.ReadLine(); n != null; n = sr.ReadLine(), i++)
                {
                    if (i == 3)
                        WriteLine($"{n}         CORE Renderer {COREMain.VERSION}");
                    else if (i == 4)
                        WriteLine($"{n}         CORE Math {MathC.VERSION}");
                    else if (i == 6)
                        WriteLine($"{n}         GPU: {COREMain.GPU}");
                    else if (i == 7)
                        WriteLine($"{n}         OpenGL {OpenGL.GL.glGetString(OpenGL.GL.GL_VERSION)}");
                    else if (i == 8)
                        WriteLine($"{n}         GLSL {OpenGL.GL.glGetString(OpenGL.GL.GL_SHADING_LANGUAGE_VERSION)}");
                    else if (i == 10)
                        WriteLine($"{n}         Active for {Math.Round(Glfw.Time)} seconds");
                    else if (i == 11)
                        WriteLine($"{n}         Initialized with {COREMain.LoadFile}");
                    else if (i == 13)
                        WriteLine($"{n}         Rendering with default shaders");
                    else if (i == 14)
                        WriteLine($"{n}         Rendering with {COREMain.splashScreen.refreshRate} Hz");
                    else
                        WriteLine(n);
                }
        }

        private enum Context
        {
            Scene,
            Camera,
            Console
        }


        private void ParseInput(string input)
        {
            if (input == "exit")
                Glfw.SetWindowShouldClose(COREMain.window, true);
            else if (input == "goto console")
            {
                currentContext = Context.Console;
                WriteLine($"COREConsole/{currentContext} > ");
            }
            else if (input == "goto camera")
            {
                currentContext = Context.Camera;
                WriteLine($"COREConsole/{currentContext} > ");
            }
            else if (input == "goto scene")
            {
                currentContext = Context.Scene;
                WriteLine($"COREConsole/{currentContext} > ");
            }
            else if (currentContext == Context.Console)
                HandleConsoleCommands(input);
            else if (currentContext == Context.Camera)
                HandleCameraCommands(input);
            else if (currentContext == Context.Scene)
                HandleSceneCommands(input);
            else
                WriteError($"Couldn't parse command \"{input}\"");
        }


        private void HandleCameraCommands(string input)
        {
            if (input.Length > 5 && input[..5] == "show ")
            {
                switch (input[5..]) 
                {
                    case "speed":
                        WriteLine($"{Camera.cameraSpeed}");
                        break;
                    case "fov":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].camera.Fov}");
                        break;
                    case "yaw":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].camera.Yaw}");
                        break;
                    case "pitch":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].camera.Pitch}");
                        break;
                    case "farplane":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].camera.FarPlane}");
                        break;
                    case "nearplane":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].camera.NearPlane}");
                        break;
                    case "position":
                        Vector3 location = COREMain.scenes[COREMain.selectedScene].camera.position;
                        WriteLine($"{location.x}, {location.y}, {location.z}");
                        break;
                    default:
                        WriteError($"Unknown variable: \"{input[5..]}\"");
                        break;
                }
            }
            else if (input.Length > 4 && input[..4] == "set ")
            {
                if (input.IndexOf(' ', input.IndexOf(' ') + 1) == -1)
                {
                    WriteError($"Unknown variable: \"{input[4..]}\"");
                    return;
                }
                switch (input[4..input.IndexOf(' ', input.IndexOf(' ') + 1)])
                {
                    case "speed":
                        ChangeValue(ref Camera.cameraSpeed, input[9..]);
                        break;
                    case "fov":
                        float result = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result, input[7..]);
                        COREMain.scenes[COREMain.selectedScene].camera.Fov = result;
                        break;
                    case "yaw":
                        float result2 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result2, input[7..]);
                        COREMain.scenes[COREMain.selectedScene].camera.Yaw = result2;
                        break;
                    case "pitch":
                        float result3 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result3, input[9..]);
                        COREMain.scenes[COREMain.selectedScene].camera.Pitch = result3;
                        break;
                    case "farplane":
                        float result4 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result4, input[12..]);
                        COREMain.scenes[COREMain.selectedScene].camera.FarPlane = result4;
                        break;
                    case "nearplane":
                        float result5 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result5, input[13..]);
                        COREMain.scenes[COREMain.selectedScene].camera.NearPlane = result5;
                        break;
                    default:
                        WriteError($"Unknown variable: \"{input[4..input.IndexOf(' ', input.IndexOf(' ') + 1)]}\"");
                        break;
                }
            }
            else
                WriteError($"Couldn't parse camera command \"{input}\"");
        }
        private void HandleSceneCommands(string input)
        {
            if (input.Length > 7 && input[..7] == "delete ")
            {
                if (input.Length > 13 && input[..13] == "delete model[")
                {
                    int index;
                    bool succeeded = int.TryParse(input[13..input.IndexOf(']')], out index);
                    if (succeeded && index < COREMain.scenes[COREMain.selectedScene].allModels.Count)
                    {
                        try
                        {
                            COREMain.scenes[COREMain.selectedScene].allModels[index].Dispose();
                            WriteLine($"Deleted model {index}");
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            WriteLine($"Couldn't delete model {input[13..input.IndexOf(']')]}");
                        }
                    }
                    else
                        WriteLine($"Couldn't delete model {input[13..input.IndexOf(']')]}");
                }
                else if (input.Length > 13 && input[..13] == "delete models")
                {
                    int index1;
                    bool succeeded = int.TryParse(input[14..input.IndexOf('.')], out index1);
                    int index2;
                    bool succeeded2 = int.TryParse(input[(input.IndexOf("..") + 2)..input.IndexOf(']')], out index2);
                    WriteLine(input[(input.IndexOf("..") + 2)].ToString());
                    if (succeeded && succeeded2)
                    {
                        for (int i = index1; i <= index2; i++)
                        {
                            try
                            {
                                if (i == 0)
                                    WriteLine($"Deleted model {i}");
                                else
                                    Write($"..{i}");
                                COREMain.scenes[COREMain.selectedScene].allModels[i].Dispose();
                            }
                            catch (IndexOutOfRangeException)
                            {
                                WriteLine($"Couldn't delete model {i}");
                            }
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1;
                        }
                    }
                    else
                        WriteLine($"Couldn't delete models {index1} through {index2}");
                }
            }
            else if (input.Length > 5 && input == "load ")
            {
                if (input[5..] == "dir")
                    COREMain.renderEntireDir = true;//COREMain.menu.SetBool("Load entire directory", true);
                else if (File.Exists(input[5..]))
                    COREMain.scenes[COREMain.selectedScene].allModels.Add(new(input[5..]));
                else
                    WriteLine($"Couldn't find the file at {input[5..]}");
            }
            else if (input.Length > 5 && input[..5] == "show ")
            {
                switch (input[5..])
                {
                    case "model count":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].allModels.Count}");
                        break;
                }
            }
            else
                WriteError($"Couldn't parse scene command \"{input}\"");
        }

        private void HandleConsoleCommands(string input)
        {
            if (input == "show info")
                ShowInfo();
            else if (input == "wipe")
            {
                Wipe();
                WriteLine($"COREConsole/{currentContext} > ");
            }
            else
                WriteError($"Couldn't parse console command \"{input}\"");
        }

        private void ChangeValue(ref float variable, string input)
        {
            bool succeeded = float.TryParse(input, out float result);
            if (succeeded)
            {
                variable = result;
                WriteLine($"Set variable to {variable}");
            }
            else
                WriteError($"Couldn't parse float {input[4..]}");
        }
    }
}
