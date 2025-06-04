using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PowerPlanSwitcher;
using System;
using System.Diagnostics;
using System.Threading.Tasks;



namespace TaskbarTray.Views;


[ObservableObject]
public sealed partial class TrayIconView : UserControl
{

    private bool _isWindowVisible;
    public bool IsWindowVisible
    {
        get => _isWindowVisible;
        set => SetProperty(ref _isWindowVisible, value);
    }

    private bool _isOptionOneChecked;
    public bool IsOptionOneChecked
    {
        get => _isOptionOneChecked;
        set => SetProperty(ref _isOptionOneChecked, value);
    }

    private bool _isOptionTwoChecked;
    public bool IsOptionTwoChecked
    {
        get => _isOptionTwoChecked;
        set => SetProperty(ref _isOptionTwoChecked, value);
    }

    private bool _isOptionThreeChecked;
    public bool IsOptionThreeChecked
    {
        get => _isOptionThreeChecked;
        set => SetProperty(ref _isOptionThreeChecked, value);
    }


    public TrayIconView()
    {
        InitializeComponent();

        MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

        TrayIcon.PopupActivation = PopupActivationMode.LeftClick;

        var currnet_plan = LoadPlansAsync();


        //SetPowerPlan(); 

        SetCPUPercentage(); // Set CPU Max %
    }

    private async Task LoadPlansAsync()
    {
       // PowerPlanList.ItemsSource = null;
        try
        {
            var plans = await Task.Run(() => PowerPlanManager.LoadPowerPlans());
            //List<PowerPlan> plans = await Task.Run(() => PowerPlanManager.LoadPowerPlans());
            //PowerPlanList.ItemsSource = plans;
           // Debug.WriteLine($"\n");
            foreach (var plan in plans)
            {
                Debug.WriteLine($"Plan: {plan.Name} - Active: {plan.IsActive} - {plan.Guid}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading power plans: {ex.Message}");
        }
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

        // if (PowerPlanList.SelectedItem is not PowerPlan selected) return;

        //var selected = power_saver; // For demo
        var selected = balanced; 
        //var selected = high_performance; 
        //var selected = ultimate_performance; // error using this plan



        try
        {
            bool success = await Task.Run(() => PowerPlanManager.SetActivePowerPlan(selected.Guid));

            if (success)
            {
                Debug.WriteLine($"\nSwitched to: {selected.Name}");
                await LoadPlansAsync();
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
    public void OptionOne()
    {
        DisplayValues();
    }


    [RelayCommand]
    public void OptionTwo()
    {
        DisplayValues();
    }


    [RelayCommand]
    public void OptionThree()
    {
        DisplayValues();
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


    private void DisplayValues()
    {
        Debug.WriteLine($"Option  [1 {IsOptionOneChecked}]  [2 {IsOptionTwoChecked}]  [3 {IsOptionThreeChecked}]");
    }


    private void MyMenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        Debug.WriteLine($"Flyout Closing called..."); // this is not beoing called when the flyout is closed!

        args.Cancel = true; // Can we prevent the flyout from closing?
    }
}

