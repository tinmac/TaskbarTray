using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerPlanSwitcher;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;



namespace TaskbarTray.Views;


[ObservableObject]
public sealed partial class TrayIconView : UserControl
{

    private bool _isPowerSaver;
    public bool IsBalanced
    {
        get => _isPowerSaver;
        set => SetProperty(ref _isPowerSaver, value);
    }


    private bool _isWindowVisible;
    public bool IsWindowVisible
    {
        get => _isWindowVisible;
        set => SetProperty(ref _isWindowVisible, value);
    }


    private bool _isSaverChecked;
    public bool IsSaverChecked
    {
        get => _isSaverChecked;
        set => SetProperty(ref _isSaverChecked, value);
    }

    private bool _isBalancedChecked;
    public bool IsBalancedChecked
    {
        get => _isBalancedChecked;
        set => SetProperty(ref _isBalancedChecked, value);
    }

    private bool _isHighChecked;
    public bool IsHighChecked
    {
        get => _isHighChecked;
        set => SetProperty(ref _isHighChecked, value);
    }

    private PowerScheme _activeScheme;
    public PowerScheme ActiveScheme
    {
        get => _activeScheme;
        set => SetProperty(ref _activeScheme, value);
    }


    public TrayIconVM ViewModel { get; } = new TrayIconVM();

    public TrayIconView()
    {
        InitializeComponent();

        MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

        TrayIcon.PopupActivation = PopupActivationMode.LeftClick;

        IsSaverChecked = false;
        IsBalancedChecked = false;
        IsHighChecked = false;

        var currnet_plan = LoadPlansAsync();

        //SetPowerPlan(); 

        //SetCPUPercentage(); // Set CPU Max %
    }

    private async Task LoadPlansAsync()
    {
        try
        {
            var plans = await Task.Run(() => PowerPlanManager.LoadPowerPlans());

            ActiveScheme = plans.FirstOrDefault(p => p.IsActive);

            Debug.WriteLine($"\nActive Plan: {ActiveScheme?.Name}");

            UpdateUi();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading power plans: {ex.Message}");
        }
    }


    private void UpdateUi()
    {
        if (ActiveScheme.DisplayName.ToLower().Contains("power saver"))
        {
            IsBalanced = false;

            IsSaverChecked = true;
            IsBalancedChecked = false;
            IsHighChecked = false;

        }
        else if (ActiveScheme.DisplayName.ToLower().Contains("balanced"))
        {
            IsBalanced = true;

            IsSaverChecked = false;
            IsBalancedChecked = true;
            IsHighChecked = false;
        }
        else if (ActiveScheme.DisplayName.ToLower().Contains("high performance"))
        {
            IsBalanced = true;

            IsSaverChecked = false;
            IsBalancedChecked = false;
            IsHighChecked = true;
        }
        else if (ActiveScheme.DisplayName.ToLower().Contains("ultimate performance"))
        {
            IsBalanced = true;

            IsSaverChecked = false;
            IsBalancedChecked = false;
            IsHighChecked = false;
        }
        else
        {
            IsBalanced = false;
            IsSaverChecked = false;
            IsBalancedChecked = false;
            IsHighChecked = false; // No known plan is active

            Debug.WriteLine($"No known plan!");
        }

    }


    private async void SetPowerPlan(PowerScheme Scheme)
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





    [RelayCommand]
    public void PowerSaver()
    {
        var power_saver = new PowerScheme
        {
            Name = "Power Saver",
            Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
            IsActive = true
        };

        SetPowerPlan(power_saver);
    }


    [RelayCommand]
    public void Balanced()
    {
        var balanced = new PowerScheme
        {
            Name = "Balanced",
            Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
            IsActive = true // Assume this is the active plan for demonstration
        };

        SetPowerPlan(balanced);
    }

    [RelayCommand]
    public void HighPerformance()
    {
        var high_performance = new PowerScheme
        {
            Name = "High Performance",
            Guid = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
            IsActive = false
        };

        SetPowerPlan(high_performance);
    }

    [RelayCommand]
    public void UltimatePerformance()
    {
        var high_performance = new PowerScheme
        {
            Name = "Ultimate Performance",
            Guid = Guid.Parse("e9a42b02-d5df-448d-aa00-03f14749eb61"),
            IsActive = false
        };

        SetPowerPlan(high_performance);
    }



    [RelayCommand]
    public void ToggleSpeed()
    {
        // At the mo we toggle between Power Saver and Balanced
        // IsBalanced is bound to the Icon in the tray, but it really needs 4 icons bound to an enum of PowerModes
        //
        if (IsBalanced)
        {
            PowerSaver();

        }
        else
        {
            Balanced();
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

            MyMenuFlyout.ShowAt(TrayIcon);

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
        TrayIcon.Dispose();
        App.MainWindow?.Close();
    }



    private void MyMenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        Debug.WriteLine($"Flyout Closing called..."); // this is not beoing called when the flyout is closed!

        args.Cancel = true; // Can we prevent the flyout from closing?
    }
}

