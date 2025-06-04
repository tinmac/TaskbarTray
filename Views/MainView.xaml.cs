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
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace TaskbarTray.Views
{

    public sealed partial class MainView// : Page
    {
        public MainView()
        {
            InitializeComponent();

            NavigationView.ItemInvoked += NavigationView_ItemInvoked;
        }

        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var options = new FrameNavigationOptions
            {
                TransitionInfoOverride = args.RecommendedNavigationTransitionInfo,
            };


            var invokedItem = (string)args.InvokedItem;
            Debug.WriteLine($"Invoked {invokedItem}");

            switch (invokedItem)
            {
                case "Notifications":
                    _ = NavigationViewFrame.NavigateToType(typeof(NotificationView), null, options);
                    ((NotificationView)NavigationViewFrame.Content).TrayIcon = TrayIconView.TrayIcon;
                    break;
            }
        }
    }
}
