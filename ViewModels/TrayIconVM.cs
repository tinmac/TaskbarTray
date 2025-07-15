using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaskbarTray.Power;
using TaskbarTray.stuff;
using Windows.Devices.Power;
using Windows.Security.Isolation;
using Windows.System.Power;

namespace TaskbarTray.ViewModels
{
    public enum PowerMode
    {
        None,
        Eco,
        Balanced,
        High
    }


    public partial class TrayIconVM : ObservableObject
    {
        private readonly ILogger<TrayIconVM> _logr;

        // to avoid warning `MVVMTK0045` use Communitty Toolkit 8.3.2 as 8.4.0 gives the warning  see https://stackoverflow.com/a/79302048/425357

        //[ObservableProperty]
        //public partial BitmapImage SelectedImage { get; set; }

        [ObservableProperty]
        private BitmapImage _selectedImage;


        [ObservableProperty]
        private PowerMode _selectedPowerMode;



        [ObservableProperty]
        private bool _show_OpenWindowMenuItem;

        [ObservableProperty]
        private PowerPlan _activeScheme;




        [ObservableProperty]
        private bool _isSaverChecked;

        [ObservableProperty]
        private bool _isBalancedChecked;

        [ObservableProperty]
        private bool _isHighChecked;

        [ObservableProperty]
        private string _batteryPercentage;


        List<PowerPlan> PowerPlans = new();

        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }


        // Constructor
        public TrayIconVM(ILogger<TrayIconVM> logr)
        {
            _logr = logr;

            //_logr = Ioc.Default.GetRequiredService<ILogger<TrayIconVM>>();

            SelectedImage = new BitmapImage(new Uri("ms-appx:///Assets/ico/gauge.ico")); // Default icon
            ActiveScheme = new PowerPlan();
            SelectedPowerMode = PowerMode.None;
            Show_OpenWindowMenuItem = false; // Initially hide the menu item to open the main window
            BatteryPercentage = string.Empty;


            WeakReferenceMessenger.Default.Register<MyMessage>(this, (r, message) =>
            {
                if (message.CloseMainWin)
                    Show_OpenWindowMenuItem = true;

                TheDispatcher.TryEnqueue(() =>
                {
                    if (SelectedImage == null)
                        return;

                    // Detect theme change by user
                    //
                    if (message.ThemeChanged_Light)
                    {
                        // we need white foreground on Dark themem bg
                        SelectedImage = new BitmapImage(new Uri(ActiveScheme.IconPath_DarkFG));
                        _logr.LogInformation($" Switched to dark foreground icons");
                    }
                    else
                    {
                        // we need dark foreground on Light theme bg
                        SelectedImage = new BitmapImage(new Uri(ActiveScheme.IconPath_WhiteFG));
                        _logr.LogInformation($" Switched to light foreground icons");
                    }
                });

            });

            #region old Messenger code

            //App.Messenger.Register<MyMessage>(this, (recipient, message) =>
            //{
            //    Console.WriteLine($"Received message: {message.Content}");
            //});

            //App.Messenger.Register<TrayIconVM, MyMessage, string>(this, "MyToken", (recipient, message) =>
            //{
            //    Log.Information($"Rx CloseMainWin: {message.Content}");

            //    Show_OpenWindowMenuItem = true;
            //});

            //App.Messenger.Register<TrayIconVM, Msg_CloseMainWin, string>(this, string.Empty, (recipient, message) =>
            //{
            //    // Handle the message  
            //    Log.Information($"Rx CloseMainWin: {message.CloseMainWin}");

            //    Show_OpenWindowMenuItem = true;
            //});

            #endregion

            Show_OpenWindowMenuItem = true; // Initially show the menu item to open the main window

            // Subscribe to changes
            PowerManager.BatteryStatusChanged += OnBatteryStatusChanged;
            PowerManager.PowerSupplyStatusChanged += OnPowerSupplyStatusChanged;

            // WHAT DO YOU WANT TO DO WHEN ONE OF THESE EVENTS IS TRIGGERED????
            //
            // change Power mode? not keen on this but could be a v2 feature
        }

        private async void OnBatteryStatusChanged(object sender, object e)
        {
            try
            {
                _logr.LogInformation("Battery status changed");

                TheDispatcher.TryEnqueue(() =>
                {
                    GetBatteryPercentage();
                });
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception handling battery status change");
            }
        }

