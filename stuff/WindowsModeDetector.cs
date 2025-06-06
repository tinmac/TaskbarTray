using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray.stuff
{
    using Microsoft.Win32;

    public static class WindowsModeDetector
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "SystemUsesLightTheme";

        public static bool IsSystemInLightMode()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            if (key?.GetValue(RegistryValueName) is int value)
            {
                return value != 0;
            }

            // Default to light if missing
            return true;
        }

        public static event Action<bool>? SystemThemeChanged;

        private static RegistryMonitor? _monitor;

        public static void StartMonitoring()
        {
            _monitor ??= new RegistryMonitor(RegistryHive.CurrentUser, RegistryKeyPath);
            _monitor.RegChanged += () =>
            {
                bool isLight = IsSystemInLightMode();
                SystemThemeChanged?.Invoke(isLight);
            };
            _monitor.Start();
        }

        public static void StopMonitoring()
        {
            _monitor?.Stop();
            _monitor = null;
        }
    }
}
