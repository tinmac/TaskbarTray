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
    // Bind ViewModel to the View
    public TrayIconVM ViewModel { get; } = new TrayIconVM();






    public TrayIconView()
    {
        InitializeComponent();

        MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

        TrayIcon.PopupActivation = PopupActivationMode.LeftClick;

        //IsSaverChecked = false;
        //IsBalancedChecked = false;
        //IsHighChecked = false;

        var currnet_plan = ViewModel.LoadPlansAsync();

        //SetPowerPlan(); 

        //SetCPUPercentage(); // Set CPU Max %
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

