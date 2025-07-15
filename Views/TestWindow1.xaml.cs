using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace TaskbarTray.Views
{
    public sealed partial class TestWindow1 : Window
    {
        public TestWindow1()
        {
            InitializeComponent();

            // Detect theme change by user
            RootGrid.ActualThemeChanged += RootGrid_ActualThemeChanged;

            // Handle initial theme
            OnThemeChanged(RootGrid.ActualTheme);
        }

        private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
        {
            OnThemeChanged(sender.ActualTheme);
        }

        private void OnThemeChanged(ElementTheme theme)
        {
            Serilog.Log.Information($"Theme changed to: {theme}");
            // Apply theme-specific logic here
        }
    }
}
