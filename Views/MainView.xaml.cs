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
using PowerSwitch.SensorPipeService;
using PowerSwitch.stuff;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace PowerSwitch.Views
{

    public sealed partial class MainView// : Page
    {
        public MainView()
        {
            InitializeComponent();

            NavigationView.ItemInvoked += NavigationView_ItemInvoked;


        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Navigate to the default page on app startup
            NavigationViewFrame.Navigate(typeof(SensorsPipeView));

            // Highlight Selectd Page in the left nav menu (must do both)
            var item = NavigationView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(x => (string)x.Tag == nameof(SensorsPipeView));

            if (item != null)
            {
                NavigationView.SelectedItem = item;
            }
        }


        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var options = new FrameNavigationOptions
            {
                TransitionInfoOverride = args.RecommendedNavigationTransitionInfo,
            };


            var invokedItem = (string)args.InvokedItem;

            switch (invokedItem)
            {
                case "Sensors":
                    _ = NavigationViewFrame.NavigateToType(typeof(SensorsPipeView), null, options);
                    ((SensorsPipeView)NavigationViewFrame.Content).TrayIcon = TrayIconView.TrayIcon;
                    break;

                //case "Sensors old":
                //    _ = NavigationViewFrame.NavigateToType(typeof(Sensors), null, options);
                //    ((Sensors)NavigationViewFrame.Content).TrayIcon = TrayIconView.TrayIcon;
                //    break;

                case "Notifications":
                    _ = NavigationViewFrame.NavigateToType(typeof(NotificationView), null, options);
                    ((NotificationView)NavigationViewFrame.Content).TrayIcon = TrayIconView.TrayIcon;
                    break;

                case "Settings":
                    _ = NavigationViewFrame.NavigateToType(typeof(Settings), null, options);
                    ((Settings)NavigationViewFrame.Content).TrayIcon = TrayIconView.TrayIcon;
                    break;

            }

        }

    }
}
