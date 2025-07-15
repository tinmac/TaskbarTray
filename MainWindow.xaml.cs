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
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
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
        private bool _initialized = false;

        ISettingsService _settingsService;

        public MainWindow()
        {
            InitializeComponent();

            //this.Activated += OnWindowActivated;

            try
            {

                Title = "Taskbar Tray App"; // Set the title of the window
                                            // Icon = new BitmapImage(new Uri("ms-appx:///Assets/Icons/app_icon.ico")) // Set the icon if needed

                _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();


                Closed += (sender, args) =>
                {
                    // Save Size & Position
                    //
                    var position = this.AppWindow.Position;
                    int x = position.X;
                    int y = position.Y;

                    WindowsData saveWinData = new WindowsData { Width = this.Width, Height = this.Height, X = position.X, Y = position.Y };

                    Log.Information($"Saving X [{saveWinData.X}]  Y [{saveWinData.Y}]  W [{saveWinData.Width}]  H [{saveWinData.Height}]");



                    _settingsService.SetWindowDataAsync(saveWinData);


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
                Log.Error($"❌ Error in MainWindow ctor: {ex}");
                throw;
            }


        }

        private async void OnWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                var winData = await _settingsService.GetWindowDataAsync();

                Log.Information($"Window loaded: {winData.Width}x{winData.Height} at ({winData.X},{winData.Y})");

                // Optionally set size/position
                this.MoveAndResize(winData.X, winData.Y, winData.Width, winData.Height);

                //this.Move((int)winData.X, (int)winData.Y);
                //this.Width = winData.Width;
                //this.Height = winData.Height;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Error restoring window position: {ex}");
                throw;
            }
        }

        private async void main_grid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // PersistenceId - WONT WORK if App is Unpackaged
                //
                // so we use T10's FileService which saves for Packaged or Unpackaged
                //
                var winData = await _settingsService.GetWindowDataAsync();

                if (winData == null)
                {
                    // Not saved so eithe rfirst run or data deleted, so window will use defaults

                    Log.Information($"winData NULL");
                }
                else
                {
                    // Apply  
                    Log.Information($"Window loaded: {winData.Width}x{winData.Height} at ({winData.X},{winData.Y})");
                    this.MoveAndResize(winData.X, winData.Y, winData.Width, winData.Height);
                }

                Content = new MainView();

                // Set Theme 
                await _settingsService.SetRequestedThemeAsync();
                await Task.CompletedTask;


            }

            catch (Exception ex)
            {
                Log.Error($"exception: {ex}");
                throw;
            }
        }

    }
}
