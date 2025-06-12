using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI;
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
using TaskbarTray.Services;
using TaskbarTray.Views;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;



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

            try
            {

                // PersistenceId - WONT WORK if App is Unpackaged
                //
                // so we use T10's FIleService which saves for Packaged or Unpackaged
                //
                var settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
                var winData = settingsService.GetWindowDataAsync().Result;

                if (winData == null)
                {
                    // Not saved so eithe rfirst run or data deleted, so window will use defaults
                }
                else
                {
                    // Apply  
                    Debug.WriteLine($"Fetched X [{winData.X}]  Y [{winData.Y}]  W [{winData.Width}]  H [{winData.Height}]");
                    this.MoveAndResize(winData.X, winData.Y, winData.Width, winData.Height);
                }




                Title = "Taskbar Tray App"; // Set the title of the window
                                            //Icon = new BitmapImage(new Uri("ms-appx:///Assets/Icons/app_icon.ico")) // Set the icon if needed
                Content = new MainView();

                Closed += (sender, args) =>
                {
                    // Save Size & Position
                    //
                    var position = this.AppWindow.Position;
                    int x = position.X;
                    int y = position.Y;

                    WindowsData saveWinData = new WindowsData { Width = this.Width, Height = this.Height, X = position.X, Y = position.Y };

                    Debug.WriteLine($"Saving X [{saveWinData.X}]  Y [{saveWinData.Y}]  W [{saveWinData.Width}]  H [{saveWinData.Height}]");

                    settingsService.SetWindowDataAsync(saveWinData);


                    // Send Msg so other Tray Icon's Context Menu can alter the Show/Hide Menu Item Text
                    //
                    WeakReferenceMessenger.Default.Send(new MyMessage { CloseMainWin = true });
                    if (App.HandleClosedEvents)
                    {
                        args.Handled = true;
                        App.Main_Window.Hide();
                    }
                };

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"exception: {ex}");
                throw;
            }

        }
    }
}
