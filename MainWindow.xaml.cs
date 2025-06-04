using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;



namespace TaskbarTray
{

    [ObservableObject]
    public sealed partial class MainWindow : Window
    {
        private bool _isWindowVisible;
        public bool IsWindowVisible
        {
            get => _isWindowVisible;
            set => SetProperty(ref _isWindowVisible, value);
        }

        public MainWindow()
        {
            InitializeComponent();

            //TrayIcon.PopupActivation = PopupActivationMode.LeftClick;
            //TrayIcon.ContextFlyout.ShowMode = FlyoutShowMode.Standard;

            // Init
            SetPowerPlan(); // Set Power Plan

           // PowerMode(); // Set Power Mode
        }

        private void PowerMode()
        {
                       // This is a placeholder for any logic related to Power Mode changes
            Debug.WriteLine("Power Mode changed.");

            PowerModeChanger.SetPowerModeToBestEfficiency();
        }

        private async void SetPowerPlan()
        {
            // MLAP has four power plans in the registry
            // 
            // Power Saver            a1841308-3541-4fab-bc81-f71556f20b4a
            // Balanced               381b4222-f694-41f0-9685-ff5bb260df2e
            // High Performance       8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
            // Ultimate Performance   e9a42b02-d5df-448d-aa00-03f14749eb61  - on MLAP
            //
            // Ultimate Performance   8c5e7fda-e8bf-4a96-b3b9-1b0c2d0f2d3a  - not on MLAP - sited on tinternet as Windows 10 Pro for Workstations only 


            // Power plans are stored in the registry under
            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes


            // Power Schemes (plans) & W11 Power Mode 
            // 
            // Power Mode is essentially a UI layer on top of Power Schemes, especially Balanced:
            // When you move the Power Mode slider, Windows tweaks processor and thermal policies within the Balanced plan.
            // So changing Power Mode doesn't switch the scheme, but modifies behavior within the active plan.
            //
            // When we change Power Plan to Balanced the Control Panel -> Power settings page
            // all the plans are removed from the Ui but if you refresh the page Balanced appears!  
            // So I can edit the plan in advanced settings TO QUIETEN THE FAN! ie I want to change the CPU Max to 60% and back to 100%
            //
            // If we set CPU Max @ 60% then this quietens the fan down and cools the Laptop 

            // How can I programatically set the CPU Min/Max in a certain power plan?
            //


            var power_saver = new PowerScheme
            {
                Name = "Power Saver",
                Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                IsActive = false
            };

            var balanced = new PowerScheme
            {
                Name = "Balanced",
                Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
                IsActive = true // Assume this is the active plan for demonstration
            };

            var high_performance = new PowerScheme
            {
                Name = "High Performance",
                Guid = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
                IsActive = false
            };

            var ultimate_performance = new PowerScheme
            {
                Name = "Ultimate Performance",
                Guid = Guid.Parse("e9a42b02-d5df-448d-aa00-03f14749eb61"),
                IsActive = false
            };

            // Ok so we cant set te Power Mode in C# :-(
            // so the best strategy is to switch between Power Saver Plan (low fan)
            // to Balanced Plan (high fan) - but with the Power Mode set to `Best Performance`
            // so even though the plan is `Balanced` we get the best performance
            // Power Mode is set in W11 Settings -> Power & Battery -> Power Mode

            // if (PowerPlanList.SelectedItem is not PowerPlan selected) return;

            var selected = power_saver; // For demo
            //var selected = balanced;
            //var selected = high_performance; 
            //var selected = ultimate_performance; // error using this plan



            try
            {
                Guid selectedScheme = selected.Guid;

                // Get CPU Max Percentage for DC and AC
                int dcCpuMax = PowerSchemeManager.GetCpuMax_DC(selectedScheme);
                int acCpuMax = PowerSchemeManager.GetCpuMax_AC(selectedScheme);

                // Set CPU Max Percentage for DC and AC
                PowerSchemeManager.SetCpuMax_DC(selectedScheme, 49);
                PowerSchemeManager.SetCpuMax_AC(selectedScheme, 85);


            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception while switching plans: {ex.Message}");
            }
        }




        [RelayCommand]
        private void DoSomething()
        {
            //TrayIconFoo.ContextFlyout.Hide();
            //TrayIconFoo.CloseTrayPopup();
            //MyMenuFlyout.Hide();
            Debug.WriteLine("DoSomething...");

        }

        private void myButton_Click(object sender, RoutedEventArgs e)
        {
            myButton.Content = "Clicked";
        }


        private void MyMenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
        {
            Debug.WriteLine($"Flyout Closing called..."); // this is not being called when the flyout is closed!

            args.Cancel = true; // Can we prevent the flyout from closing?
        }


        [RelayCommand]
        public void ShowHideWindow()
        {
            // If we try to show the context flyout on left click,
            // it does not work
            //
            bool LeftClickOpensContextFlyout = false;

            if (LeftClickOpensContextFlyout)
            {
                Debug.WriteLine($"Left: Show Context...");

               // MyMenuFlyout.ShowAt(TrayIcon);

            }
            else
            {
                Debug.WriteLine($"Left: Show/Hide Main...");

                var window = App.MainWindow;
                if (window == null)
                {
                    return;
                }

                if (window.Visible)
                {
                    window.Hide();
                }
                else
                {
                    window.Show();
                }
                IsWindowVisible = window.Visible;
            }
        }


        [RelayCommand]
        public void ExitApplication()
        {
            App.HandleClosedEvents = false;
          //  TrayIcon.Dispose();
            App.MainWindow?.Close();
        }


        // Cpu % Management 
        //
        [RelayCommand]
        public void GetCpuMax()
        {
        }

        [RelayCommand]
        public void CpuLow()
        {
        }

        [RelayCommand]
        public void CpuHigh()
        {
        }

    }
}
