using COREMath;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.Loaders;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security;
using static System.Net.Mime.MediaTypeNames;

namespace CORERenderer.GUI
{
    public class COREConsole
    {
        public static bool writeDebug = false, writeError = true;
        private bool canWriteToLog;
        private string logLocation;

        private Div quad;

        private int maxLines = 0;
        private int indexOfFirstLineToRender = 0, previousIOFLTR = 0;

        //if this needs to be optimised make it reuse the previous frames as a texture so the previous lines dont have to reprinted, saving draw calls
        private List<string> lines = new();
        private string LastLine { get { return lines[^1]; } set { lines[^1] = value; } }
        private string CurrentContext { get { return $"COREConsole/{currentContext} > "; } }
        private List<string> allCommands = new();

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
        }

        public void Update()
        {
            changed = changed ? changed : indexOfFirstLineToRender != previousIOFLTR; //only change changed if it isnt already true
            
            isInFocus = isInFocus ? COREMain.CheckAABBCollision(x, y, Width, Height) : COREMain.CheckAABBCollisionWithClick(x, y, Width, Height); //if the console is already in focus just check if the cursor is still in the console, otherwise check if the console is clicked on to make it in focus

            if (isInFocus)
            {
                indexOfFirstLineToRender -= (int)(COREMain.scrollWheelMovedAmount * 1.5);
                indexOfFirstLineToRender = indexOfFirstLineToRender >= 0 ? indexOfFirstLineToRender : 0; //IOFLTR cannot be smaller 0, since that would result in an index out of range error. otherwise apply the desired direction
                changed = COREMain.scrollWheelMovedAmount != 0;

                //deletes the last char of the input, unless it reached "> " indicating the begin of the input. It only deletes one char per press, if it didnt have a limit the entire input would be gone within a few milliseconds since it updates every frame
                if (Glfw.GetKey(COREMain.window, Keys.Backspace) == InputState.Press && !LastLine.EndsWith(CurrentContext) && !isPressedPrevious)
                {
                    LastLine = LastLine[..^1]; //replace the current version with a version of itself with the last char missing
                    changed = true;
                }

                if (Glfw.Time - previousTime > 0.06)
                {
                    previousTime = Glfw.Time;
                    if (!LastLine.StartsWith(CurrentContext) && Glfw.GetMouseButton(COREMain.window, MouseButton.Left) == InputState.Press)
                        WriteLine(CurrentContext);

                    if (Glfw.GetKey(COREMain.window, Keys.Enter) == InputState.Press && !LastLine.EndsWith(CurrentContext))
                        ParseInput(LastLine[(LastLine.IndexOf(CurrentContext) + CurrentContext.Length)..]);
                }
                if (COREMain.keyIsPressed)
                {
                    char letter;
                    try //catches exceptions in case the pressed key isnt the key to char dictionary
                    {
                        letter = Glfw.GetKey(COREMain.window, Keys.LeftShift) == InputState.Press ? Globals.keyShiftCharBinding[(int)COREMain.pressedKey] : Globals.keyCharBinding[(int)COREMain.pressedKey]; //if shift is pressed use the appropriate version of that key
                        if (letter != LastLine[^1] || Glfw.Time - previousTime2 > 0.15)
                        {
                            Write($"{letter}"); //adds letter to the last line of text
                            previousTime2 = Glfw.Time;
                        }
                    }
                    catch (System.Exception) //catches the error thrown if the pressed key isnt in the key char lut
                    {
                    }
                }
                isPressedPrevious = Glfw.GetKey(COREMain.window, Keys.Backspace) == InputState.Press;
            }
            previousIOFLTR = indexOfFirstLineToRender;
        }

        public void Wipe()
        {
            lines = new();
            indexOfFirstLineToRender = 0;
            changed = true;
        }

