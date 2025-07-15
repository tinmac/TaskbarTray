using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray.stuff;

public static class ElevationHelper
{
    public static bool IsRunningAsAdmin()
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    public static void RelaunchAsAdmin()
    {
        var exePath = Process.GetCurrentProcess().MainModule!.FileName;

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = true,
            Verb = "runas" // Triggers UAC
        };

        try
        {
            Process.Start(psi);
            Environment.Exit(0); // Close current (non-elevated) instance
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            // User canceled UAC prompt
            Log.Warning("User canceled admin prompt: " + ex.Message);
        }
    }
}
