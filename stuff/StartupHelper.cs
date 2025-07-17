using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IWshRuntimeLibrary; // Add COM Reference to Windows Script Host Object Model

namespace PowerSwitch.stuff;

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
