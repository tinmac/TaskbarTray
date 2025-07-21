using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PowerSwitch.ViewModels;

namespace PowerSwitch.Views;


public sealed partial class TrayIconView : UserControl
{
    private readonly ILogger<TrayIconView> _logr;

    // Bind ViewModel to the View
    public TrayIconVM ViewModel { get; }// = new TrayIconVM();


    // Constructor
    public TrayIconView()
    {
        ViewModel = Ioc.Default.GetService<TrayIconVM>()!;
        ViewModel.TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
      
        InitializeComponent();

        _logr = Ioc.Default.GetRequiredService<ILogger<TrayIconView>>();


        //MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

        TrayIcon.PopupActivation = PopupActivationMode.LeftClick;

        ViewModel.InitAsync();
    }


    [RelayCommand]
    public void ExitApplication()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this); // Unregister all messages for this view
        App.HandleClosedEvents = false;
        TrayIcon.Dispose();
        App.Main_Window?.Close();
    }


    //[RelayCommand]
    private void MyMenuFlyout_Closing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        _logr.LogInformation($"Flyout Closing called..."); // this is not beoing called when the flyout is closed!

        args.Cancel = true; // Can we prevent the flyout from closing?
    }
}

