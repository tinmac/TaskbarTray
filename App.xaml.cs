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
using System.IO;


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

        this.UnhandledException += (sender, e) =>
        {
            Log.Fatal($"Unhandled exception: {e.Exception}");

            Debug.WriteLine($"Unhandled exception: {e.Exception}");

            try
            {
                var logger = ServiceProvider?.GetService<ILogger<App>>();
                logger?.LogError(e.Exception, "Unhandled exception in App");
            }
            catch { }
        };

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
        var LogPath = @"C:\Programdata\PowerSwitch\.log";
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
        services.AddSingleton<SettingsViewModel, SettingsViewModel>((sp) =>
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

            // Ensure log directory exists
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "PowerSwitch");
            
            Directory.CreateDirectory(logDir);


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


            // Create Icons!!
            //
           // ConvertSvgToIco();
        }
        catch (Exception ex)
        {
            _logr.LogError(ex, $"Exception in App.xaml.cs -> OnLaunched: {ex}");
            throw;
        }

    }


    private void ConvertSvgToIco()
    {
        try
        {
            // Create ico files from FA svg files
            //
            // syntax: ImageHelper.ConvertSvgToIco("svg Path", "ico Path", 24, 24);

            //var DbPath = ApplicationData.Current.LocalFolder.Path;

            string appFolderPath = AppContext.BaseDirectory;
            _logr.LogInformation($"App folder path: {appFolderPath}");


            // File Paths
            var source_svg = "C:\\Users\\mmcca\\OneDrive\\Pictures\\Power Switch App\\svg"; // svgs are vectors 
            var blue_1 = new SKColor(173, 216, 230, 255); // Light blue with full opacity
            var blue_2 = new SKColor(105, 195, 230, 255); // Darker blue with full opacity
            var darkGrey = new SKColor(40, 40, 40, 255); //

            // Creating the App Png
            //
            bool CreateAppPng = true;
            if (CreateAppPng)
            {
                var dest_app_png = "C:\\Users\\mmcca\\OneDrive\\Pictures\\Power Switch App\\app";
           
                _logr.LogInformation($"Creating Windows AppIcon.png at {dest_app_png}");
              
                // Foreground Light blue with full opacity: SKColor(173, 216, 230, 255)
                ImageHelper.ConvertSvgToPng(
                    $"{source_svg}/gauge-high.svg",
                    $"{dest_app_png}/AppIcon.png",
                    128,
                    128, 
                    blue_2
                );

                // Foreground black
                ImageHelper.ConvertSvgToPng(
                    $"{source_svg}/gauge-high.svg",
                    $"{dest_app_png}/AppIconDark.png",
                    256,
                    256,
                    darkGrey
                );
            }


            // Creating the App Icon
            //
            bool CreateAppIcon = true;
            if (CreateAppIcon)
            {
                var dest_app_ico = "C:\\Users\\mmcca\\OneDrive\\Pictures\\Power Switch App\\app";
           
                _logr.LogInformation($"Creating Windows AppIcon.ico at {dest_app_ico}");
                
                ImageHelper.ConvertSvgToMultiSizeIco(
                    $"{source_svg}/gauge-high.svg", 
                    $"{dest_app_ico}/AppIcon.ico",
                    blue_2
                );
            }


            // Creatig the Taskbar Icons
            //
            bool CreateTaskBarIcons = false;
            var dest_task_bar = "C:\\Users\\mmcca\\OneDrive\\Pictures\\Power Switch App\\task-bar";
            if (CreateTaskBarIcons)
            {
                _logr.LogInformation($"Creating Taskbar icons at {dest_task_bar}");

                // White Foreground
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge.svg", $"{dest_task_bar}/gauge-wh.ico", 24, 24, SKColors.White);
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge-min.svg", $"{dest_task_bar}/gauge-min-wh.ico", 24, 24, SKColors.White);
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge-max.svg", $"{dest_task_bar}/gauge-max-wh.ico", 24, 24, SKColors.White);

                // Black Foreground
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge.svg", $"{dest_task_bar}/gauge.ico", 24, 24);
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge-min.svg", $"{dest_task_bar}/gauge-min.ico", 24, 24);
                ImageHelper.ConvertSvgToIco($"{source_svg}/gauge-max.svg", $"{dest_task_bar}/gauge-max.ico", 24, 24);
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Exception in App.xaml.cs -> ConvertSvgToIco: {ex}");
            throw;
        }

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
