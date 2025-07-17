using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO;

namespace PowerSwitch.stuff;


//public static class ServiceHelper
//{
//    private const string ServiceName = "SensorMonitorService";

//    public static bool IsServiceInstalled()
//    {
//        return ServiceController.GetServices().Any(s => s.ServiceName == ServiceName);
//    }

//    public static bool IsServiceRunning()
//    {
//        try
//        {
//            using var sc = new ServiceController(ServiceName);
//            return sc.Status == ServiceControllerStatus.Running;
//        }
//        catch { return false; }
//    }

//    public static void RunInstallScriptIfNeeded()
//    {
//        if (!IsServiceInstalled())
//        {
//            var scriptPath = Path.Combine(AppContext.BaseDirectory, "install-service.ps1");
//            if (!File.Exists(scriptPath)) return;

//            var psi = new ProcessStartInfo
//            {
//                FileName = "powershell.exe",
//                Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\"",
//                UseShellExecute = true,
//                Verb = "runas" // Elevates
//            };
//            Process.Start(psi);
//        }
//    }

//    public static void StartService()
//    {
//        using var sc = new ServiceController(ServiceName);
//        if (sc.Status != ServiceControllerStatus.Running)
//        {
//            sc.Start();
//            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
//        }
//    }

//    public static void StopService()
//    {
//        using var sc = new ServiceController(ServiceName);
//        if (sc.Status == ServiceControllerStatus.Running)
//        {
//            sc.Stop();
//            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
//        }
//    }
//}
