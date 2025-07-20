using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using PowerSwitch.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Common.Models;
using Serilog;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PowerSwitch.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public SettingsViewModel ViewModel { get; }

        public TaskbarIcon TrayIcon { get; set; }


        // Replace usage of Window.Current with the correct way to get the current window in WinUI 3.
        // In WinUI 3, you should use the 'this' reference inside a Page to access the window via Window.GetWindow(this).

        public Settings()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
            this.DataContext = ViewModel;

            // With this line:
            var window = App.Main_Window;
            if (window != null)
            {
                window.Activated += Window_Activated;
            }
        }

        #region Weird issue after sleep

        // None of this worked
        //
        // copilot said:  This is a known WinUI 3 framework bug related to binding refresh after suspend/resume, and there is currently no reliable workaround.
        //                 If Microsoft releases a fix or a recommended pattern, you can revisit this in the future.
        //
        // Had a weird issue where the toggle checkboxes were showing as null after laptop was in sleep
        // ie horizontal line over checkbox instead of tick, 
        // if I hovered over (not clicked) the checkbox it would turn to a check mark tick. 
        // This was fixed by calling LoadPlanToggleSettingsAsync() here after sleep or navigation.

        private async void Window_Activated(object sender, WindowActivatedEventArgs e)
        {
            Log.Information("Settings window activated...");
            await ViewModel.LoadPlanToggleSettingsAsync();

            // Workaround for WinUI 3 checkbox indeterminate bug after resume
            this.DataContext = null;
            this.DataContext = ViewModel;

            // Force property change notifications for toggles
            ViewModel.IncludePowerSaver = ViewModel.IncludePowerSaver;
            ViewModel.IncludeBalanced = ViewModel.IncludeBalanced;
            ViewModel.IncludeHighPerformance = ViewModel.IncludeHighPerformance;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.LoadPlanToggleSettingsAsync();
        }

        #endregion

        private void SensorTypeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var sensorType = checkBox.Content as string;
                var hardwareSelection = checkBox.DataContext as HardwareSensorSelection;
                if (sensorType != null && hardwareSelection != null && !hardwareSelection.AllowedSensorTypes.Contains(sensorType))
                {
                    hardwareSelection.AllowedSensorTypes.Add(sensorType);
                }
            }
        }

        private void SensorTypeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var sensorType = checkBox.Content as string;
                var hardwareSelection = checkBox.DataContext as HardwareSensorSelection;
                if (sensorType != null && hardwareSelection != null && hardwareSelection.AllowedSensorTypes.Contains(sensorType))
                {
                    hardwareSelection.AllowedSensorTypes.Remove(sensorType);
                }
            }
        }
    }
}
