using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Imaging;
using PowerPlanSwitcher;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaskbarTray.stuff;
using Windows.Devices.Power;
using Windows.Security.Isolation;

namespace TaskbarTray.Views
{
    public enum PowerMode
    {
        None,
        Eco,
        Balanced,
        High
    }

    public enum ImageSourceType
    {
        PowerSaver_Icon,
        Balanced_Icon,
        High_Icon
    }

    public partial class TrayIconVM : ObservableObject
    {
        [ObservableProperty]
        public BitmapImage _selectedImage;// => ConvertEnumToImage(ActiveScheme);


        [ObservableProperty]
        private PowerMode _selectedPowerMode;



        [ObservableProperty]
        private bool _show_OpenWindowMenuItem;

        [ObservableProperty]
        private PowerScheme _activeScheme;




        [ObservableProperty]
        private bool _isSaverChecked;

        [ObservableProperty]
        private bool _isBalancedChecked;

        [ObservableProperty]
        private bool _isHighChecked;

        [ObservableProperty]
        private string _batteryPercentage;


        List<PowerScheme> PowerPlans = new();


        public TrayIconVM()
        {

            WeakReferenceMessenger.Default.Register<MyMessage>(this, (r, message) =>
            {
                Debug.WriteLine($"Rx CloseMainWin: {message.Content}");
                Show_OpenWindowMenuItem = true;
            });

            #region old Messenger code

            //App.Messenger.Register<MyMessage>(this, (recipient, message) =>
            //{
            //    Console.WriteLine($"Received message: {message.Content}");
            //});

            //App.Messenger.Register<TrayIconVM, MyMessage, string>(this, "MyToken", (recipient, message) =>
            //{
            //    Debug.WriteLine($"Rx CloseMainWin: {message.Content}");

            //    Show_OpenWindowMenuItem = true;
            //});

            //App.Messenger.Register<TrayIconVM, Msg_CloseMainWin, string>(this, string.Empty, (recipient, message) =>
            //{
            //    // Handle the message  
            //    Debug.WriteLine($"Rx CloseMainWin: {message.CloseMainWin}");

            //    Show_OpenWindowMenuItem = true;
            //});

            #endregion

            Show_OpenWindowMenuItem = true; // Initially show the menu item to open the main window

            //GetBatteryPercentage();
        }


        // NEXT: Detect when user changes theme and change Icon to dark/wh
        // get battery level on opening context menu
        // other features?
        // package into MSIX
        //

        public void Init()
        {
            CreatePlans();

            LoadPlansAsync();

            GetBatteryPercentage();
        }

        private void CreatePlans()
        {
            var power_saver = new PowerScheme
            {
                Name = "Power Saver",
                Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                IsActive = true,
                PowerMode = PowerMode.Eco,
                IconPath_DarkFG = "ms-appx:///Assets/ico/gauge-min.ico",
                IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-min-wh.ico"
            };

            var balanced = new PowerScheme
            {
                Name = "Balanced",
                Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
                IsActive = true, // Assume this is the active plan for demonstration
                PowerMode = PowerMode.Balanced,
                IconPath_DarkFG = "ms-appx:///Assets/ico/gauge.ico",
                IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-wh.ico"
            };


            var high_performance = new PowerScheme
            {
                Name = "High Performance",
                Guid = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
                IsActive = false,
                PowerMode = PowerMode.High,
                IconPath_DarkFG = "ms-appx:///Assets/ico/gauge-max.ico",
                IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-max-wh.ico"
            };

            PowerPlans.Add(power_saver);
            PowerPlans.Add(balanced);
            PowerPlans.Add(high_performance);
        }


        private BitmapImage ConvertEnumToImage(PowerScheme Schene)
        {
            var PowerMode = Schene.PowerMode;

            Debug.WriteLine($"Active PowerMode {PowerMode}");

            if (PowerMode == PowerMode.None)
            {
                Debug.WriteLine($"PowerMode is None, returning null image.");
                return null; // No image for None mode
            }

            string uri = PowerMode switch
            {
                PowerMode.Eco => "ms-appx:///Assets/gauge_low.ico",
                PowerMode.Balanced => "ms-appx:///Assets/Inactive.ico",
                PowerMode.High => "ms-appx:///Assets/gauge_high.ico",
                //  ImageSourceType.Ultimate_Icon => "ms-appx:///Assets/Red.ico",
                //_ => throw new ArgumentOutOfRangeException()
            };
            return new BitmapImage(new Uri(uri));
        }


        // METHODS
        public async Task LoadPlansAsync()
        {
            try
            {
                var active_plan_guid = await Task.Run(() => PowerSchemeManager.GetActivePlanGuid());

                ActiveScheme = PowerPlans.First(p=> p.Guid == active_plan_guid);


                Debug.WriteLine($"\nInitial Plan: {ActiveScheme?.Name}");

                UpdateUi();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading power plans: {ex.Message}");
            }
        }