        private async void OnPowerSupplyStatusChanged(object sender, object e)
        {
            try
            {
                _logr.LogInformation("Power supply status changed");
                TheDispatcher.TryEnqueue(() =>
                {
                    GetBatteryPercentage();
                });
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception handling power supply status change");
            }
        }


        public void Init()
        {
            try
            {
                CreatePlans();
                LoadPlansAsync();
                //GetBatteryPercentage();
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception during TrayIconVM initialization");
            }
        }

        private void CreatePlans()
        {
            try
            {
                _logr.LogInformation("Creating power plans");
                var power_saver = new PowerPlan
                {
                    Name = "Power Saver",
                    Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                    IsActive = true,
                    PowerMode = PowerMode.Eco,
                    IconPath_DarkFG = "ms-appx:///Assets/ico/gauge-min.ico",
                    IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-min-wh.ico"
                };

                var balanced = new PowerPlan
                {
                    Name = "Balanced",
                    Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
                    IsActive = true, // Assume this is the active plan for demonstration
                    PowerMode = PowerMode.Balanced,
                    IconPath_DarkFG = "ms-appx:///Assets/ico/gauge.ico",
                    IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-wh.ico"
                };


                var high_performance = new PowerPlan
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
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception creating power plans");
                throw; // Re-throw as this is a critical initialization step
            }
        }


        private BitmapImage ConvertEnumToImage(PowerPlan Schene)
        {
            var PowerMode = Schene.PowerMode;

            _logr.LogInformation($"Active PowerMode {PowerMode}");

            if (PowerMode == PowerMode.None)
            {
                _logr.LogInformation($"PowerMode is None, returning null image.");
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
                var active_plan_guid = await Task.Run(() => PowerPlanManager.GetActivePlanGuid());
                ActiveScheme = PowerPlans.First(p => p.Guid == active_plan_guid);
                _logr.LogInformation($"Loaded Plan: {ActiveScheme?.Name}");
                UpdateUi();
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception loading power plans");
                _logr.LogError($"Exception loading power plans: {ex.Message}");
            }
        }


        public async void SetPowerPlan(PowerPlan Scheme)
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

            try
            {
                ActiveScheme = Scheme;

                bool success = await Task.Run(() => PowerPlanManager.SetActivePowerPlan(Scheme.Guid));

                if (success)
                {
                    UpdateUi();
                    _logr.LogInformation($"Switched to: {Scheme.Name}");
                }
                else
                {
                    _logr.LogWarning("Failed to set power plan to {PlanName}", Scheme.Name);
                    _logr.LogWarning($"Failed to set power plan.");
                }
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception setting power plan to {PlanName}", Scheme.Name);
                _logr.LogError($"Exception while switching plans: {ex.Message}");
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
            try
            {
                var report = Battery.AggregateBattery.GetReport();
                var remaining = report.RemainingCapacityInMilliwattHours;
                var full = report.FullChargeCapacityInMilliwattHours;
                var status = report.Status;

                if (remaining.HasValue && full.HasValue && full != 0)
                {
                    int percentage = (int)((remaining.Value / (double)full.Value) * 100);
                    BatteryPercentage = $"Battery: {percentage}% ({status})";
                }
                else
                {
                    BatteryPercentage = "Battery info not available.";
                }

                _logr.LogInformation($"{BatteryPercentage}");
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception getting battery percentage");
                BatteryPercentage = "Exception getting battery info";
            }
        }

        private void UpdateUi()
        {
            try
            {
                var isWinDark = ThemeHelper.IsWindowsInDarkMode();
                //_logr.LogInformation($"Windows dark mode: {isWinDark}");

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

                    _logr.LogInformation($"No known plan!");
                }

                GetBatteryPercentage();
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception updating UI");
            }
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
            try
            {
                if (show == "True")
                {
                    _logr.LogInformation("Showing main window");
                    _logr.LogInformation($"Show Main Window...");

                    if (App.Main_Window == null)
                    {
                        App.Main_Window = new MainWindow();
                    }
                    App.Main_Window.Show();
                    Show_OpenWindowMenuItem = false;
                }
                else
                {
                    _logr.LogInformation("Hiding main window");
                    _logr.LogInformation($"Hide Main Window...");
                    App.Main_Window?.Close();
                    Show_OpenWindowMenuItem = true;
                }
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception showing/hiding window");
            }
        }


        [RelayCommand]
        public void OpenContextMenu()
        {
            GetBatteryPercentage();
        }

    }

}