        public void Render()
        {
            if (!changed) //if no lines have been changed / added break, this improves performance a lot
                return;

            changed = false;

            quad.Render(); //renders background

            int sum = (int)(COREMain.debugText.characterHeight * 0.7f + 2); //rounding to an int makes it always render on top of a single pixel, instead of dividing into over multiple, which causes uglier looking letters //better to calculate here than every loop to save wasted performance
            int lineOffset = sum;

            bool original = COREMain.debugText.drawWithHighlights;
            int max = indexOfFirstLineToRender + maxLines > lines.Count ? lines.Count : indexOfFirstLineToRender + maxLines; //if there are less lines than the console can show it must only render those lines, otherwise it will try to render lines that dont exist
            for (int i = indexOfFirstLineToRender; i < max; i++, lineOffset += sum)
            {
                if (lines[i] == null)
                    continue;
                COREMain.debugText.drawWithHighlights = !lines[i].Contains('@') && !lines[i].StartsWith("ERROR ") && !lines[i].StartsWith("DEBUG ");
                Vector3 color = GetColorFromPrefix(lines[i], out string printResult);
                string suffix = i == lines.Count - 1 ? "|" : ""; //the | indicates the cursor. Only the last string has this

                quad.Write(printResult + suffix, 0, Height - lineOffset, 0.7f, color);
            }
            COREMain.debugText.drawWithHighlights = original;
        }

        private void WriteLineF(string text)
        {
            changed = true; //tells the console render everything again (could be optimised better by only rendering the newly added sentence / letters instead of every (already existing) sentence)
            indexOfFirstLineToRender = lines.Count - maxLines > 0 || indexOfFirstLineToRender < lines.Count - maxLines ? indexOfFirstLineToRender + 1 : indexOfFirstLineToRender;

            lines.Add(text);
        }

        public void WriteLine(object value)
        {
            bool containsToString = true;
            try
            {
                string test = value.ToString();
            }
            catch (System.Exception)
            {
                this.WriteError($"Couldn't get value of variable {nameof(value)}");
                return;
            }

            if (containsToString)
            {
                string end = value.ToString();
                string[] allResults = new string[] { end };

                if (end.Contains('\n'))
                    allResults = SeperateByNewLines(end);
                
                foreach (string s in allResults)
                {
                    string[] finals = SeperateByLength(s); //check if any string is too long to fit in the console
                    foreach (string s2 in finals)
                        this.WriteLineF(s2);
                }
            }
            else
                this.WriteError($"Couldn't get value of variable {nameof(value)}");
        }

        public string[] SeperateByNewLines(string end)
        {
            string prefix = GetPrefix(end); //decides if the base string is an error or debug by checking if it starts with its prefix

            List<int> newLineIndexes = new();
            for (int i = end.IndexOf('\n'); i != -1; i = end.IndexOf('\n', i + 1))
                newLineIndexes.Add(i);

            string[] seperatedLines = new string[newLineIndexes.Count + 1];
            for (int i = 0; i < newLineIndexes.Count; i++)
                seperatedLines[i] = i > 0 ? prefix + end[(newLineIndexes[i - 1] + 1)..newLineIndexes[i]] : end[..newLineIndexes[i]]; //the first string is seperated from the beginning to the first \n, like ..indexOf(\n). this needs to be done seperately, because it otherwise usees an index of -1 aka index of out range error (beginning of string index = 0, 0 - 1 = -1) 
            seperatedLines[^1] = newLineIndexes[^1] != end.Length - 1 && newLineIndexes[^1] != -1 ? prefix + end[(newLineIndexes[^1] + 1)..] : "";
            
            return seperatedLines;
        }

        private string[] SeperateByLength(string end)
        {
            float textWidth = COREMain.debugText.GetStringWidth(end, 0.7f);
            float tooBigPercentage = textWidth / Width;
            if (tooBigPercentage < 1)
                return new string[] { end };

            int stringLength = end.StartsWith("ERROR ") || end.StartsWith("DEBUG ") ? end.Length - 5 : end.Length; //DEBUG and ERROR arent rendered to the console so they need to be neglected when calculating if the string is too long
            string prefix = GetPrefix(end);

            int amountOfCharsForCorrectWidth = (int)(stringLength / tooBigPercentage);
            int timesTooBig = (int)tooBigPercentage;
            string[] seperations = new string[timesTooBig];
            for (int i = 0; i < timesTooBig; i++)
                seperations[i] = i == 0 ? end[..(amountOfCharsForCorrectWidth - 1)] : prefix + end[((amountOfCharsForCorrectWidth - 1) * i)..((amountOfCharsForCorrectWidth - 1) * (i + 1))];//end = end.Insert((amountOfCharsForCorrectWidth - 1) * (i + 1), "\n");
            return seperations;
        }

        private string GetPrefix(string end) => end.StartsWith("DEBUG ") ? "DEBUG " : end.StartsWith("ERROR ") ? "ERROR " : "";

        public void WriteLine() => WriteLine("");

