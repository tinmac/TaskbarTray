using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerSwitch.stuff
{
    public static class ThemeHelper
    {
        // There are two settings App Dark & Windows Dark
        //
        // This reports App Dark not Win Dark, see Personalisation -> Colours
        //
        // To discover if the Task Bar is Dark/Light for .ico Tray Icons use ThemeHelper.IsWindowsInDarkMode();

        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>
        /// Returns true if Windows mode (taskbar, etc.) is dark.
        /// </summary>
        public static bool IsWindowsInDarkMode()
        {
            return GetRegistryDword("SystemUsesLightTheme") == 0;
        }

        /// <summary>
        /// Returns true if App mode (apps like WinUI, etc.) is dark.
        /// </summary>
        public static bool IsAppInDarkMode()
        {
            return GetRegistryDword("AppsUseLightTheme") == 0;
        }

        private static int GetRegistryDword(string valueName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
            {
                return key?.GetValue(valueName) is int value ? value : 1; // Default to light
            }
        }
    }
}
