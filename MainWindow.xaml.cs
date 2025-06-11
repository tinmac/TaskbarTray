using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TaskbarTray.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TaskbarTray
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WindowEx
    {
        public MainWindow()
        {
            InitializeComponent();

            // PersistenceId Wont work if App is Unpackaged
            //
            var manager = WinUIEx.WindowManager.Get(this);
            manager.PersistenceId = "MainWindowPersistanceId";
            manager.MinWidth = 400; // Set the minimum width of the window
            manager.MinHeight = 300; // Set the minimum height of the window


            Title = "Taskbar Tray App"; // Set the title of the window
            //Icon = new BitmapImage(new Uri("ms-appx:///Assets/Icons/app_icon.ico")) // Set the icon if needed
           // Content = new MainView();

            Closed += (sender, args) =>
            {
                WeakReferenceMessenger.Default.Send(new MyMessage { CloseMainWin = true });
                if (App.HandleClosedEvents)
                {
                    args.Handled = true;
                    App.Main_Window.Hide();
                }
            };
        }
    }
}