        public void Write(string text)
        {
            lines[^1] += text; //add the new text to the to be overridden line
            changed = true;
        }

        public void WriteError(System.Exception err) => WriteError(err.ToString());

        public void WriteError(string err)
        {
            if (writeError)
                WriteLine("ERROR " + err);

            if (canWriteToLog)
            {
                using FileStream fs = new(logLocation, FileMode.Append);
                using StreamWriter sw = new(fs);
                sw.Write(err + "\n");
            }
        }

        public void WriteDebug(string debug)
        {
            if (writeDebug)
                WriteLine("DEBUG " + debug);
        }

        /// <summary>
        /// Renders the entire console even if nothing has changed, overriding the optimisation. This will make a draw call to the GPU for each letter, causing a severe performance drop
        /// </summary>
        public void RenderEvenIfNotChanged()
        {
            changed = true;
            Render();
        }

        public void RenderStatic() => Render();

        private Vector3 GetColorFromPrefix(string s, out string trimmedString)
        {
            if (s.StartsWith("ERROR "))
            {
                trimmedString = s[6..];
                return new(1, 0, 0);
            }

            else if (s.StartsWith("DEBUG "))
            {
                trimmedString = s[6..];
                return new(0.3f, 0.8f, 0.9f);
            }
            else
            {
                trimmedString = s;
                return new(1, 1, 1);
            }
        }

        public void GenerateConsoleErrorLog(string path)
        {
            FileStream fs = File.Create($"{path}\\consoleErrorLog.txt");
            fs.Close();
            canWriteToLog = true;
            logLocation = $"{path}\\consoleErrorLog.txt";
        }

