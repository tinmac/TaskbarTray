using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;
using TaskbarTray.Persistance;
using TaskbarTray.Services;
using TaskbarTray.stuff;
using Windows.ApplicationModel;

namespace TaskbarTray.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    [ObservableProperty] private bool serviceIsRunning;
    private const string ServiceName = "SensorService_Labs";

    private readonly ISettingsService _themeSelectorService;

    [ObservableProperty]
    private string serviceStatusText = "Checking sensor service...";

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    public ICommand SwitchThemeCommand { get; }

    public SettingsViewModel(ISettingsService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });

        UpdateServiceStatus();
    }

    [RelayCommand]
    private void StartSensorService()
    {
        using var sc = new ServiceController(ServiceName);
        if (sc.Status != ServiceControllerStatus.Running)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
        }
        ServiceIsRunning = true;
        ServiceStatusText = "Sensor service is running.";
    }

    [RelayCommand]
    private void StopSensorService()
    {
        using var sc = new ServiceController(ServiceName);
        if (sc.Status == ServiceControllerStatus.Running)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
        }
        ServiceIsRunning = false;
        ServiceStatusText = "Sensor service is stopped.";
    }

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;
            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private async void UpdateServiceStatus()
    {
        const int maxAttempts = 5;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                ServiceIsRunning = sc.Status == ServiceControllerStatus.Running;
                ServiceStatusText = ServiceIsRunning ? "Sensor service is running." : "Sensor service is stopped.";
                return;
            }
            catch
            {
                await Task.Delay(1000); // wait and retry
            }
        }

        ServiceIsRunning = false;
        ServiceStatusText = "Sensor service not installed.";
    }
}