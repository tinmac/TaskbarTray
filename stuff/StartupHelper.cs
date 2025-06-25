using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// To fix the CS0246 error, you need to add a COM reference to the "Windows Script Host Object Model" in your project.  
// Follow these steps:  
// 1. In Visual Studio, right-click on your project in the Solution Explorer and select "Add" -> "Reference".  
// 2. In the Reference Manager, go to "COM" -> "Type Libraries".  
// 3. Find and select "Windows Script Host Object Model" and click "OK".  

// After adding the reference, the error should be resolved. Ensure the following using directive remains in your code:  

using IWshRuntimeLibrary; // Add COM Reference to Windows Script Host Object Model

namespace TaskbarTray.stuff;

public static class StartupHelper
{
    public static void AddAppToStartup(string appName, string exePath)
    {
        string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startupFolder, $"{appName}.lnk");

        if (System.IO.File.Exists(shortcutPath))
            return; // Already exists

        var shell = new WshShell();
        IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = exePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
        shortcut.WindowStyle = 1;
        shortcut.Description = appName;
        shortcut.Save();// bla
    }

    public static void RemoveAppFromStartup(string appName)
    {
        string shortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            $"{appName}.lnk");

        if (System.IO.File.Exists(shortcutPath))
            System.IO.File.Delete(shortcutPath);
    }
}
