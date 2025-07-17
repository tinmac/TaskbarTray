using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceProcess;
using System.Diagnostics;
using System.IO;
using Serilog;

namespace PowerSwitch.SensorPipeService;

public static class ServiceInstallerHelper
{
    private const string ServiceName = "SensorService_Labs";
    private const int WaitForInstallTimeoutSeconds = 15;

    public static async Task RunInstallScriptIfNeededAsync()
    {
        var services = ServiceController.GetServices();
        bool isInstalled = services.Any(s => s.ServiceName == ServiceName);

        if (isInstalled)
        {
            Log.Information($"{ServiceName} is already installed.");
            return;  
        }

        string scriptPath = Path.Combine(AppContext.BaseDirectory, "install-service.ps1");
        if (!File.Exists(scriptPath))
        {
            Log.Error($"❌ install-service.ps1 not found at: {scriptPath}");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(psi);
            Log.Information("ℹ️ install-service.ps1 launched, waiting for service to be installed...");

            bool appeared = await WaitForServiceInstallationAsync(ServiceName, WaitForInstallTimeoutSeconds);
            if (appeared)
            {
                var sc = new ServiceController(ServiceName);
                Log.Information($"✅ {ServiceName} installed, status: {sc.Status}");
            }
            else
            {
                Log.Error($"⚠️ Timeout: {ServiceName} not detected after {WaitForInstallTimeoutSeconds} seconds.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Failed to run install-service.ps1: {ex.Message}");
        }
    }

    private static async Task<bool> WaitForServiceInstallationAsync(string serviceName, int timeoutSeconds)
    {
        int waited = 0;
        const int delay = 1000;

        while (waited < timeoutSeconds * 1000)
        {
            if (ServiceController.GetServices().Any(s => s.ServiceName == serviceName))
                return true;

            await Task.Delay(delay);
            waited += delay;
        }

        return false;
    }
}

