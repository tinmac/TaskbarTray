using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        public BitmapImage SelectedImage => ConvertEnumToImage(SelectedPowerMode);


        [ObservableProperty]
        private PowerMode _selectedPowerMode;



        [ObservableProperty]
        private bool _isWindowVisible;

        [ObservableProperty]
        private PowerScheme _activeScheme;


        public void Init()
        {
            LoadPlansAsync();

        }

        public bool IsEco
        {
            get => SelectedPowerMode == PowerMode.Eco;
            set
            {
                if (value && SelectedPowerMode != PowerMode.Eco)
                    SelectedPowerMode = PowerMode.Eco;
                // Ignore false or redundant toggle
            }
        }

        public bool IsBalanced
        {
            get => SelectedPowerMode == PowerMode.Balanced;
            set
            {
                if (value && SelectedPowerMode != PowerMode.Balanced)
                    SelectedPowerMode = PowerMode.Balanced;
            }
        }

        public bool IsHigh
        {
            get => SelectedPowerMode == PowerMode.High;
            set
            {
                if (value && SelectedPowerMode != PowerMode.High)
                    SelectedPowerMode = PowerMode.High;
            }
        }

        [ObservableProperty]
        private bool _isSaverChecked;

        [ObservableProperty]
        private bool _isBalancedChecked;
   
        [ObservableProperty]
        private bool _isHighChecked;


        partial void OnSelectedPowerModeChanged(PowerMode oldValue, PowerMode newValue)
        {
            // Notify related booleans so the UI updates
            Debug.WriteLine($"On Selected Power Mode Changed...");

            OnPropertyChanged(nameof(SelectedImage));

            OnPropertyChanged(nameof(IsEco));
            OnPropertyChanged(nameof(IsBalanced));
            OnPropertyChanged(nameof(IsHigh));
        }



        private BitmapImage ConvertEnumToImage(PowerMode PowerMode)
        {
            string uri = PowerMode switch
            {
                PowerMode.Eco => "ms-appx:///Assets/gauge_low.ico",
                PowerMode.Balanced => "ms-appx:///Assets/Inactive.ico",
                PowerMode.High => "ms-appx:///Assets/gauge_high.ico",
                //  ImageSourceType.Ultimate_Icon => "ms-appx:///Assets/Red.ico",
                _ => throw new ArgumentOutOfRangeException()
            };
            return new BitmapImage(new Uri(uri));
        }


        // METHODS
        public async Task LoadPlansAsync()
        {
            try
            {
                var plans = await Task.Run(() => PowerSchemeManager.LoadPowerSchemes());

                ActiveScheme = plans.FirstOrDefault(p => p.IsActive);

                Debug.WriteLine($"\nActive Plan: {ActiveScheme?.Name}");

                if (ActiveScheme.PowerMode == PowerMode.Eco)
                {
                    IsEco = true;
                }
                else if (ActiveScheme.PowerMode == PowerMode.Balanced)
                {
                    IsBalanced = true;
                }
                else if (ActiveScheme.PowerMode == PowerMode.High)
                {
                    IsHigh = true;
                }

                // UpdateUi();
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
                    // UpdateUi();

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



        // RELAY COMMANDS

        [RelayCommand]
        public void Set_PowerSaver()
        {

            var power_saver = new PowerScheme
            {
                Name = "Power Saver",
                Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                IsActive = true
            };

            IsEco = true;

            // There is no need to set to true to show the check mark as its bound
            // to IsChecked of the ToggleMenuFlyoutItem (Power Saver in this case) 
            // ie we dont need to: IsPowerSaver_Selected = true;

            // Set the other ToggleMenuFlyoutItem's to false so it removes their check mark - or else 
            //IsHigh = false;
            //IsBalanced = false;

            // Change TrayIcon image
            // SelectedImageType = ImageSourceType.PowerSaver_Icon;

            SetPowerPlan(power_saver);
        }


        [RelayCommand]
        public void Set_Balanced()
        {
            var balanced = new PowerScheme
            {
                Name = "Balanced",
                Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
                IsActive = true // Assume this is the active plan for demonstration
            };

            IsBalanced = true;

            //IsEco = false;
            //IsHigh = false;

            // SelectedImageType = ImageSourceType.Balanced_Icon;

            SetPowerPlan(balanced);
        }

        [RelayCommand]
        public void Set_HighPerformance()
        {
            var high_performance = new PowerScheme
            {
                Name = "High Performance",
                Guid = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
                IsActive = false
            };

            IsHigh = true;

            //IsEco = false;
            //IsBalanced = false;

            //SelectedImageType = ImageSourceType.High_Icon;

            SetPowerPlan(high_performance);
        }


        //[RelayCommand]
        //public void Set_UltimatePerformance()
        //{
        //    var ultimate_performance = new PowerScheme
        //    {
        //        Name = "Ultimate Performance",
        //        Guid = Guid.Parse("e9a42b02-d5df-448d-aa00-03f14749eb61"),
        //        IsActive = false
        //    };

        //    IsHigh_Selected = false;
        //    IsBalanced_Selected = false;
        //    IsPowerSaver_Selected = false;


        //    SelectedImageType = ImageSourceType.Ultimate_Icon;

        //    SetPowerPlan(ultimate_performance);
        //}



        [RelayCommand]
        public void ToggleSpeed()
        {
            // Left click toggles between Power Saver and Balanced
            //
            if (IsEco)
            {
                Set_Balanced();
            }

            if (IsBalanced)
            {
                Set_PowerSaver();
            }
        }


         [RelayCommand]
        public void ShowHideWindow(bool show)
        {

            // MyMenuFlyout.ShowAt(TrayIcon);

            var window = App.MainWindow;
            if (window == null)
            {
                return;
            }

            if (show)
            {
                Debug.WriteLine($"Show Main Window...");


                window.Show();
            }
            else
            {
                Debug.WriteLine($"Hide Main Window...");

                window.Hide();
            }
            
            IsWindowVisible = window.Visible;
        }

    }

}

