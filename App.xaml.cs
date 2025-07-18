using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SkiaSharp;
using System;
using System.Diagnostics;
using PowerSwitch.stuff;
using PowerSwitch.ViewModels;
using PowerSwitch.Views;
using PowerSwitch.Services;
using Windows.Storage;
using WinUIEx;
using PowerSwitch.Persistance;
using System.Threading.Tasks;
using PowerSwitch.Sensor;
using PowerSwitch.SensorPipeService;


namespace PowerSwitch;

public sealed partial class App : Application
{
    private readonly ILogger<App> _logr;

    //public static WindowEx? MainWindow { get; set; }

    public static WindowEx Main_Window { get; set; }

    public static bool HandleClosedEvents { get; set; } = true;

    public static IServiceProvider ServiceProvider { get; private set; }


    //private IThemeSelectorService _themeSelectorService;


    //public static WeakReferenceMessenger Messenger { get; } = new WeakReferenceMessenger();

    public App()
    {
        InitializeComponent();

        ServiceProvider = ConfigureServices();
        Ioc.Default.ConfigureServices(ServiceProvider);
        _logr = ServiceProvider.GetRequiredService<ILogger<App>>();

        // No service for type 'Microsoft.Extensions.Logging.ILogger`1[PowerSwitch.App]' has been registered.'


    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Serilog setup (do not change)
        var tmpl_1 = "[{Timestamp:dd/MM  HH:mm:ss.fff} {Level:u3}{SourceContext} {AppId}]  {Message:lj}{NewLine}{Exception}";
        var LogPath = @"C:\PowerSwitcher_logs\power_.log";
        var lgr = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.With(new SimpleClassEnricher())
            .WriteTo.File(
                LogPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5_000_000,
                outputTemplate: tmpl_1,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: System.TimeSpan.FromSeconds(1))
            .WriteTo.Debug(outputTemplate: tmpl_1)
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
            loggingBuilder
                .ClearProviders()
                .AddSerilog(lgr, dispose: true));
        Log.Logger = lgr;

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        services.AddSingleton<IFileService, FileService>();
        services.AddTransient<TrayIconVM, TrayIconVM>((sp) =>
            new TrayIconVM(
                sp.GetRequiredService<ILogger<TrayIconVM>>(),
                sp.GetRequiredService<ISettingsService>()));
        services.AddSingleton<IHardwareMonitorService, HardwareMonitorService>();
        services.AddTransient<SettingsViewModel, SettingsViewModel>((sp) =>
            new SettingsViewModel(
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<ILogger<SettingsViewModel>>()));
        services.AddTransient<Settings>();
        services.AddTransient<SensorsViewModel>();
        services.AddTransient<Sensors>();
        services.AddTransient<SensorsPipeViewModel>();
        services.AddTransient<SensorsPipeView>();

        var svcs = services.BuildServiceProvider();
        return svcs;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var _settingsService = ServiceProvider.GetRequiredService<ISettingsService>();

            // Execute tasks before activation.
            // Get Theme
            await _settingsService.GetThemeAsync();


            // Pipe Service Worker Check
            await ServiceInstallerHelper.RunInstallScriptIfNeededAsync();



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

            // Initially set tray icons colour detected at startup 
            //
            WeakReferenceMessenger.Default.Send(new MyMessage { ThemeChanged_Light = isLight });


            // Start watching for changes
            WindowsThemeChangedDetector.SystemThemeChanged += isLightMode =>
            {
                _logr.LogInformation($"Changed TaskBar theme:  {(isLightMode ? "Light" : "Dark")}");

                // Update tray icons, Rx'd in TrayIconVM 
                //
                WeakReferenceMessenger.Default.Send(new MyMessage { ThemeChanged_Light = isLightMode });
            };

            WindowsThemeChangedDetector.StartMonitoring();

            // Set Theme 
            await _settingsService.SetRequestedThemeAsync();
            await Task.CompletedTask;



        }
        catch (Exception ex)
        {
            _logr.LogError(ex, $"Exception in App.xaml.cs -> OnLaunched: {ex}");
            throw;
        }

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

    //private async Task InitializeAsync()
    //{
    //    await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
    //    await Task.CompletedTask;
    //}

    //private async Task StartupAsync()
    //{
    //    await _themeSelectorService.SetRequestedThemeAsync();
    //    await Task.CompletedTask;
    //}


    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}
