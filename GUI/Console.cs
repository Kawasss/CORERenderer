using COREMath;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.Loaders;
using CORERenderer.Main;
using CORERenderer.OpenGL;
using CORERenderer.textures;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace CORERenderer.GUI
{
    public partial class Console
    {
        #region public
        public static int[] ConsoleDimensionsWithDebugmenu { get => new int[] { (int)(COREMain.monitorWidth * 0.496 - COREMain.monitorWidth * 0.125f), (int)(COREMain.monitorHeight * 0.242f - 25), COREMain.monitorWidth - COREMain.viewportX - (int)(COREMain.monitorWidth * 0.496 - COREMain.monitorWidth * 0.125f), (int)(COREMain.monitorHeight * 0.004f) }; }
        public static int[] ConsoleDimensionsWithoutDebugmenu { get => new int[] { COREMain.renderWidth, (int)(COREMain.monitorHeight * 0.242f - 25), COREMain.viewportX, (int)(COREMain.monitorHeight * 0.004f) }; }
        public bool IsInFocus { get => IsInFocus; }

        public static bool writeDebug = false, writeError = true, changed = true;
        #endregion
        #region private
        private        string LastLine { get { return lines[^1]; } set { lines[^1] = value; } }
        private        string CurrentContext { get { return $"COREConsole/{currentContext} > "; } }

        private static int indexOfFirstLineToRender = 0, previousIOFLTR = 0;
        private        int Width, Height, x, y, maxLines = 0;

        private        double previousTime = 0, previousTime2 = 0;

        private static bool canWriteToLog = false;
        private        bool isInFocus = false, isPressedPrevious = false;

        private static string logLocation;

        private static Dictionary<string, int> amountOfAppearancesLine = new();
        private static Dictionary<string, ShaderType> shaderTypeLUT = new() { { "pathtracing", ShaderType.PathTracing }, { "pbr", ShaderType.PBR }, { "fullbright", ShaderType.FullBright } };
        private static Dictionary<string, Context> contextLUT = new() { { "scene", Context.Scene }, { "camera", Context.Camera }, { "console", Context.Console } };
        private        Dictionary<string, Model> BasicShapeLUT = new() { { "cube", Model.Cube }, { "cylinder", Model.Cylinder }, { "plane", Model.Plane }, { "sphere", Model.Sphere } }; //cant be static because opengl calls in model constructor
        private static List<string> lines = new();
        
        private        List<string> allCommands = new();
        private        Div quad;

        private Context currentContext = Context.Console;
        #endregion
        //if this needs to be optimised make it reuse the previous frames as a texture so the previous lines dont have to reprinted, saving draw calls
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

            isInFocus = isInFocus ? COREMain.CheckAABBCollision(x, y, Width, Height) : COREMain.CheckAABBCollisionWithClick(x, y, Width, Height); //if the console is already in focus just check if the cursor is still in the console, otherwise check if the console is clicked on to make it in focus

            if (previousLineCount < lines.Count)
                indexOfFirstLineToRender = lines.Count - maxLines;

            if (!isInFocus)
            {
                previousIOFLTR = indexOfFirstLineToRender;
                previousLineCount = lines.Count;
                return;
            }

            indexOfFirstLineToRender -= (int)(Main.COREMain.scrollWheelMovedAmount * 1.5);
            indexOfFirstLineToRender = indexOfFirstLineToRender >= 0 ? indexOfFirstLineToRender : 0; //IOFLTR cannot be smaller 0, since that would result in an index out of range error. otherwise apply the desired direction
            changed = Main.COREMain.scrollWheelMovedAmount != 0;

            CheckIfKeyNeedsToBeDeleted();

            CheckForCommands();

            CheckForUserInput();
        }

        private void CheckForCommands()
        {
            if (Glfw.Time - previousTime < 0.06)
                return;
                
            previousTime = Glfw.Time;
            if (COREMain.MouseButtonIsPressed(MouseButton.Left) && !LastLine.StartsWith(CurrentContext))
                WriteLine(CurrentContext);

            if (COREMain.KeyIsPressed(Keys.Enter) && !LastLine.EndsWith(CurrentContext))
                ParseInput(LastLine[(LastLine.IndexOf(CurrentContext) + CurrentContext.Length)..]);
        }

        private void CheckForUserInput()
        {
            if (COREMain.KeyIsPressed(Keys.Up) && lines[^1] != $"{CurrentContext}{allCommands[^1]}")
            {
                lines[^1] = $"{CurrentContext}{allCommands[^1]}";
                changed = true;
                return;
            }

            if (!COREMain.keyIsPressed || !Globals.keyCharBinding.ContainsKey((int)COREMain.pressedKey) || !Globals.keyShiftCharBinding.ContainsKey((int)COREMain.pressedKey))
            {
                isPressedPrevious = COREMain.KeyIsPressed(Keys.Backspace);
                return;
            }

            char letter = Globals.PressedLetter; //if shift is pressed use the appropriate version of that key
            if (letter != LastLine[^1] || Glfw.Time - previousTime2 > 0.15)
            {
                Write($"{letter}"); //adds letter to the last line of text
                previousTime2 = Glfw.Time;
            }
        }

        private void CheckIfKeyNeedsToBeDeleted()
        {
            //deletes the last char of the input, unless it reached "> " indicating the begin of the input. It only deletes one char per press, if it didnt have a limit the entire input would be gone within a few milliseconds since it updates every frame
            if (!COREMain.KeyIsPressed(Keys.Backspace) || LastLine.EndsWith(CurrentContext) || isPressedPrevious)
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

                string[] allText = SeperateByLength(printResult);

                for (int j = 0; j < allText.Length; j++)
                {
                    if (j > 0)
                    {
                        lineOffset += sum;
                        max--;
                    }
                    string suffix = "";
                    if (amountOfAppearancesLine.ContainsKey(lines[i]))
                        suffix = i == lines.Count - 1 && j == allText.Length - 1 ? "|" : !lines[i].Contains("COREConsole/") && amountOfAppearancesLine[lines[i]] != 0 && COREMain.debugText.GetStringWidth(lines[i] + $" ({amountOfAppearancesLine[lines[i]]})", 0.7f) < Width ? $" ({amountOfAppearancesLine[lines[i]]})" : ""; //the | indicates the cursor. Only the last string has this
                    else suffix = i == lines.Count - 1 && j == allText.Length - 1 ? "|" : "";

                    if (lines[i].Contains("COREConsole/") || i == lines.LastIndexOf(lines[i]))
                        quad.Write(allText[j] + suffix, 0, Height - lineOffset, 0.7f, color);
                }
            }
            COREMain.debugText.drawWithHighlights = original;
        }

        private static void WriteLineF(bool removeIfDuplicate, string text)
        {
            if (removeIfDuplicate)
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
            }

            changed = true; //tells the console to render everything again (could be optimised better by only rendering the newly added sentence / letters instead of every (already existing) sentence)

            lines.Add(text);
        }

        public static void WriteLine(object value) => WriteLine(true, value);
        public static void WriteLine(bool removeIfDuplicate, object value)
        {
            string end = value.ToString();
            string[] allResults = new string[] { end };

            if (end.Contains(Environment.NewLine))
                allResults = SeperateByNewLines(end);
            
            foreach (string s in allResults)
                WriteLineF(removeIfDuplicate, s);
        }

        public static string[] SeperateByNewLines(string end)
        {
            string prefix = GetPrefix(end); //decides if the base string is an error or debug by checking if it starts with its prefix

            string[] seperatedLines = end.Split('\n');//end.Split(new string[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < seperatedLines.Length; i++)
                seperatedLines[i] = prefix + seperatedLines[i];
            
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

            if (!canWriteToLog)
                return;

            using FileStream fs = new(logLocation, FileMode.Append);
            using StreamWriter sw = new(fs);
            sw.WriteLine(err);
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

        private static Vector3 GetColorFromPrefix(string s, out string trimmedString)
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
            string initialized = COREMain.LoadFilePath != null ? $"from {Path.GetFileName(COREMain.LoadFilePath)}" : "independently";
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
            try
            {
                allCommands.Add(input);

                string[] inputs = input.Split(new string[] { " " }, StringSplitOptions.TrimEntries);

                if (input == "exit")
                    Glfw.SetWindowShouldClose(COREMain.window, true);

                else if (inputs[0] == "save")
                {
                    if (inputs[1] == "scene")
                    {
                        string filename = Path.GetFileNameWithoutExtension(inputs[3]);
                        Writers.GenerateCRS(COREMain.BaseDirectory, filename, $"Generated by CORE-Renderer {COREMain.VERSION}, CRW {Readers.CURRENT_VERSION}", COREMain.CurrentScene);
                        WriteLine($"Finished generating {filename}.crs in {COREMain.BaseDirectory}");
                    }
                    else if (inputs[1].Contains("model"))
                    {
                        if (inputs[1] == "currentModel" && COREMain.GetCurrentObjFromScene == -1)
                        {
                            WriteError("Couldn't get the current model since no model is set as current: there are no models or none are selected");
                            return;
                        }
                        Writers.GenerateSTL(COREMain.BaseDirectory, $"Written by CORE-Renderer {Main.COREMain.VERSION}", GetModel(inputs[1]));
                    }
                    else if (inputs[1] == "as")
                    {
                        if (COREMain.CurrentScene.models.Count > 0 && Main.COREMain.GetCurrentObjFromScene != -1)
                        {
                            Writers.GenerateSTL(COREMain.BaseDirectory, $"Written by CORE-Renderer {Main.COREMain.VERSION}", COREMain.CurrentModel);
                            WriteDebug($"Generated {COREMain.CurrentModel.Name}.stl");
                        }
                        else
                            WriteError("There is no model to get data from");
                    }
                }

                else if (inputs[0] == "goto" && !input.Contains("->"))
                    currentContext = GetScene(inputs[1]);

                else if (inputs[0] == "goto" && inputs[2] == "->")
                    HandleCommand(inputs[1], inputs[3..]); //maybe 4?

                else if (input == "reload config")
                    COREMain.RestartWithArgsAndConfig();

                else if (input.Contains("set shaders"))
                    ChangeShaders(inputs[2]);

                else if (input == "recompile shaders")
                    COREMain.RestartWithArgsAndConfig();

                else HandleContextCommand(input);

            }
            catch (System.Exception err)
            {
                WriteError($"Couldn't parse given command \"{input}\", a provided argument was incorrect or there was insufficient information provided to construct a command. \nCaught error:\n{err}");
            }

            WriteLine($"COREConsole/{currentContext} > ");
        }

        private static Context GetScene(string input) => contextLUT[input];

        private void HandleCommand(string context, string[] input) => HandleCommand(GetScene(context), input);

        private void HandleCommand(Context context, string[] input)
        {
            if (context == Context.Console)
                HandleConsoleCommands(input);
            else if (context == Context.Scene)
                HandleSceneCommands(input);
            else if (context == Context.Camera)
                HandleCameraCommands(input);
        }

        private void HandleContextCommand(string input)
        {
            if (currentContext == Context.Console)
                HandleConsoleCommands(input.Split(new string[] { " " }, StringSplitOptions.TrimEntries));
            else if (currentContext == Context.Camera)
                HandleCameraCommands(input.Split(new string[] { " " }, StringSplitOptions.TrimEntries));
            else if (currentContext == Context.Scene)
                HandleSceneCommands(input.Split(new string[] { " " }, StringSplitOptions.TrimEntries));
            else
                WriteError($"Couldn't parse command \"{input}\"");
        }

        private void HandleCameraCommands(string[] input)
        { //every if statement checks the length of the input first, because if it tries the check the contents if the input like input[..4] == "get " it could throw an error if the input isnt 4 chars long (out of range). This is also prevented later on by using try { } catch (OutOfRangeException) { }
            if (input[0] == "get") //keyword for displaying variable values
                CameraGetCommand(input);
            else if (input[0] == "set") //keyword for changing the value of variables
                CameraSetCommand(input);
            else
                WriteError($"Couldn't parse camera command \"{input}\"");
        }

        private void HandleSceneCommands(string[] input)
        {
            if (input[0] == "attach")
            {
                string path = GetFullPath(input[1]);

                Submodel submodelToAttachTo = new();

                if (input[1].EndsWith("\\cpbr") && input.Length == 2) //if the given value is cpbr load the first cpbr found
                {
                    foreach (string file in Directory.GetFiles(path[..^4]))
                        if (Path.GetExtension(file) == ".cpbr")
                        {
                            path = file;
                            break;
                        }

                    if (COREMain.GetCurrentObjFromScene == -1) //avoiding unnecessary index out of range error
                    {
                        WriteError($"Can't attach {Path.GetFileName(path)} to a model since no model is currently selected. trying to continue with the first submodel of the first model, if that exists");
                        if (COREMain.CurrentScene.models.Count <= 0)
                        {
                            WriteError("Couldn't get the first submodel, since there are no models");
                            return;
                        }
                        submodelToAttachTo = COREMain.CurrentScene.models[0].submodels[0];
                    }
                    else submodelToAttachTo = COREMain.CurrentModel.submodels[0]; //its possible to only say "attach pathtocpbr", if so itll attach to current model
                }
                else if (input.Length > 2) //if the command is "attach pathtocpbr to submodel" itll get the submodels location and use that instead of the current model
                {
                    int[] modelIndex = Readers.GetTwoIntsWithRegEx(input[3]);
                    submodelToAttachTo = COREMain.CurrentScene.models[modelIndex[0]].submodels[modelIndex[1]];
                }
                submodelToAttachTo.material = Readers.LoadCPBR(path);
                return;
            }

            //delete
            if (input[0] == "delete") //keyword for deleting one or more models
            {
                if (!input[1].StartsWith("models") && !input[1].StartsWith("lights"))
                {
                    WriteError($"Invalid argument after \"delete\": {input[1]} is not recognized");
                    return;
                }
                try
                {
                    if (input[1].StartsWith("models") && input[1].Contains("..")) //.. indicates multiple entries
                    {
                        Model[] models = GetModels(input[1]);
                        for (int i = 0; i < models.Length; i++)
                            models[i].Dispose();
                        WriteLine($"Deleted models {COREMain.CurrentScene.models.IndexOf(models[0])} through {COREMain.CurrentScene.models.IndexOf(models[^1])}, totalling {models.Length} models");
                    }

                    else if (input[1].StartsWith("models")) //allows the removal of a single object
                    {
                        try
                        {
                            int index = Readers.GetOneIntWithRegEx(input[1]);
                            GetModel(input[1]).Dispose();
                            WriteLine($"Deleted model {index}");
                        }
                        catch (IndexOutOfRangeException)
                        {
                            WriteLine($"Couldn't delete model {input[13..input[1].IndexOf(']')]}"); //manually find the index instead of with regex since if no index is given regex wont properly work too
                        }
                    }
                    else if (input[1].StartsWith("lights"))
                    {
                        int index = Readers.GetOneIntWithRegEx(input[1]);
                        COREMain.CurrentScene.lights.RemoveAt(index);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    WriteError("Couldn't delete given variable(s), one or more indices were out of range. Variables that were deleted before the error can't be restored and no effort will be made to delete any other given variables");
                }
            }

            //load
            else if (input[0] == "load") //keyword for loading in a file with given path
            {
                if (BasicShapeLUT.ContainsKey(input[1]))
                    COREMain.CurrentScene.models.Add(BasicShapeLUT[input[1]]);

                else if (input[1] == "dir")
                {
                    string dir = GetFullPath(input[2]);

                    if (!Directory.Exists(dir)) //checks if the given directory is valid
                    {
                        WriteError($"Couldn't find the directory at {dir}");
                        return;
                    }

                    COREMain.LoadDir(dir);
                    WriteLine($"Loading directory {dir}");
                }
                else //checks if user wants to load in an directory or a single file
                {
                    string dir = GetFullPath(input[1]);
                    if (!File.Exists(dir) || COREMain.GetModelType(dir) == ModelType.None) //only allows certain file types, not really that necessary since model will catch it, but just to be sure
                    {
                        WriteError($"Invalid file at {dir}, one of the following conditions failed. Exists: {File.Exists(dir)}, valid extension: {COREMain.GetModelType(dir) == ModelType.None}");
                        return;
                    }

                    if (COREMain.GetModelType(dir) == ModelType.CRSFile)
                    {
                        WriteError("CRS files can't be loaded on runtime. Consider running this via the file.");
                        return;
                    }

                    COREMain.CurrentScene.models.Add(new(dir));
                    WriteLine($"Loaded file {Path.GetFileName(dir)} from {Path.GetDirectoryName(dir)}");
                }
            }

            //move
            else if (input[0] == "move" || input[0] == "moveto")
            {
                try
                {
                    if (input[1].Contains("models["))
                    {
                        if (input[1].Contains(".."))
                        {
                            WriteError("Invalid syntax found: \"..\" can't be used with this command");
                            return;
                        }

                        int indexOfSymbol = input[1].IndexOf(']');
                        int modelIndex = Readers.GetOneIntWithRegEx(input[1][..indexOfSymbol]);

                        Model model = GetModel(input[1]);

                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input[2] + ' ' + input[3] + ' ' + input[4]);
                        if (input[0] == "moveto") //sorts for moveto, this value is applied absolutely
                        {
                            model.Transform.translation = location;
                            WriteLine($"Moved model {modelIndex} to {location}");
                            return;
                        }
                        //sorts for move, this value is applied relatively
                        model.Transform.translation += location;
                        WriteLine($"Moved model {modelIndex} with {location}");
                    }
                    else if (input.Contains("models"))
                    {
                        Vector3 location = Readers.GetThreeFloatsWithRegEx(input[2] + ' ' + input[3] + ' ' + input[4]);
                        if (input[0] == "moveto") //sorts for moveto, this value is applied absolutely
                        {
                            foreach (Model model in COREMain.CurrentScene.models)
                                model.Transform.translation += location;
                            WriteLine($"Moved all models to {location}");
                            return;
                        }
                        //sorts for move, this value is applied relatively
                        foreach (Model model in COREMain.CurrentScene.models)
                            model.Transform.translation += location;
                        WriteLine($"Moved all models with {location}");
                    }
                }
                catch (System.Exception)
                {
                    WriteError("Given index is invalid");
                    return;
                }
            }

            else if (input[0] == "disable" || input[0] == "enable")
                SceneEnableDisable(input);

            //get
            else if (input[0] == "get") //keyword for displaying variable values
                SceneGetCommand(input);

            //set
            else if (input[0] == "set") //keyword for displaying variable values
                SceneSetCommand(input);

            else
                WriteError($"Couldn't parse scene command \"{input}\"");
        }

        private void HandleConsoleCommands(string[] input)
        {
            if (input[0] == "get")
            {
                if (input[1] == "info")
                    ShowInfo();
                else if (input[1] == "$PATH" || input[1] == "this")
                    WriteLine(GetFullPath(input[1]));
                else if (input[1] == "path")
                    WriteLine(GetFullPath("$PATH"));
                else
                    WriteError($"Couldn't resolve get-variable {input[1]}");
            }
                
            else if (input[0] == "clear" && input[1] == "GUI") //clears the GUI framebuffer, can leave artifacts behind
            {
                GL.glClearColor(0.085f, 0.085f, 0.085f, 1);
                GL.glClear(GL.GL_COLOR_BUFFER_BIT); 
                WriteLine("Cleared framebuffer");
            }
                
            else if (input[0] == "wipe") //wipes the console clean
                Wipe();

            else if ((input[0] == "disable" || input[0] == "enable") && input[1] == "debug")
            {
                if (!Debugmenu.isVisible)
                    return;

                Debugmenu.isVisible = input[0] == "enable"; //see disable enable above for info
                int[] dimensions = input[0] != "enable" ? ConsoleDimensionsWithoutDebugmenu : ConsoleDimensionsWithDebugmenu;
                COREMain.console = new(dimensions[0], dimensions[1], dimensions[2], dimensions[3]);
            }
            else
                WriteError($"Couldn't parse console command");
        }

        private static string GetTextureInfo(string info, Texture texture)
        {
            if (info == "log")
                return texture.Log;
            else if (info == "name")
                return texture.name;
            WriteError($"invalid argument for the given texture, it can only be log or name");
            return "";
        }

        private static void ChangeValue(ref float variable, string input)
        {
            bool succeeded = float.TryParse(input, out float result);
            if (succeeded)
            {
                variable = result;
                WriteLine($"Set variable to {variable}");
            }
            else
                WriteError($"Couldn't parse float {input[4..]}. the value is the same as it was before the command was issued");
        }

        private void ChangeShaders(string input)
        {
            if (shaderTypeLUT.ContainsKey(input))
            {
                Rendering.shaderConfig = shaderTypeLUT[input];
                GenerateCacheFile(COREMain.BaseDirectory);
                COREMain.RestartWithArgsAndConfig();
            }
            else
                WriteError("Couldn't find shader type");
        }

        private static Model GetModel(string input)
        {
            if (input == "currentModel")
                return COREMain.CurrentModel;

            return COREMain.CurrentScene.models[Readers.GetOneIntWithRegEx(input)];
        }

        private static Model[] GetModels(string input)
        {
            int[] indices = Readers.GetTwoIntsWithRegEx(input.Replace("..", " "));
            return COREMain.CurrentScene.models.ToArray()[indices[0]..indices[1]];
        }

        private static Submodel GetSubmodel(string input)
        {
            int[] indices = Readers.GetTwoIntsWithRegEx(input);
            return COREMain.CurrentScene.models[indices[0]].submodels[indices[1]];
        }

        public void LoadCacheFile(string dirPath)
        {
            if (!File.Exists($"{dirPath}\\consoleCache"))
            {
                WriteError("Failed to retrieve cache as it doesn't exist");
                return;
            }

            string[] commands = File.ReadAllLines($"{dirPath}\\consoleCache");
            foreach (string command in commands)
                ParseInput(command);

            File.Delete($"{dirPath}\\consoleCache");
            WriteDebug("Successfully retrieved and loaded cache");
        }

        private void GenerateCacheFile(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                WriteError($"Failed to create a cache as the given destination ({dirPath}) doesn't exist");
                return;
            }

            using StreamWriter sw = File.CreateText($"{dirPath}\\consoleCache");
            foreach (string command in allCommands)
                if (command != "exit" && !command.Contains("reload") && !command.Contains("set shaders") && !command.Contains("recompile shaders"))
                    sw.WriteLine(command);
        }

        private static string GetFullPath(string path)
        {
            if (path.Contains("this"))
            {
                if (COREMain.LoadFilePath == null)
                    throw new System.Exception($"Invalid argument given for alias {path}: this == null. This instance initialized independently and doesn't have an instance directory to fetch");
                path = path.Replace("this", Path.GetDirectoryName(COREMain.LoadFilePath));
            } 
            else if (path.Contains("$PATH"))
            {
                if (COREMain.BaseDirectory == null)
                    throw new System.Exception($"Invalid argument given for alias {path}: $PATH == null. This means that the .exe and its files isn't inside a folder named \"CORERenderer\", or (unlikely) a different error prevented the base directory from being found");
                path = path.Replace("$PATH", COREMain.BaseDirectory);
            }
            return path;
        }
    }
}