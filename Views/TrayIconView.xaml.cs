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
        InitializeComponent();

        _logr = Ioc.Default.GetRequiredService<ILogger<TrayIconView>>();


        ViewModel.TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        //MyMenuFlyout.ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway;

        TrayIcon.PopupActivation = PopupActivationMode.LeftClick;

        ViewModel.InitAsync();
    }

    [RelayCommand]
    public void RebuildMenuFlyout()
    {
        #region Why are we creating the FlyoutMenu in code behind?
        
        // We have to build the Context Flyout Menu in code behind because there is no way to have the MenuFlyoutItems Check mark change using binding
        // see the ChatGPT chat -> Enum Image Source Selection
        // ChatGPT's Conclusion was: It�s likely MenuFlyout caching

        #endregion

        _logr.LogInformation($"Right Tapped called...");

        var flyout = new MenuFlyout();

        // Add Exit Menu item
        var ext_item = new MenuFlyoutItem { Text = "Exit",  };
        ext_item.Click += (_, _) => { ExitApplication(); };
        flyout.Items.Add(ext_item);

        // Show/Hide Main screen Menu item

        //if (!ViewModel.IsWindowVisible)
        //{
        //    var window_item = new MenuFlyoutItem { Text = "Show Main Window", };
        //    window_item.Click += (_, _) => { ViewModel.ShowHideWindow(true); };
        //    flyout.Items.Add(window_item);
        //}
        //else
        //{
        //    var window_item = new MenuFlyoutItem { Text = "Hide Main Window", };
        //    window_item.Click += (_, _) => { ViewModel.ShowHideWindow(false); };
        //    flyout.Items.Add(window_item);
        //}

        // Add Power Mode Menu items

        foreach (PowerMode mode in Enum.GetValues(typeof(PowerMode)))
        {
            if (mode == PowerMode.None)
                continue; // Skip None mode, as it is not a valid power mode

            var item = new MenuFlyoutItem
            {
                Text = mode.ToString(),
            };

            item.Click += (_, _) =>
            {
                ViewModel.SelectedPowerMode = mode;
            };

            flyout.Items.Add(item);
        }


        flyout.XamlRoot = App.Main_Window?.Content.XamlRoot ?? this.XamlRoot; // Set the XamlRoot for the flyout to ensure it displays correctly in the context of the current view
        flyout.XamlRoot = TrayIcon.XamlRoot;
        //TrayIcon.XamlRoot = this.XamlRoot; // Set the XamlRoot for the flyout to ensure it displays correctly in the context of the current view

        TrayIcon.ContextFlyout = flyout;
        flyout.ShowAt(TrayIcon);
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

