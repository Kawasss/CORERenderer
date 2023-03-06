using COREMath;
using CORERenderer.GLFW;
using CORERenderer.GLFW.Enums;
using CORERenderer.Loaders;
using CORERenderer.Main;
using CORERenderer.OpenGL;
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
            if (!isInFocus) //user can only type if they clicked on the console and the cursor remains in the console
                isInFocus = COREMain.CheckAABBCollisionWithClick(x, y, Width, Height);
            if (isInFocus)
                isInFocus = COREMain.CheckAABBCollision(x, y, Width, Height);
            if (isInFocus)
            {   //deletes the last char of the input, unless it reached "> " indicating the begin of the input. It only deletes one char per press, if it didnt have a limit the entire input would be gone within a few milliseconds since it updates every frame
                if (Glfw.GetKey(COREMain.window, Keys.Backspace) == InputState.Press && lines[linesPrinted - 1][^3..] != " > " && !isPressedPrevious)
                {
                    lines[linesPrinted - 1] = lines[linesPrinted - 1][..^1];
                    changed = true;
                }

                if (Glfw.Time - previousTime > 0.06) //time limit to prevent unneeded amount of requests
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
                    try //catches exceptions in case the pressed key isnt the key to char dictionary
                    {
                        letter = Globals.keyCharBinding[(int)COREMain.pressedKey]; //gets the char related to the pressed key
                        if (Glfw.GetKey(COREMain.window, Keys.LeftShift) == InputState.Press) //if shift is pressed get the uppercase variant of the char
                        {
                            if (Glfw.GetKey(COREMain.window, Keys.Alpha4) == InputState.Press)
                                letter = '$'; //special key for aliases
                            else if (Glfw.GetKey(COREMain.window, Keys.Period) == InputState.Press)
                                letter = '>';
                            else
                                letter = letter.ToString().ToUpper()[0]; //first to string then to upper because chars dont have to upper, then back to char with [0]
                            
                        }  
                        if ((letter != lines[linesPrinted - 1][^1] || Glfw.Time - previousTime2 > 0.15))
                        {
                            Write($"{letter}"); //adds letter to the last line of text
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
            if (!changed) //if no lines have been changed / added break, this improves performance a lot
                return;

            changed = false;

            quad.Render(); //renders background

            int sum = (int)(COREMain.debugText.characterHeight * 0.7f + 2); //rounding to an int makes it always render on top of a single pixel, instead of dividing into over multiple, which causes uglier looking letters //better to calculate here than every loop to save wasted performance
            int lineOffset = sum;
            if (lines == null)
                return;
            for (int i = 0; i < linesPrinted; i++, lineOffset += sum) //decides the space between the lines
            {
                if (lines[i] == null)
                    continue;
                if (lines[i].Length > 5 && lines[i][..5] == "ERROR")
                    quad.WriteError(lines[i], 0, Height - lineOffset, 0.7f);
                else if (lines[i].Length > 5 && lines[i][..5] == "DEBUG")
                    quad.Write(lines[i], 0, Height - lineOffset, 0.7f, new(0, 1, 0));
                else if (lines.Length > 0 && i != linesPrinted - 1) //determines if the line is an error or not
                    quad.Write(lines[i], 0, Height - lineOffset, 0.7f);
                else if (lines.Length > 0 && i == linesPrinted - 1)
                    quad.Write(lines[i] + '|', 0, Height - lineOffset, 0.7f);
            }
        }

        public void WriteLine(string text)
        {
            changed = true; //tells the console render everything again (could be optimised better by only rendering the newly added sentence / letters instead of every (already existing) sentence)

            linesPrinted++;
            if (linesPrinted > maxLines) //if statement the prevent the console lines from rendering outside if its height (or give more lines than the lines[] array can handle)
            {
                linesPrinted--; //change it to reflect that no new lines will be added, the first line will be removed and a new one will be added
                string[] oldLines = lines;
                lines = new string[maxLines];
                for (int i = 0; i < maxLines - 1; i++) //move every line back by one to make room for the new line, making it look like the text is moving downwards everytime theres too much text
                    lines[i] = oldLines[i + 1];
            }
            float textWidth = COREMain.debugText.GetStringWidth(text + '|', 0.7f);
            
            //returns if the string isnt longer than the width if the console
            if (textWidth < Width)
                lines[linesPrinted - 1] = text;
            else
                lines[linesPrinted - 1] = "ERROR Message is too long, returning";
        }

        public void Write(string text)
        {
            linesPrinted--; //tricks WriteLine() into thinking thats its adding a new line instead of overriding an old one
            WriteLine(lines[linesPrinted] + text); //add the new text to the to be overridden line
        }

        public void WriteError(System.Exception err) => WriteLine("ERROR " + err.ToString());

        public void WriteError(string err) => WriteLine("ERROR " + err);

        public void WriteDebug(string debug) => WriteLine("DEBUG " + debug);

        /// <summary>
        /// Renders the entire console even if nothing has changed, overriding the optimisation. This will make a draw call to the GPU for each letter, causing a severe performance drop
        /// </summary>
        public void RenderEvenIfNotChanged()
        {
            changed = true;
            Render();
        }

        public void RenderStatic() => Render();

        /// <summary>
        /// Writes all information gathered from the initial start up to the console
        /// </summary>
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
                    else if (i == 12 && COREMain.LoadFilePath != null)
                        WriteLine($"{n}         Initialized from {Path.GetFileName(COREMain.LoadFilePath)}");
                    else if (i == 12 && COREMain.LoadFilePath == null)
                        WriteLine($"{n}         Initialized independently");
                    else if (i == 14)
                        WriteLine($"{n}         Rendering with default shaders");
                    else if (i == 15)
                        WriteLine($"{n}         Rendering with {COREMain.splashScreen.refreshRate} Hz");
                    else
                        WriteLine(n);
                }
            WriteLine($"COREConsole/{currentContext} > ");
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
            else if (input == "goto console") //introducing contexts allows for better grouping of commands and better readability / makes it more expandable
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
            else if (input.Length > 16 && input[..5] == "goto " && input.Contains("->"))
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
                    if (succeeded && index < COREMain.scenes[COREMain.selectedScene].allModels.Count) //only continue if the index is found and its within range
                    {
                        try
                        {
                            COREMain.scenes[COREMain.selectedScene].allModels[index].Dispose();
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
                    int index1;
                    bool succeeded = int.TryParse(input[14..input.IndexOf('.')], out index1); //gets the value if the first index
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
                                COREMain.scenes[COREMain.selectedScene].allModels[i].Dispose();
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
                    if (File.Exists(dir) && dir[^4..] == ".obj" || dir[^4..] == ".hdr") //only allows certain file types, in this case .obj and .hdr
                        COREMain.scenes[COREMain.selectedScene].allModels.Add(new(dir));
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
                        WriteLine($"{COREMain.scenes[COREMain.selectedScene].allModels.Count}");
                        break;
                    case "submodel count":
                        int count = 0;
                        foreach (Model model in COREMain.scenes[COREMain.selectedScene].allModels)
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
                        Model model = COREMain.GetCurrentScene.allModels[modelIndex];

                        int[] indexes = new int[3]; //gets all the indexes for the spaces in between the value
                        int aa = 0;
                        for (int i = input[indexOfSymbol..].IndexOf(' '); i > -1; i = input[indexOfSymbol..].IndexOf(' ', i + 1), aa++)
                            indexes[aa] = i;
                        Vector3 output = new(input[indexOfSymbol..][indexes[0]..indexes[1]], input[indexOfSymbol..][indexes[1]..indexes[2]], input[indexOfSymbol..][indexes[2]..]);
                        if (input[4..6] == "to") //sorts for moveto, this value is applied absolutely
                        {
                            Vector3 distance = output - model.submodels[0].translation;
                            foreach (Submodel submodel in model.submodels)
                                submodel.translation += distance;
                            WriteLine($"Moved model to {output.x}, {output.y}, {output.z}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            foreach (Submodel submodel in model.submodels)
                                submodel.translation += output;
                            WriteLine($"Moved model with {output.x}, {output.y}, {output.z}");
                        }
                    }
                    else if (input.Contains("models"))
                    {
                        int[] indexes = new int[3]; //gets all the indexes for the spaces in between the value
                        int aa = 0;
                        for (int i = input[(input.IndexOf("models") + 1)..].IndexOf(' '); i > -1; i = input[(input.IndexOf("models") + 1)..].IndexOf(' ', i + 1), aa++)
                            indexes[aa] = i;
                        Vector3 output = new(input[(input.IndexOf("models") + 1)..][indexes[0]..indexes[1]], input[(input.IndexOf("models") + 1)..][indexes[1]..indexes[2]], input[(input.IndexOf("models") + 1)..][indexes[2]..]);
                        if (input[4..6] == "to") //sorts for moveto, this value is applied absolutely
                        {
                            Vector3 distance = output - COREMain.GetCurrentScene.allModels[0].submodels[0].translation;
                            foreach (Model model in COREMain.GetCurrentScene.allModels)
                                foreach (Submodel submodel in model.submodels)
                                    submodel.translation += distance;
                            WriteLine($"Moved model to {output.x}, {output.y}, {output.z}");
                        }
                        else //sorts for move, this value is applied relatively
                        {
                            foreach (Model model in COREMain.GetCurrentScene.allModels)
                                foreach (Submodel submodel in model.submodels)
                                    submodel.translation += output;
                            WriteLine($"Moved model with {output.x}, {output.y}, {output.z}");
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
    }
}
