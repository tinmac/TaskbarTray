using System;
using System.Diagnostics;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using PowerSwitch.Persistance;
using PowerSwitch.Services;
using PowerSwitch.stuff;
using Windows.ApplicationModel;

namespace PowerSwitch.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly ILogger<SettingsViewModel> _logr;

    [ObservableProperty] private bool serviceIsRunning;
    private const string ServiceName = "SensorService_Labs";

    private readonly ISettingsService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;
    private const string TemperatureUnitKey = "TemperatureUnit";

    [ObservableProperty]
    private string serviceStatusText = "Checking sensor service...";

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    private TemperatureUnit temperatureUnit = TemperatureUnit.Celsius;
    public TemperatureUnit TemperatureUnit
    {
        get => temperatureUnit;
        set
        {
            if (SetProperty(ref temperatureUnit, value))
            {
                // Persist the value
                _ = _localSettingsService.SaveSettingAsync(TemperatureUnitKey, temperatureUnit);

                _logr.LogInformation("Persisted Unit {TemperatureUnit}", temperatureUnit);
            }
        }
    }

    public ICommand SwitchThemeCommand { get; }


    // Detect if app is MSIX packaged
    private static bool IsPackaged;



    // For start/stop when Windows starts
    private const string AppName = "LLabsPowerSwitch"; // For registry value
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupTaskId = "StartMyApp"; // For MSIX manifest

    private bool _startWithWindows;
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            _logr.LogInformation("StartWithWindows toggled...");
            if (SetProperty(ref _startWithWindows, value))
            {
                _ = ApplyStartupSettingAsync(value);
            }
        }
    }

    public SettingsViewModel(ISettingsService themeSelectorService, ILogger<SettingsViewModel> logr, ILocalSettingsService localSettingsService)
    {
        _logr = logr;
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        // Load persisted temperature unit
        _ = LoadTemperatureUnitAsync();


        _logr.LogInformation("SettingsViewModel initialized. Theme: {Theme}, Version: {Version}", _elementTheme, _versionDescription);

        try
        {
            IsPackaged = Package.Current?.Id?.Name != null;
        }
        catch (Exception ex)
        {
            IsPackaged = false;
            _logr.LogWarning(ex, "Failed to detect MSIX packaging. Assuming not packaged.");
        }
        _logr.LogInformation($"IsPackaged: {IsPackaged}");

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    _logr.LogInformation("SwitchThemeCommand invoked. Changing theme from {OldTheme} to {NewTheme}", ElementTheme, param);
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                    _logr.LogInformation("Theme changed to {Theme}", param);
                }
            });

        _ = LoadStartupStateAsync();

        UpdateServiceStatus();
    }

    [RelayCommand]
    private void StartSensorService()
    {
        _logr.LogInformation("Attempting to start service: {ServiceName}", ServiceName);
        using var sc = new ServiceController(ServiceName);
        if (sc.Status != ServiceControllerStatus.Running)
        {
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            _logr.LogInformation("Service {ServiceName} started.", ServiceName);
        }
        else
        {
            _logr.LogInformation("Service {ServiceName} was already running.", ServiceName);
        }
        ServiceIsRunning = true;
        ServiceStatusText = "Running";
    }

    [RelayCommand]
    private void StopSensorService()
    {
        _logr.LogInformation("Attempting to stop service: {ServiceName}", ServiceName);
        using var sc = new ServiceController(ServiceName);
        if (sc.Status == ServiceControllerStatus.Running)
        {
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
            _logr.LogInformation("Service {ServiceName} stopped.", ServiceName);
        }
        else
        {
            _logr.LogInformation("Service {ServiceName} was already stopped.", ServiceName);
        }
        ServiceIsRunning = false;
        ServiceStatusText = "Stopped";
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
                ServiceStatusText = ServiceIsRunning ? "Running" : "Stopped";
                _logr.LogInformation("Checked service status: {Status}", ServiceStatusText);
                return;
            }
            catch (Exception ex)
            {
                _logr.LogWarning(ex, "Failed to check service status on attempt {Attempt}", attempt + 1);
                await Task.Delay(1000); // wait and retry
            }
        }

        ServiceIsRunning = false;
        ServiceStatusText = "Not installed.";
        _logr.LogError("Service {ServiceName} not installed or could not be queried after {MaxAttempts} attempts.", ServiceName, maxAttempts);
    }



    private async Task LoadStartupStateAsync()
    {
        if (IsPackaged)
        {
            _logr.LogInformation("IsPackaged: true, using StartupTask API.");

            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);
                StartWithWindows = task.State == StartupTaskState.Enabled;
                _logr.LogInformation("Startup task state: {State}", task.State);
            }
            catch (Exception ex)
            {
                StartWithWindows = false;
                _logr.LogError(ex, "Failed to retrieve startup task state.");
            }
        }
        else
        {
            _logr.LogInformation("IsPackaged: false, using registry method.");

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                var currentValue = key?.GetValue(AppName) as string;
                StartWithWindows = string.Equals(currentValue, exePath, StringComparison.OrdinalIgnoreCase);
                _logr.LogInformation("Registry startup value: {Value}, StartWithWindows: {StartWithWindows}", currentValue, StartWithWindows);
            }
            catch (Exception ex)
            {
                StartWithWindows = false;
                _logr.LogError(ex, "Failed to check registry for startup value.");
            }
        }
    }

    private async Task ApplyStartupSettingAsync(bool enable)
    {
        if (IsPackaged)
        {
            try
            {
                var task = await StartupTask.GetAsync(StartupTaskId);

                if (enable)
                {
                    if (task.State == StartupTaskState.Disabled)
                    {
                        var result = await task.RequestEnableAsync();
                        StartWithWindows = result == StartupTaskState.Enabled;
                        _logr.LogInformation("Requested enable startup task. Result: {Result}", result);
                    }
                }
                else
                {
                    if (task.State == StartupTaskState.Enabled ||
                        task.State == StartupTaskState.DisabledByUser)
                    {
                        task.Disable(); // No prompt, effective immediately
                        StartWithWindows = false;
                        _logr.LogInformation("Startup task disabled.");
                    }
                }

            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Failed to set startup task state for MSIX packaged app (StartupTask API)");
            }
        }
        else
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                if (enable)
                {
                    key.SetValue(AppName, exePath);
                    _logr.LogInformation("Set registry value for startup: {ExePath}", exePath);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    _logr.LogInformation("Removed registry value for startup.");
                }

                StartWithWindows = enable;

                await LoadStartupStateAsync();

            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Failed to set startup task state for non-MSIX packaged app (Registry method)");
            }
        }
    }

    private async Task LoadTemperatureUnitAsync()
    {
        var persisted = await _localSettingsService.ReadSettingAsync<TemperatureUnit>(TemperatureUnitKey);
        TemperatureUnit = persisted;

        _logr.LogInformation("Loaded unit: {TemperatureUnit}", TemperatureUnit);

    }
}