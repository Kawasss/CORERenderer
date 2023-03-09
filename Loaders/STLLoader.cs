using COREMath;
using CORERenderer.GLFW;
using CORERenderer.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CORERenderer.Loaders
{
    public class STLLoader
    {
        public static bool LoadSTL(string path, out string name, out List<float> vertices)
        {
            if (!File.Exists(path))
            {
                name = "ERROR";
                vertices = new();
                return false;
            }
                

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                string binaryOrASCII = sr.ReadLine();
                if (binaryOrASCII.Length < 5) //the first 5 letters are needed to determine if the file is written in binary or not
                {
                    name = "ERROR";
                    vertices = new();
                    return false;
                }
                    

                bool succes = false;
                if (binaryOrASCII[..5] == "solid")
                    succes = LoadSTLInASCII(path, out name, out vertices);
                else
                    succes = LoadSTLInBinary(path, out name, out vertices);
            }
            return false;
        }

        private static bool LoadSTLInASCII(string path, out string name, out List<float> vertices)
        {
            vertices = new();

            using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new(fs))
            using (StreamReader sr = new(bs))
            {
                name = sr.ReadLine()[6..]; //first 6 chars are "solid " so those can be skipped
                for (string line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    MatchCollection normalValues = GetThreeFloatsWithRegEx(line);
                    foreach (Match match in normalValues)
                    {

                    }
                }
            }

            return false;
        }

        private static bool LoadSTLInBinary(string path, out string name, out List<float> vertices)
        {
            name = "ERROR";
            vertices = new();
            return false;
        }

        private static Vector3 GetThreeFloatsWithRegEx(string line)
        {
            Vector3 returnValue = new();
            MatchCollection matches = Regex.Matches(line, @"(?<destinations>[XYZ])(?<floats>[+-]?\d+(\.\d*)?)"); //fuck regex
            foreach (Match match in matches) //for loop would be faster but eh
            {
                string returned = match.Groups["destinations"].Value;
                float value = float.Parse(returned);
                switch (returned)
                {
                    case "X": returnValue.x = value; break;

                }
            }
            return returnValue;
        }
