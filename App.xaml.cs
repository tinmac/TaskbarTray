using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SkiaSharp;
using System;
using System.Diagnostics;
using TaskbarTray.stuff;
using TaskbarTray.ViewModels;
using TaskbarTray.Views;
using Windows.Storage;
using WinUIEx;


namespace TaskbarTray;

public sealed partial class App : Application
{
    private readonly ILogger<App> _logr;

    //public static WindowEx? MainWindow { get; set; }

    public static WindowEx Main_Window { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;

    public static IServiceProvider ServiceProvider { get; private set; }


    //public static WeakReferenceMessenger Messenger { get; } = new WeakReferenceMessenger();

    public App()
    {
        InitializeComponent();

        ServiceProvider = ConfigureServices();
        Ioc.Default.ConfigureServices(ServiceProvider);
        _logr = ServiceProvider.GetRequiredService<ILogger<App>>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Main_Window = new MainWindow();

        Main_Window.Hide();

        #region Original MainWIndow - created in code behind

        //MainWindow = new WindowEx
        //{
        //    MinWidth = 400,
        //    MinHeight = 300,
        //    PersistenceId = win_name, // This is used to persist the window size and position across app restarts
        //    Content = new Frame
        //    {
        //        Content = new MainView(),
        //    },
        //};

        //MainWindow.Closed += (sender, args) =>
        //{
        //    //Messenger.Send(new Msg_CloseMainWin { CloseMainWin = true });
        //    WeakReferenceMessenger.Default.Send(new MyMessage { CloseMainWin = true });

        //    if (HandleClosedEvents)
        //    {
        //        args.Handled = true;
        //        MainWindow.Hide();
        //    }
        //};


        //MainWindow.Hide();// Hide by default at startup, as this is a tray app

        #endregion


        // Theme watcher
        //
        // Initial check
        //
        // We need to know the `System Theme` (not app theme) at startup so we can set the tray icon accordingly
        // Loaded System theme ie Taskbar & TitleBar (not the app theme)
        //
        bool isLight = WindowsThemeChangedDetector.IsSystemInLightMode();
        _logr.LogInformation($"Loaded TaskBar theme:  {(isLight ? "Light" : "Dark")}");

        // Start watching for changes
        WindowsThemeChangedDetector.SystemThemeChanged += isLightMode =>
        {
            _logr.LogInformation($"Changed TaskBar theme:  {(isLightMode ? "Light" : "Dark")}");
          
            // Update tray icons, Rx'd in TrayIconVM 
            //
            WeakReferenceMessenger.Default.Send(new MyMessage { ThemeChanged_Light = isLightMode });
        };

        WindowsThemeChangedDetector.StartMonitoring();
    }

    private IServiceProvider ConfigureServices()
    {
        // Register your services, viewmodels and pages here
        var services = new ServiceCollection();

        services.AddTransient<TrayIconVM>(); // ViewModel for the tray icon


        #region SERILOG

        // Serilog   : Verbose  Debug  Information  Warning  Error  Fatal
        // Microsoft : Trace    Debug  Information  Warning  Error  Critical

        //LogEventLevel level = LogEventLevel.Debug;
        var tmpl_1 = "[{Timestamp:dd/MM  HH:mm:ss.fff} {Level:u3}{SourceContext} {AppId}]  {Message:lj}{NewLine}{Exception}";
        var tmpl_2 = "[{Timestamp:HH:mm:ss} {Level:u3}{SourceContext}]  {Message:lj}{NewLine}";
        var tmpl_3 = "[{Timestamp:HH:mm:ss} {Level:u3}{SourceContext} {AppId}]  {Message:lj}{NewLine}{Exception}";
        var tmpl_4 = "[{Timestamp:HH:mm:ss} {Level:u3}{SourceContext} {AppId}]  {Message:lj}{NewLine}";

        var LogPath = @"C:\PowerSwitcher_logs\power_.log";
        var lgr = new LoggerConfiguration()
            .MinimumLevel.Debug() // <<<<<<<--------------   MINIMUM LEVEL
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new SimpleClassEnricher()) // shortens the SourceContext ie: Agy.Wpf.Services.Duende to Duende
            .WriteTo.File(
                LogPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5_000_000,
                outputTemplate: tmpl_1,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: System.TimeSpan.FromSeconds(1))
           .WriteTo.Debug(outputTemplate: tmpl_1)// was tmpl_3
           .CreateLogger();

        #endregion


        // Now you're set to inject ILogger<TService> into any constructor you need.
        services.AddLogging(loggingBuilder =>
            loggingBuilder
            .ClearProviders()
            .AddSerilog(lgr, dispose: true));

        // Static classes
        // so we can use Log.LogInformation("bla"); in static classes (or any where)
        Log.Logger = lgr; 
       
        var svcs = services.BuildServiceProvider();

        return svcs;

    }

    private void ConvertSvgToIco()
    {
        // Create ico files from FA svg files
        //
        // syntax: ImageHelper.ConvertSvgToIco("svg Path", "ico Path", 24, 24);

        //var DbPath = ApplicationData.Current.LocalFolder.Path;

        string appFolderPath = AppContext.BaseDirectory;
        _logr.LogInformation($"App folder path: {appFolderPath}");

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