        public async void SetPowerPlan(PowerScheme Scheme)
        {
            #region Notes

            // MLAP has four power plans in the registry
            // 
            // Power Saver            a1841308-3541-4fab-bc81-f71556f20b4a
            // Balanced               381b4222-f694-41f0-9685-ff5bb260df2e
            // High Performance       8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
            // Ultimate Performance   8c5e7fda-e8bf-4a96-b3b9-1b0c2d0f2d3a  - not on MLAP - sited on tinternet as Windows 10 Pro for Workstations only 
            //
            // I havent included the plans below as they are not commonly used 
            //
            // found on my laptop but may not be present on all systems:
            // Ultimate Performance   e9a42b02-d5df-448d-aa00-03f14749eb61  - on MLAP
            //
            // may not be present on all systems, but found on some AMD systems:
            // AMD Ryzen Balanced     45bcc044-d885-43e2-8605-ee558b2a56b0 (varies by driver/version)


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

            #endregion


            ActiveScheme = Scheme;

            try
            {
                bool success = await Task.Run(() => PowerPlanManager.SetActivePowerPlan(Scheme.Guid));

                if (success)
                {
                    UpdateUi();

                    Debug.WriteLine($"\nSwitched to: {Scheme.Name}");
                }
                else
                {
                    Debug.WriteLine("Failed to set power plan.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception while switching plans: {ex.Message}");
            }
        }


        private void SetCPUPercentage()
        {
            Guid plan = PowerPlanManager.GetActivePlanGuid();

            int acCpu = PowerPlanManager.GetCpuMaxPercentage(plan);
            int dcCpu = PowerPlanManager.GetCpuMaxPercentageDC(plan);

            Console.WriteLine($"AC Max CPU: {acCpu}%");
            Console.WriteLine($"DC Max CPU: {dcCpu}%");

            PowerPlanManager.SetCpuMaxPercentage(plan, 65);    // AC
            PowerPlanManager.SetCpuMaxPercentageDC(plan, 63);  // DC
        }

        private void GetBatteryPercentage()
        {
            var report = Battery.AggregateBattery.GetReport();
            var remaining = report.RemainingCapacityInMilliwattHours;
            var full = report.FullChargeCapacityInMilliwattHours;

            var left = report.Status;

            Debug.WriteLine($"status {left}");

            if (remaining.HasValue && full.HasValue && full != 0)
            {
                int percentage = (int)((remaining.Value / (double)full.Value) * 100);
                BatteryPercentage = $"Battery: {percentage}%";
            }
            else
            {
                BatteryPercentage = "Battery info not available.";
            }


            Debug.WriteLine($"{BatteryPercentage}");
        }

        private void UpdateUi()
        {
            // There are two settings App Dark & Windows Dark
            //
            // To discover if the Task Bar is Dark/Light for .ico Tray Icons use ThemeHelper.IsWindowsInDarkMode();

            var isWinDark = ThemeHelper.IsWindowsInDarkMode();// Use to work out if we use Light/Dark Tray icons

            Debug.WriteLine($"Win Dark {isWinDark}");

            if (isWinDark)
            {
                var img = new BitmapImage(new Uri(ActiveScheme.IconPath_WhiteFG));

                SelectedImage = img;
            }
            else
            {
                var img = new BitmapImage(new Uri(ActiveScheme.IconPath_DarkFG));

                SelectedImage = img;
            }

            if (ActiveScheme.PowerMode == PowerMode.Eco)
            {
                IsSaverChecked = true;
                IsBalancedChecked = false;
                IsHighChecked = false;
            }
            else if (ActiveScheme.PowerMode == PowerMode.Balanced)
            {
                IsSaverChecked = false;
                IsBalancedChecked = true;
                IsHighChecked = false;
            }
            else if (ActiveScheme.PowerMode == PowerMode.High)
            {
                IsSaverChecked = false;
                IsBalancedChecked = false;
                IsHighChecked = true;
            }
            else if (ActiveScheme.PowerMode == PowerMode.None)
            {
                //IsBalanced = false;

                IsSaverChecked = false;
                IsBalancedChecked = false;
                IsHighChecked = false;
            }
            else
            {
                //IsBalanced = false;
                IsSaverChecked = false;
                IsBalancedChecked = false;
                IsHighChecked = false; // No known plan is active

                Debug.WriteLine($"No known plan!");
            }

            GetBatteryPercentage();

        }



        // RELAY COMMANDS

        [RelayCommand]
        public void Set_PowerSaver()
        {
            if (ActiveScheme.PowerMode == PowerMode.Eco)
            {
                UpdateUi();
                return;
            }

            var power_saver = PowerPlans.First(p => p.PowerMode == PowerMode.Eco);
            SetPowerPlan(power_saver);
        }


        [RelayCommand]
        public void Set_Balanced()
        {
            if (ActiveScheme.PowerMode == PowerMode.Balanced)
            {
                UpdateUi();
                return;
            }

            var balanced = PowerPlans.First(p => p.PowerMode == PowerMode.Balanced);
            SetPowerPlan(balanced);
        }

        [RelayCommand]
        public void Set_HighPerformance()
        {
            if (ActiveScheme.PowerMode == PowerMode.High)
            {
                UpdateUi();
                return;
            }

            var high_performance = PowerPlans.First(p => p.PowerMode == PowerMode.High);
            SetPowerPlan(high_performance);
        }




        [RelayCommand]
        public void ToggleSpeed()
        {
            // Left click toggles between Power Saver and Balanced
            //
            if (IsSaverChecked)
            {
                Set_Balanced();
            }

            if (IsBalancedChecked)
            {
                Set_PowerSaver();
            }
        }


        [RelayCommand]
        public void ShowHideWindow(string show)
        {

            // MyMenuFlyout.ShowAt(TrayIcon);

            var window = App.MainWindow;
            if (window == null)
            {
                return;
            }

            if (show == "True")
            {
                Debug.WriteLine($"Show Main Window...");

                window.Show();

                Show_OpenWindowMenuItem = false;
            }
            else
            {
                Debug.WriteLine($"Hide Main Window...");

                window.Hide();

                Show_OpenWindowMenuItem = true;
            }

        }

    }

}

