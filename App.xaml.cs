using CommunityToolkit.Mvvm.Messaging;
using SkiaSharp;
using System;
using System.Diagnostics;
using TaskbarTray.stuff;
using TaskbarTray.Views;
using Windows.Storage;
using WinUIEx;


namespace TaskbarTray;

public sealed partial class App : Application
{

    public static Window? MainWindow { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;


    //public static WeakReferenceMessenger Messenger { get; } = new WeakReferenceMessenger();

    public App()
    {
        InitializeComponent();
    }


    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        //ConvertSvgToIco();

        MainWindow = new WindowEx
        {
            Width = 400,
            Height = 300,
            Content = new Frame
            {
                Content = new MainView(),
            },
        };

        MainWindow.Closed += (sender, args) =>
        {
            //Messenger.Send(new Msg_CloseMainWin { CloseMainWin = true });
            WeakReferenceMessenger.Default.Send(new MyMessage { CloseMainWin = true });

            if (HandleClosedEvents)
            {
                args.Handled = true;
                MainWindow.Hide();
            }
        };


        MainWindow.Hide();// Hide by default at startup, as this is a tray app

        // Theme watcher
        //
        // Initial check
        bool isLight = WindowsThemeChangedDetector.IsSystemInLightMode();
        Debug.WriteLine($"System (Windows Mode) is {(isLight ? "Light" : "Dark")}");

        // Start watching for changes
        WindowsThemeChangedDetector.SystemThemeChanged += isLightMode =>
        {
            Debug.WriteLine($"\nSystem theme changed to: {(isLightMode ? "Light" : "Dark")}");
          
            // Update tray icons, Rx'd in TrayIconVM 
            //
            WeakReferenceMessenger.Default.Send(new MyMessage { ThemeChanged_Light = isLightMode });
        };

        WindowsThemeChangedDetector.StartMonitoring();
    }

    private void ConvertSvgToIco()
    {
        // Create ico files from FA svg files
        //
        // syntax: ImageHelper.ConvertSvgToIco("svg Path", "ico Path", 24, 24);

        //var DbPath = ApplicationData.Current.LocalFolder.Path;

        string appFolderPath = AppContext.BaseDirectory;
        Debug.WriteLine($"appFolderPath {appFolderPath}");

        // White Foreground
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge.svg", "c:/assets/ico/gauge-wh.ico", 24, 24, SKColors.White);
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge-min.svg", "c:/assets/ico/gauge-min-wh.ico", 24, 24, SKColors.White);
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge-max.svg", "c:/assets/ico/gauge-max-wh.ico", 24, 24, SKColors.White);

        // Black Foreground
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge.svg", "c:/assets/ico/gauge.ico", 24, 24);
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge-min.svg", "c:/assets/ico/gauge-min.ico", 24, 24);
        ImageHelper.ConvertSvgToIco("c:/assets/svg/gauge-max.svg", "c:/assets/ico/gauge-max.ico", 24, 24);

    }


    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}
