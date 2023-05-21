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
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace CORERenderer.GUI
{
    public class Console
    {
        public static bool writeDebug = false, writeError = true;

        private Div quad;

        private int maxLines = 0;
        private static int indexOfFirstLineToRender = 0, previousIOFLTR = 0;

        //if this needs to be optimised make it reuse the previous frames as a texture so the previous lines dont have to reprinted, saving draw calls
        private static List<string> lines = new();
        private string LastLine { get { return lines[^1]; } set { lines[^1] = value; } }
        private string CurrentContext { get { return $"COREConsole/{currentContext} > "; } }
        private List<string> allCommands = new();
        private static Dictionary<string, int> amountOfAppearancesLine = new();

        private int Width, Height, x, y;

        public static bool changed = true;
        private static bool canWriteToLog = false;
        private static string logLocation;

        public bool isInFocus = false;

        private Context currentContext = Context.Console;

        private double previousTime = 0;
        private double previousTime2 = 0;

        private bool isPressedPrevious = false;

        public Console(int width, int height, int x, int y)
        {
            quad = new(width, height, x, y);

            Width = width;
            Height = height;

            this.x = x;
            this.y = y;

            maxLines = height / (int)(Main.COREMain.debugText.characterHeight * 0.7f + 2);
        }

        private int previousLineCount = 0;
        public void Update()
        {
            changed = changed ? changed : indexOfFirstLineToRender != previousIOFLTR; //only change changed if it isnt already true

            isInFocus = isInFocus ? Main.COREMain.CheckAABBCollision(x, y, Width, Height) : Main.COREMain.CheckAABBCollisionWithClick(x, y, Width, Height); //if the console is already in focus just check if the cursor is still in the console, otherwise check if the console is clicked on to make it in focus

            if (previousLineCount < lines.Count)
                indexOfFirstLineToRender = lines.Count - maxLines;

            if (isInFocus)
            {
                indexOfFirstLineToRender -= (int)(Main.COREMain.scrollWheelMovedAmount * 1.5);
                indexOfFirstLineToRender = indexOfFirstLineToRender >= 0 ? indexOfFirstLineToRender : 0; //IOFLTR cannot be smaller 0, since that would result in an index out of range error. otherwise apply the desired direction
                changed = Main.COREMain.scrollWheelMovedAmount != 0;

                CheckIfKeyNeedsToBeDeleted();

                CheckForCommands();

                CheckForUserInput();
            }
            previousIOFLTR = indexOfFirstLineToRender;
            previousLineCount = lines.Count;
        }

        private void CheckForCommands()
        {
            if (Glfw.Time - previousTime > 0.06)
            {
                previousTime = Glfw.Time;
                if (!LastLine.StartsWith(CurrentContext) && Glfw.GetMouseButton(Main.COREMain.window, MouseButton.Left) == InputState.Press)
                    WriteLine(CurrentContext);

                if (Glfw.GetKey(Main.COREMain.window, Keys.Enter) == InputState.Press && !LastLine.EndsWith(CurrentContext))
                    ParseInput(LastLine[(LastLine.IndexOf(CurrentContext) + CurrentContext.Length)..]);
            }
        }

        private void CheckForUserInput()
        {
            if (!Main.COREMain.keyIsPressed || !Globals.keyCharBinding.ContainsKey((int)Main.COREMain.pressedKey) || !Globals.keyShiftCharBinding.ContainsKey((int)Main.COREMain.pressedKey))
            {
                isPressedPrevious = Glfw.GetKey(Main.COREMain.window, Keys.Backspace) == InputState.Press;
                return;
            }
                
            char letter = Glfw.GetKey(Main.COREMain.window, Keys.LeftShift) == InputState.Press ? Globals.keyShiftCharBinding[(int)Main.COREMain.pressedKey] : Globals.keyCharBinding[(int)Main.COREMain.pressedKey]; //if shift is pressed use the appropriate version of that key
            if (letter != LastLine[^1] || Glfw.Time - previousTime2 > 0.15)
            {
                Write($"{letter}"); //adds letter to the last line of text
                previousTime2 = Glfw.Time;
            }
        }

        private void CheckIfKeyNeedsToBeDeleted()
        {
            //deletes the last char of the input, unless it reached "> " indicating the begin of the input. It only deletes one char per press, if it didnt have a limit the entire input would be gone within a few milliseconds since it updates every frame
            if (Glfw.GetKey(Main.COREMain.window, Keys.Backspace) != InputState.Press || LastLine.EndsWith(CurrentContext) || isPressedPrevious)
                return;

            LastLine = LastLine[..^1]; //replace the current version with a version of itself with the last char missing
            changed = true;
        }

        public static void Wipe()
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

            int sum = (int)(Main.COREMain.debugText.characterHeight * 0.7f + 2); //rounding to an int makes it always render on top of a single pixel, instead of dividing into over multiple, which causes uglier looking letters //better to calculate here than every loop to save wasted performance
            int lineOffset = sum;

            bool original = Main.COREMain.debugText.drawWithHighlights;
            int max = indexOfFirstLineToRender + maxLines > lines.Count ? lines.Count : indexOfFirstLineToRender + maxLines; //if there are less lines than the console can show it must only render those lines, otherwise it will try to render lines that dont exist
            for (int i = indexOfFirstLineToRender; i < max; i++, lineOffset += sum)
            {
                if (lines[i] == null)
                    continue;

                Main.COREMain.debugText.drawWithHighlights = !lines[i].Contains('@') && !lines[i].StartsWith("ERROR ") && !lines[i].StartsWith("DEBUG ");
                Vector3 color = GetColorFromPrefix(lines[i], out string printResult);

                string[] allText = SeperateByLength(printResult);

                for (int j = 0; j < allText.Length; j++)
                {
                    if (j > 0)
                    {
                        lineOffset += sum;
                        max--;
                    }
                        
                    string suffix = i == lines.Count - 1 && j == allText.Length - 1 ? "|" : !lines[i].Contains("COREConsole/") && amountOfAppearancesLine[lines[i]] != 0 && COREMain.debugText.GetStringWidth(lines[i] + $" ({amountOfAppearancesLine[lines[i]]})", 0.7f) < Width ? $" ({amountOfAppearancesLine[lines[i]]})" : ""; //the | indicates the cursor. Only the last string has this

                    if (lines[i].Contains("COREConsole/") || i == lines.LastIndexOf(lines[i]))
                        quad.Write(allText[j] + suffix, 0, Height - lineOffset, 0.7f, color);
                }
            }
            Main.COREMain.debugText.drawWithHighlights = original;
        }

        private static void WriteLineF(string text)
        {
            if (!amountOfAppearancesLine.ContainsKey(text))
                indexOfFirstLineToRender++;

            if (amountOfAppearancesLine.ContainsKey(text) && !text.Contains("COREConsole/"))
            {
                int index = amountOfAppearancesLine[text];
                amountOfAppearancesLine.Remove(text);
                amountOfAppearancesLine.Add(text, index + 1);
                lines.RemoveAt(lines.IndexOf(text));
            }
            else if (!text.Contains("COREConsole/"))
                amountOfAppearancesLine.Add(text, 0);

            changed = true; //tells the console render everything again (could be optimised better by only rendering the newly added sentence / letters instead of every (already existing) sentence)

            lines.Add(text);
        }

        public static void WriteLine(object value)
        {
            string end = value.ToString();
            string[] allResults = new string[] { end };

            if (end.Contains(Environment.NewLine))
                allResults = SeperateByNewLines(end);

            foreach (string s in allResults)
                WriteLineF(s);
        }

        public static string[] SeperateByNewLines(string end)
        {
            string prefix = GetPrefix(end); //decides if the base string is an error or debug by checking if it starts with its prefix

            string[] seperatedLines = end.Split(new string[] {Environment.NewLine}, StringSplitOptions.None);
            for (int i = 1; i < seperatedLines.Length; i++)
                seperatedLines[i] = prefix + seperatedLines[i];
            
            return seperatedLines;
        }

        private string[] SeperateByLength(string end)
        {
            float textWidth = Main.COREMain.debugText.GetStringWidth(end, 0.7f);
            float tooBigPercentage = textWidth / Width;
            if (tooBigPercentage < 1)
                return new string[] { end };

            int stringLength = end.StartsWith("ERROR ") || end.StartsWith("DEBUG ") ? end.Length - 5 : end.Length; //DEBUG and ERROR arent rendered to the console so they need to be neglected when calculating if the string is too long
            string prefix = GetPrefix(end);

            int amountOfCharsForCorrectWidth = (int)(stringLength / tooBigPercentage);
            int timesTooBig = (int)tooBigPercentage;
            string[] seperations = new string[timesTooBig + 1];
            for (int i = 0; i < timesTooBig; i++)
                seperations[i] = i == 0 ? end[..amountOfCharsForCorrectWidth] : prefix + end[(amountOfCharsForCorrectWidth * i)..(amountOfCharsForCorrectWidth * (i + 1))];//end = end.Insert((amountOfCharsForCorrectWidth - 1) * (i + 1), "\n");
            seperations[^1] = prefix + end[(amountOfCharsForCorrectWidth * timesTooBig)..];

            return seperations;
        }

        private static string GetPrefix(string end) => end.StartsWith("DEBUG ") ? "DEBUG " : end.StartsWith("ERROR ") ? "ERROR " : "";

        public static void WriteLine() => WriteLine("");

        public void Write(string text)
        {
            lines[^1] += text; //add the new text to the to be overridden line
            changed = true;
        }

        public static void WriteError(System.Exception err) => WriteError(err.ToString());

        public static void WriteError(string err)
        {
            if (writeError)
                WriteLine("ERROR " + err);

            if (canWriteToLog)
            {
                using (FileStream fs = new(logLocation, FileMode.Append))
                using (StreamWriter sw = new(fs))
                    sw.WriteLine(err);
            }
        }

        public static void WriteDebug(string debug)
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

        public static void GenerateConsoleErrorLog(string path)
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
            string initialized = COREMain.LoadFilePath != null ? $"from {Path.GetFileName(Main.COREMain.LoadFilePath)}" : "independently";
            WriteLine( "                   =                  ");
            WriteLine( "                 :*%@*.               ");
            WriteLine($"               :%%%%@@@=                       CORE Renderer {COREMain.VERSION}");
            WriteLine($"             :+%%%%%@@@@@*.                    CORE Math {MathC.VERSION}");
            WriteLine( "           :%%%%%%%#@@@@@@@=          ");
            WriteLine($"         :+%%%%%%%%#=*%@@@@@@*.                GPU: {COREMain.GPU}");
            WriteLine($"       :%%%%%%%%%%%#===*@@@@@@@=               OpenGL {GL.glGetString(GL.GL_VERSION)}");
            WriteLine($"     :+%%%%%%%%%%%%#=====*%@@@@@@*.            GLSL {GL.glGetString(GL.GL_SHADING_LANGUAGE_VERSION)}");
            WriteLine( "   :%%%%%%%%%%%%%%%#=======*@@@@@@@=  ");
            WriteLine($"  +%%%%%%%%%%%%%%%%#=========*@@@@@@@-         {lines.Count - 9} messages were logged before this menu");
            WriteLine($"   +###############%========*@@@@@@@=          Initialized with {COREMain.LoadFile}");
            WriteLine($"    :+#############%======*@@@@@@@=            Initialized from {initialized}");
            WriteLine( "       +###########%====*@@@@@@@=     ");
            WriteLine($"        :+#########%==*@@@@@@@=                Rendering with {Rendering.shaderConfig} shaders");
            WriteLine($"           +#######%*@@@@@@@=                  Rendering with {COREMain.splashScreen.refreshRate} Hz");
            WriteLine($"            :+#####%@@@@@@=                    Rendering with {1 / Rendering.TextureQuality}x screen resolution");
            WriteLine($"               +###%@@@@=                      Rendering resolution of {COREMain.monitorWidth}x{COREMain.monitorHeight}");
            WriteLine( "                :+#%@@=               ");
            WriteLine( "                  .+-                 ");
            WriteLine();
            WriteLine($"COREConsole/{currentContext} > ");
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
                Glfw.SetWindowShouldClose(Main.COREMain.window, true);

            else if (input.Contains("save scene as"))
            {
                string filename = input[14..^4];
                new Job(() => Writers.GenerateCRS(Main.COREMain.BaseDirectory, filename, $"Generated by CORE-Renderer {Main.COREMain.VERSION}, CRW {Readers.CURRENT_VERSION}", COREMain.CurrentScene)).Start();
            }

            else if (input == "save as stl") //debug
            {
                Main.COREMain.CurrentScene.currentObj = 0;
                if (Main.COREMain.CurrentScene.models.Count > 0 && Main.COREMain.GetCurrentObjFromScene != -1)
                {
                    Writers.GenerateSTL(Main.COREMain.BaseDirectory, $"Written by CORE-Renderer {Main.COREMain.VERSION}", Main.COREMain.CurrentModel);
                    WriteDebug($"Generated {Main.COREMain.CurrentModel.Name}.stl");
                }
                else
                    WriteError("There is no model to get data from");
            }
            else if (input == "save all as stl")
            {
                Main.COREMain.MergeAllModels(out List<List<float>> vertices, out List<Vector3> offsets);
                Writers.GenerateSTL(Main.COREMain.BaseDirectory, "test", $"test.stl written by CORE-Renderer {Main.COREMain.VERSION}", vertices, offsets);
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
                Restart();

            else if (input.Contains("set shaders"))
                ChangeShaders(input);

            else if (input == "recompile shaders")
                Restart();

            else HandleContextCommand(input);

            WriteLine($"COREConsole/{currentContext} > ");
        }

        private void HandleContextCommand(string input)
        {
            if (currentContext == Context.Console)
                HandleConsoleCommands(input);
            else if (currentContext == Context.Camera)
                HandleCameraCommands(input);
            else if (currentContext == Context.Scene)
                HandleSceneCommands(input);
            else
                WriteError($"Couldn't parse command \"{input}\"");
        }

        private void HandleCameraCommands(string input)
        { //every if statement checks the length of the input first, because if it tries the check the contents if the input like input[..4] == "get " it could throw an error if the input isnt 4 chars long (out of range). This is also prevented later on by using try { } catch (OutOfRangeException) { }
            if (input.Length > 4 && input[..4] == "get ") //keyword for displaying variable values
            {
                switch (input[4..]) 
                {
                    case "speed": WriteLine($"{Camera.cameraSpeed}");
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
                    default: WriteError($"Unknown variable: \"{input[5..]}\"");
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
                        Rendering.Camera.Fov = result;
                        break;
                    case "yaw":
                        float result2 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result2, input[7..]);
                        Rendering.Camera.Yaw = result2;
                        break;
                    case "pitch":
                        float result3 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result3, input[9..]);
                        Rendering.Camera.Pitch = result3;
                        break;
                    case "farplane":
                        float result4 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result4, input[12..]);
                        Rendering.Camera.FarPlane = result4;
                        break;
                    case "nearplane":
                        float result5 = 0; //hold value here because property cant be used as ref
                        ChangeValue(ref result5, input[13..]);
                        Rendering.Camera.NearPlane = result5;
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
            if (input.StartsWith("attach "))
            {
                string[] inputs = input.Split(new string[] { " " }, StringSplitOptions.TrimEntries);
                string path = GetFullPath(inputs[1]);
                int modelIndex = Readers.GetOneIntWithRegEx(inputs[3]);
                COREMain.CurrentScene.models[modelIndex].submodels[0].material = Readers.LoadCPBR(path);
                return;
            }

            //delete
            if (input.StartsWith("delete ")) //keyword for deleting one or more models
            {
                if (input.StartsWith("delete model[")) //allows the removal of a single object
                {
                    try
                    {
                        int index = Readers.GetOneIntWithRegEx(input);
                        Main.COREMain.scenes[Main.COREMain.SelectedScene].models[index].Dispose();
                        WriteLine($"Deleted model {index}");
                        Main.COREMain.scenes[Main.COREMain.SelectedScene].currentObj = -1; //making no models highlighted to prevent crashes
                    }
                    catch (IndexOutOfRangeException)
                    {
                        WriteLine($"Couldn't delete model {input[13..input.IndexOf(']')]}");
                    }
                }
                else if (input.StartsWith("delete models")) //allows the removal of multiple objects at once with array-like indexing ([1..4] to delete the second model till the 4th for example)
                {
                    try
                    {
                        int index1 = Readers.GetOneIntWithRegEx(input[..(input.IndexOf("..") + 1)]);
                        int index2 = Readers.GetOneIntWithRegEx(input[input.IndexOf("..")..]);
                        for (int i = index1; i <= index2; i++) //iterate through every index between the found indexes
                            COREMain.scenes[COREMain.SelectedScene].models[i].Dispose();
                        COREMain.scenes[COREMain.SelectedScene].currentObj = -1; //making no models highlighted to prevent crashes
                        WriteLine($"Deleted models {index1} through {index2}");
                    }
                    catch (IndexOutOfRangeException)
                    {
                        WriteLine($"Couldn't delete given models");
                    }
                }
            }

            //load
            else if (input.StartsWith("load ")) //keyword for loading in a file with given path
            {
                if (input.Length > 12 && input[5..9] == "dir ")
                {
                    string dir = GetFullPath(input[9..]);
                    
                    if (dir[..4] == "this" && Main.COREMain.LoadFilePath == null) //throws an error if it wasnt opened in a directory but 'this' is used
                    {
                        WriteError($"\"{dir}\" isn't valid here");
                        return;
                    }
                    else if (dir[..5] == "$PATH") //allows '$PATH' to be used as an alias for the base of the directory that the .exe is located
                        dir = Main.COREMain.BaseDirectory + dir[5..];
                    
                    if (Directory.Exists(dir)) //checks if the given directory is valid
                    {
                        Main.COREMain.LoadDir(dir);
                        WriteLine($"Loaded directory {dir}");
                    }
                    else
                        WriteError($"Couldn't find the directory at {dir}");
                }  
                else if (input[5..9] != "dir ") //checks if user wants to load in an directory or a single file
                {
                    string dir = GetFullPath(input[5..]);
                    if (File.Exists(dir) && dir[^4..] == ".obj" || dir[^4..] == ".hdr" || dir[^4..] == ".stl" || dir[^4..] == ".png" || dir[^4..] == ".jpg") //only allows certain file types, in this case .obj and .hdr
                    {
                        Main.COREMain.scenes[Main.COREMain.SelectedScene].models.Add(new(dir));
                        WriteLine("Loaded file");
                    }
                    else
                        WriteError($"Invalid file at {dir}");
                }
                else
                    WriteError($"Couldn't find the file at {input[5..]}");
            }

            //get
            else if (input.StartsWith("get ")) //keyword for displaying variable values
            {
                switch (input[4..])
                {
                    case "model count":
                        WriteLine($"{Main.COREMain.CurrentScene.models.Count}");
                        break;
                    case "submodel count":
                        int count = 0;
                        foreach (Model model in COREMain.CurrentScene.models)
                            count += model.submodels.Count;
                        WriteLine($"{count}");
                        break;
                    case "draw calls":
                        WriteLine($"{Main.COREMain.drawCallsPerFrame}");
                        break;
                }
            }

            //set
            else if (input.StartsWith("set ")) //keyword for displaying variable values
            {
                if (input[5..].IndexOf(' ') + 5 == -1)
                    return;
                switch (input[4..(input[5..].IndexOf(' ') + 5)])
                {
                    case "useRenderDistance":
                        Submodel.useRenderDistance = input.Contains("true");
                        WriteLine($"Set variable to {Submodel.useRenderDistance}");
                        break;
                    case "renderDistance":
                        ChangeValue(ref Submodel.renderDistance, input[18..]);
                        WriteLine($"Set variable to {Submodel.renderDistance}");
                        break;
                    case "reflectionQuality":
                        Rendering.ShadowQuality = Readers.GetOneFloatWithRegEx(input);
                        WriteLine($"Set reflection quality to {Rendering.ShadowQuality}");
                        break;
                    case "textureQuality":
                        Rendering.TextureQuality = Readers.GetOneFloatWithRegEx(input);
                        WriteLine($"Set texture quality to {Rendering.TextureQuality}");
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
                        int indexOfSymbol = input.IndexOf(']');
                        int modelIndex = Readers.GetOneIntWithRegEx(input[..indexOfSymbol]);

                        Model model = COREMain.CurrentScene.models[modelIndex];
                        
                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input[(indexOfSymbol + 1)..]);
                        if (input.StartsWith("moveto")) //sorts for moveto, this value is applied absolutely
                        {
                            model.Transform.translation = location;
                            WriteLine($"Moved model to {location}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            model.Transform.translation += location;
                            WriteLine($"Moved model with {location}");
                        }
                    }
                    else if (input.Contains("models"))
                    {
                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input);
                        if (input[4..6] == "to") //sorts for moveto, this value is applied absolutely
                        {
                            foreach (Model model in Main.COREMain.CurrentScene.models)
                                model.Transform.translation += location;
                            WriteLine($"Moved model to {location}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            foreach (Model model in Main.COREMain.CurrentScene.models)
                                    model.Transform.translation += location;
                            WriteLine($"Moved model with {location}");
                        }
                    }
                    
                }
                catch (System.Exception)
                {
                    WriteError("Given index is invalid");
                    return;
                }
            }

            else if (input == "disable arrows")
            {
                Arrows.disableArrows = true;
                WriteLine("Disabled arrows");
            }

            else if (input == "enable arrows")
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
                Wipe();
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

        private void Restart()
        {
            if (Main.COREMain.LoadFilePath == null)
                Process.Start("CORERenderer.exe");
            else
                Process.Start("CORERenderer.exe", Main.COREMain.LoadFilePath);
            GenerateCacheFile(Main.COREMain.BaseDirectory);
            Environment.Exit(1);
        }

        private void ChangeShaders(string input)
        {
            if (input.ToLower().Contains("pathtracing"))
            {
                Rendering.shaderConfig = ShaderType.PathTracing;
                Main.COREMain.GenerateConfig();
                if (Main.COREMain.LoadFilePath == null)
                    Process.Start("CORERenderer.exe");
                else
                    Process.Start("CORERenderer.exe", Main.COREMain.LoadFilePath);
                GenerateCacheFile(Main.COREMain.BaseDirectory);
                Environment.Exit(1);
            }
            else if (input.ToLower().Contains("lighting"))
            {
                Rendering.shaderConfig = ShaderType.Lighting;
                Main.COREMain.GenerateConfig();
                if (Main.COREMain.LoadFilePath == null)
                    Process.Start("CORERenderer.exe");
                else
                    Process.Start("CORERenderer.exe", Main.COREMain.LoadFilePath);
                GenerateCacheFile(Main.COREMain.BaseDirectory);
                Environment.Exit(1);
            }
            else if (input.ToLower().Contains("fullbright"))
            {
                Rendering.shaderConfig = ShaderType.FullBright;
                Main.COREMain.GenerateConfig();
                if (Main.COREMain.LoadFilePath == null)
                    Process.Start("CORERenderer.exe");
                else
                    Process.Start("CORERenderer.exe", Main.COREMain.LoadFilePath);
                GenerateCacheFile(Main.COREMain.BaseDirectory);
                Environment.Exit(1);
            }
            else
                WriteError("Couldn't find shader type");
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

        private static string GetFullPath(string path)
        {
            if (COREMain.BaseDirectory == null)
                return path;

            if (path.Contains("this"))
                path = path.Replace("this", Path.GetDirectoryName(COREMain.LoadFilePath));
            else if (path.Contains("$PATH"))
                path = path.Replace("$PATH", COREMain.BaseDirectory);
            return path;
        }
    }
}