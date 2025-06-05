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

namespace TaskbarTray.Views
{
    public enum ImageSourceType
    {
        Ultimate_Icon,
        High_Icon,
        Balanced_Icon,
        PowerSaver_Icon
    }

    public partial class TrayIconVM : ObservableObject
    {
        public BitmapImage SelectedImage => ConvertEnumToImage(SelectedImageType);

        [ObservableProperty]
        private ImageSourceType selectedImageType = ImageSourceType.Ultimate_Icon;



        [ObservableProperty]
        private bool _isWindowVisible;

        [ObservableProperty]
        private PowerScheme _activeScheme;



        [ObservableProperty]
        private bool _isUltimate_Selected;

        [ObservableProperty]
        private bool _isHigh_Selected;

        [ObservableProperty]
        private bool _isBalanced_Selected;

        [ObservableProperty]
        private bool _isPowerSaver_Selected;


        partial void OnSelectedImageTypeChanged(ImageSourceType oldValue, ImageSourceType newValue)
        {
            // This method changes the TrayIcon image, if we comment it out th eocon doesnt change
            //
            // calling OnPropertyChanged here will raise the PropertyChanged event of the Property
            //
            // its wired up in the partial class created by the [ObservableProperty] attribute of selectedImageType above
            //

            Debug.WriteLine($"On Selected Image Type Changed...");

            OnPropertyChanged(nameof(SelectedImage));
            OnPropertyChanged(nameof(IsHigh_Selected));
            OnPropertyChanged(nameof(IsBalanced_Selected));
            OnPropertyChanged(nameof(IsPowerSaver_Selected));
            OnPropertyChanged(nameof(IsUltimate_Selected));
        }

        private BitmapImage ConvertEnumToImage(ImageSourceType sourceType)
        {
            string uri = sourceType switch
            {
                ImageSourceType.Ultimate_Icon => "ms-appx:///Assets/Red.ico",
                ImageSourceType.High_Icon => "ms-appx:///Assets/gauge_high.ico",
                ImageSourceType.Balanced_Icon => "ms-appx:///Assets/Inactive.ico",
                ImageSourceType.PowerSaver_Icon => "ms-appx:///Assets/gauge_low.ico",
                _ => throw new ArgumentOutOfRangeException()
            };
            return new BitmapImage(new Uri(uri));
        }


        // METHODS
        public async Task LoadPlansAsync()
        {
            try
            {
                var plans = await Task.Run(() => PowerPlanManager.LoadPowerPlans());

                ActiveScheme = plans.FirstOrDefault(p => p.IsActive);

                Debug.WriteLine($"\nActive Plan: {ActiveScheme?.Name}");

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
            // Ultimate Performance   e9a42b02-d5df-448d-aa00-03f14749eb61  - on MLAP
            //
            // AMD Ryzen Balanced     45bcc044-d885-43e2-8605-ee558b2a56b0 (varies by driver/version)
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
            Debug.WriteLine($"SelectedImageType {SelectedImageType}");
            if (SelectedImageType == ImageSourceType.PowerSaver_Icon)
                return;

            var power_saver = new PowerScheme
            {
                Name = "Power Saver",
                Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                IsActive = true
            };

            // There is no need to set to true to show the check mark as its bound
            // to IsChecked of the ToggleMenuFlyoutItem (Power Saver in this case) 
            // ie we dont need to: IsPowerSaver_Selected = true;

            // Set the other ToggleMenuFlyoutItem's to false so it removes their check mark - or else 
            IsUltimate_Selected = false;
            IsHigh_Selected = false;
            IsBalanced_Selected = false;

            // Change TrayIcon image
            SelectedImageType = ImageSourceType.PowerSaver_Icon;

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

            IsUltimate_Selected = false;
            IsHigh_Selected = false;
            IsPowerSaver_Selected = false;

            SelectedImageType = ImageSourceType.Balanced_Icon;

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

            IsUltimate_Selected = false;
            IsBalanced_Selected = false;
            IsPowerSaver_Selected = false;

            SelectedImageType = ImageSourceType.High_Icon;

            SetPowerPlan(high_performance);
        }


        [RelayCommand]
        public void Set_UltimatePerformance()
        {
            var ultimate_performance = new PowerScheme
            {
                Name = "Ultimate Performance",
                Guid = Guid.Parse("e9a42b02-d5df-448d-aa00-03f14749eb61"),
                IsActive = false
            };

            IsHigh_Selected = false;
            IsBalanced_Selected = false;
            IsPowerSaver_Selected = false;


            SelectedImageType = ImageSourceType.Ultimate_Icon;

            SetPowerPlan(ultimate_performance);
        }



        [RelayCommand]
        public void ToggleSpeed()
        {
            // Left click toggles between Power Saver and Balanced
            //
            if (IsPowerSaver_Selected)
            {
                Set_Balanced();
            }

            if (IsBalanced_Selected)
            {
                Set_PowerSaver();
            }
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

    }
}

