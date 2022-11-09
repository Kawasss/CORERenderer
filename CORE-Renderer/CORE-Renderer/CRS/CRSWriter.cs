using CORERenderer.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CORERenderer.CRS
{
    public partial class CRS : EngineProperties
    {
        public static bool GenerateCRS(string name) //in future make it so that it takes requests for places to generate
        {                                //for now its in the main path of the renderer
            string path = $"{CORERenderContent.pathRenderer}\\{name}.crs";
            string image = $"{path}\\folder.ico";

            if (Directory.Exists(path))
                return false;

            System.IO.Directory.CreateDirectory(path);
            File.SetAttributes(path, FileAttributes.System);
            File.Create(image);
            File.Copy($"{CORERenderContent.pathRenderer}\\logos\\logo4.ico", image);
            File.SetAttributes(image, FileAttributes.Hidden);
            FileStream DekstopIni = File.Create($"{path}\\Desktop.ini");

            string DesktopContent = 
                """
                [.ShellClassInfo]
                ConfirmFileOp=0
                NoSharing=0
                IconFile=folder.ico
                IconIndex=0
                InfoTip=CORE Rendering Scene
                """;
            byte[] writeDesktop = Encoding.Unicode.GetBytes(DesktopContent);
            DekstopIni.Write(writeDesktop);

            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.Hidden);
            File.SetAttributes($"{path}\\Desktop.ini", FileAttributes.System);

            return true;
        }
    }
}