        /// <summary>
        /// Writes all information gathered from the initial start up to the console
        /// </summary>
        public void ShowInfo()
        {
            int i = 0;
            using (FileStream fs = File.Open($"{COREMain.pathRenderer}\\logos\\logo.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
                for (string n = sr.ReadLine(); n != null; n = sr.ReadLine(), i++) //terribly written
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
                    else if (i == 12 && COREMain.LoadFilePath != null)
                        WriteLine($"{n}         Initialized from {Path.GetFileName(COREMain.LoadFilePath)}");
                    else if (i == 12 && COREMain.LoadFilePath == null)
                        WriteLine($"{n}         Initialized independently");
                    else if (i == 14)
                        WriteLine($"{n}         Rendering with {Rendering.shaderConfig} shaders");
                    else if (i == 15)
                        WriteLine($"{n}         Rendering with {COREMain.splashScreen.refreshRate} Hz");
                    else if (i ==16)
                        WriteLine($"{n}         Rendering resolution of {COREMain.monitorWidth}x{COREMain.monitorHeight}");
                    else
                        WriteLine(n);
                }
            WriteLine($"COREConsole/{currentContext} > ");
            indexOfFirstLineToRender += 2;
        }

        private enum Context
        {
            Scene,
            Camera,
            Console
        }


        public void ParseInput(string input)
        {
            allCommands.Add(input);

            if (input == "exit")
                Glfw.SetWindowShouldClose(COREMain.window, true);

            else if (input.Contains("save scene as"))
            {
                string filename = input[14..^4];
                Task.Run(() => Writers.GenerateCRS(COREMain.pathRenderer, filename, $"Generated by CORE-Renderer {COREMain.VERSION}, CRW {Readers.CURRENT_VERSION}", COREMain.CurrentScene.models.ToArray()));
            }

            else if (input == "save as stl") //debug
            {
                COREMain.CurrentScene.currentObj = 0;
                if (COREMain.CurrentScene.models.Count > 0 && COREMain.GetCurrentObjFromScene != -1)
                {
                    Writers.GenerateSTL(COREMain.pathRenderer, $"Written by CORE-Renderer {COREMain.VERSION}", COREMain.CurrentModel);
                    WriteDebug($"Generated {COREMain.CurrentModel.Name}.stl");
                }
                else
                    WriteError("There is no model to get data from");
            }
            else if (input == "save all as stl")
            {
                COREMain.MergeAllModels(out List<List<float>> vertices, out List<Vector3> offsets);
                Writers.GenerateSTL(COREMain.pathRenderer, "test", $"test.stl written by CORE-Renderer {COREMain.VERSION}", vertices, offsets);
            }

            else if (input == "goto console") //introducing contexts allows for better grouping of commands and better readability / makes it more expandable
                currentContext = Context.Console;
            else if (input == "goto camera")
                currentContext = Context.Camera;
            else if (input == "goto scene")
                currentContext = Context.Scene;

            else if (input.StartsWith("goto ") && input.Contains("->"))
            {
                string arg = input[5..input.IndexOf(' ', input.IndexOf(' ') + 1)];
                if (arg == "scene")
                    HandleSceneCommands(input[(input.IndexOf("-> ") + 3)..]);
                else if (arg == "camera")
                    HandleCameraCommands(input[(input.IndexOf("-> ") + 3)..]);
                else if (arg == "console")
                    HandleConsoleCommands(input[(input.IndexOf("-> ") + 3)..]);
                else
                    WriteError($"Couldn't solve argument reference {arg}");
            }
            else if (input == "reload config")
            {
                if (COREMain.LoadFilePath == null)
                    Process.Start("CORERenderer.exe");
                else
                    Process.Start("CORERenderer.exe", COREMain.LoadFilePath);
                GenerateCacheFile(COREMain.pathRenderer);
                Environment.Exit(1);
            }
            else if (input.Contains("set shaders"))
            {
                if (input.ToLower().Contains("pathtracing"))
                {
                    Rendering.shaderConfig = ShaderType.PathTracing;
                    COREMain.GenerateConfig();
                    if (COREMain.LoadFilePath == null)
                        Process.Start("CORERenderer.exe");
                    else
                        Process.Start("CORERenderer.exe", COREMain.LoadFilePath);
                    GenerateCacheFile(COREMain.pathRenderer);
                    Environment.Exit(1);
                }
                else if (input.ToLower().Contains("lighting"))
                {
                    Rendering.shaderConfig = ShaderType.Lighting;
                    COREMain.GenerateConfig();
                    if (COREMain.LoadFilePath == null)
                        Process.Start("CORERenderer.exe");
                    else
                        Process.Start("CORERenderer.exe", COREMain.LoadFilePath);
                    GenerateCacheFile(COREMain.pathRenderer);
                    Environment.Exit(1);
                }
                else if (input.ToLower().Contains("fullbright"))
                {
                    Rendering.shaderConfig = ShaderType.FullBright;
                    COREMain.GenerateConfig();
                    if (COREMain.LoadFilePath == null)
                        Process.Start("CORERenderer.exe");
                    else
                        Process.Start("CORERenderer.exe", COREMain.LoadFilePath);
                    GenerateCacheFile(COREMain.pathRenderer);
                    Environment.Exit(1);
                }
                else
                    WriteError("Couldn't find shader type");
            }
            else if (input == "recompile shaders")
            {
                if (COREMain.LoadFilePath == null)
                    Process.Start("CORERenderer.exe");
                else
                    Process.Start("CORERenderer.exe", COREMain.LoadFilePath);
                GenerateCacheFile(COREMain.pathRenderer);
                Environment.Exit(1);
            }

            else if (currentContext == Context.Console)
                HandleConsoleCommands(input);
            else if (currentContext == Context.Camera)
                HandleCameraCommands(input);
            else if (currentContext == Context.Scene)
                HandleSceneCommands(input);
            else
                WriteError($"Couldn't parse command \"{input}\"");

            WriteLine($"COREConsole/{currentContext} > ");
        }


        private void HandleCameraCommands(string input)
        { //every if statement checks the length of the input first, because if it tries the check the contents if the input like input[..4] == "get " it could throw an error if the input isnt 4 chars long (out of range). This is also prevented later on by using try { } catch (OutOfRangeException) { }
            if (input.Length > 4 && input[..4] == "get ") //keyword for displaying variable values
            {
                switch (input[4..]) 
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
            else if (input.Length > 4 && input[..4] == "set ") //keyword for changing the value of variables
            {
                if (input.IndexOf(' ', input.IndexOf(' ') + 1) == -1) //the syntax for setting a value is "set VARIALBE VALUE", so 2 spaces are needed, if those dont exist it should abort
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
            //delete
            if (input.Length > 7 && input[..7] == "delete ") //keyword for deleting one or more models
            {
                if (input.Length > 13 && input[..13] == "delete model[") //allows the removal of a single object
                {
                    int index;
                    bool succeeded = int.TryParse(input[13..input.IndexOf(']')], out index); //finds the index of the model
                    if (succeeded && index < COREMain.scenes[COREMain.selectedScene].models.Count) //only continue if the index is found and its within range
                    {
                        try
                        {
                            COREMain.scenes[COREMain.selectedScene].models[index].Dispose();
                            WriteLine($"Deleted model {index}");
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1; //making no models highlighted to prevent crashes
                        }
                        catch (IndexOutOfRangeException)
                        {
                            WriteLine($"Couldn't delete model {input[13..input.IndexOf(']')]}");
                        }
                    }
                    else
                        WriteLine($"Couldn't delete model {input[13..input.IndexOf(']')]}");
                }
                else if (input.Length > 13 && input[..13] == "delete models") //allows the removal of multiple objects at once with array-like indexing ([1..4] to delete the second model till the 4th for example)
                {
                    bool succeeded = int.TryParse(input[14..input.IndexOf('.')], out int index1); //gets the value if the first index
                    int index2;
                    bool succeeded2 = int.TryParse(input[(input.IndexOf("..") + 2)..input.IndexOf(']')], out index2); //gets the value if the second index by looking for a value between '..' and ']' in [VALUE..VALUE]
                    if (succeeded && succeeded2) //only continue if both indexes are found
                    {
                        for (int i = index1; i <= index2; i++) //iterate through every index between the found indexes
                        {
                            try
                            {
                                if (i == index1)
                                    WriteLine($"Deleted model {i}"); //simple way of showing which models are deleted
                                else
                                    Write($"..{i}");
                                COREMain.scenes[COREMain.selectedScene].models[i].Dispose();
                            }
                            catch (IndexOutOfRangeException)
                            {
                                WriteLine($"Couldn't delete model {i}");
                            }
                            COREMain.scenes[COREMain.selectedScene].currentObj = -1; //making no models highlighted to prevent crashes
                        }
                    }
                    else
                        WriteLine($"Couldn't delete models {index1} through {index2}");
                }
            }

            //load
            else if (input.Length > 5 && input[..5] == "load ") //keyword for loading in a file with given path
            {
                if (input.Length > 12 && input[5..9] == "dir ")
                {
                    string dir = input[9..];
                    if (dir[..4] == "this" && COREMain.LoadFilePath != null) //allows the use of 'this' as an alias for the directory it was opened in
                        dir = Path.GetDirectoryName(COREMain.LoadFilePath);
                    else if (dir[..4] == "this" && COREMain.LoadFilePath == null) //throws an error if it wasnt opened in a directory but 'this' is used
                    {
                        WriteError($"\"{dir}\" isn't valid here");
                        return;
                    }
                    else if (dir[..5] == "$PATH") //allows '$PATH' to be used as an alias for the base of the directory that the .exe is located
                        dir = COREMain.pathRenderer + dir[5..];
                    
                    if (Directory.Exists(dir)) //checks if the given directory is valid
                    {
                        COREMain.LoadDir(dir);
                        WriteLine($"Loaded directory {dir}");
                    }
                    else
                        WriteError($"Couldn't find the directory at {dir}");
                }  
                else if (input[5..9] != "dir ") //checks if user wants to load in an directory or a single file
                {
                    string dir = input[5..];
                    if (dir[..5] == "$PATH")
                        dir = COREMain.pathRenderer + dir[5..];
                    if (File.Exists(dir) && dir[^4..] == ".obj" || dir[^4..] == ".hdr" || dir[^4..] == ".stl" || dir[^4..] == ".png" || dir[^4..] == ".jpg") //only allows certain file types, in this case .obj and .hdr
                    {
                        COREMain.scenes[COREMain.selectedScene].models.Add(new(dir));
                        WriteLine("Loaded file");
                    }
                    else
                        WriteError($"Invalid file at {dir}");
                }
                else
                    WriteError($"Couldn't find the file at {input[5..]}");
            }

            //get
            else if (input.Length > 4 && input[..4] == "get ") //keyword for displaying variable values
            {
                switch (input[4..])
                {
                    case "model count":
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].models.Count}");
                        break;
                    case "submodel count":
                        int count = 0;
                        foreach (Model model in COREMain.scenes[COREMain.selectedScene].models)
                            count += model.submodels.Count;
                        WriteLine($"{count}");
                        break;
                    case "draw calls":
                        WriteLine($"{COREMain.drawCallsPerFrame}");
                        break;
                }
            }

            //set
            else if (input.Length > 4 && input[..4] == "set ") //keyword for displaying variable values
            {
                if (input[5..].IndexOf(' ') + 5 == -1)
                    return;
                switch (input[4..(input[5..].IndexOf(' ') + 5)])
                {
                    case "useRenderDistance":
                        if (input.Contains("true"))
                        {
                            Submodel.useRenderDistance = true;
                            WriteLine($"Set variable to true");
                        }
                            
                        else if (input.Contains("false"))
                        {
                            Submodel.useRenderDistance = false;
                            WriteLine($"Set variable to false");
                        }
                        else
                            WriteLine("Couldn't find a bool value");
                        break;
                    case "renderDistance":
                        ChangeValue(ref Submodel.renderDistance, input[18..]);
                        WriteLine($"Set variable to {Submodel.renderDistance}");
                        break;
                    default:
                        WriteError($"Couldn't parse input {input[4..input[5..].IndexOf(' ')]}");
                        break;
                }
            }

            //move
            else if (input.Length > 13 && input[..4] == "move")
            {
                try
                {
                    if (input.Contains("model["))
                    {
                        int indexOfSymbol = input.IndexOf(']'); //better performance
                        bool succeeded = int.TryParse(input[(input.IndexOf('[') + 1)..indexOfSymbol], out int modelIndex);
                        if (!succeeded)
                        {
                            WriteError($"Couldn't find model index in {input}");
                            return;
                        }
                        Model model = COREMain.CurrentScene.models[modelIndex];
                        
                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input[(indexOfSymbol + 1)..]);
                        if (input[4..6] == "to") //sorts for moveto, this value is applied absolutely
                        {
                            Vector3 distance = location - model.Transform.translation;
                            model.Transform.translation += distance;
                            WriteLine($"Moved model to {location.x}, {location.y}, {location.z}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            model.Transform.translation += location;
                            WriteLine($"Moved model with {location.x}, {location.y}, {location.z}");
                        }
                    }
                    else if (input.Contains("models"))
                    {
                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input);
                        if (input[4..6] == "to") //sorts for moveto, this value is applied absolutely
                        {
                            Vector3 distance = location - COREMain.CurrentScene.models[0].Transform.translation;
                            foreach (Model model in COREMain.CurrentScene.models)
                                model.Transform.translation += distance;
                            WriteLine($"Moved model to {location.x}, {location.y}, {location.z}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            foreach (Model model in COREMain.CurrentScene.models)
                                    model.Transform.translation += location;
                            WriteLine($"Moved model with {location.x}, {location.y}, {location.z}");
                        }
                    }
                    
                }
                catch (IndexOutOfRangeException)
                {
                    WriteError("Given index is out of range");
                    return;
                }
            }

            else if (input.Length > 13 && input == "disable arrows")
            {
                Arrows.disableArrows = true;
                WriteLine("Disabled arrows");
            }

            else if (input.Length > 13 && input == "enable arrows")
            {
                Arrows.disableArrows = false;
                WriteLine("Enabled arrows");
            }

            else
                WriteError($"Couldn't parse scene command \"{input}\"");
        }

        private void HandleConsoleCommands(string input)
        {
            if (input == "get info")
                ShowInfo();
            else if (input == "clear GUI") //clears the GUI framebuffer, can leave artifacts behind
            {
                GL.glClearColor(0.085f, 0.085f, 0.085f, 1);
                GL.glClear(GL.GL_COLOR_BUFFER_BIT); 
                WriteLine("Cleared framebuffer");
            }
                
            else if (input == "wipe") //wipes the console clean
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

        public void LoadCacheFile(string dirPath)
        {
            if (!File.Exists($"{dirPath}\\consoleCache"))
            {
                WriteError("Failed to retrieve cache");
                return;
            }

            string[] commands = File.ReadAllLines($"{dirPath}\\consoleCache");
            foreach (string command in commands)
                ParseInput(command);

            File.Delete($"{dirPath}\\consoleCache");
            WriteDebug("Succeeded retrieved and loaded cache");
        }

        private void GenerateCacheFile(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                WriteError($"Failed to create a cache");
                return;
            }

            using StreamWriter sw = File.CreateText($"{dirPath}\\consoleCache");
            foreach (string command in allCommands)
                if (command != "exit" && !command.Contains("reload") && !command.Contains("set shaders") && !command.Contains("recompile shaders"))
                    sw.WriteLine(command);
        }
    }
}