using System;
using System.Diagnostics;
using System.Linq;
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
using Common.Models;
using LibreHardwareMonitor.Hardware;
using System.Collections.ObjectModel;
using System.IO.Pipes;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;

namespace PowerSwitch.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly ILogger<SettingsViewModel> _logr;
    [ObservableProperty] private bool serviceIsRunning;
    private const string ServiceName = "PowerSwitchService";
    private readonly ISettingsService _settingsService;
    private const string TemperatureUnitKey = "TemperatureUnit";
    [ObservableProperty] private string serviceStatusText = "Checking sensor service...";
    [ObservableProperty] private ElementTheme _elementTheme;
    [ObservableProperty] private string _versionDescription;
    private TemperatureUnit temperatureUnit = TemperatureUnit.Celsius;
    public TemperatureUnit TemperatureUnit
    {
        get => temperatureUnit;
        set
        {
            _logr.LogInformation("Unit Fired...");
            if (SetProperty(ref temperatureUnit, value))
            {
                _ = _settingsService.SaveSettingAsync(TemperatureUnitKey, temperatureUnit);
                _logr.LogInformation("Persisted Unit {TemperatureUnit}", temperatureUnit);
            }
        }
    }
    public ICommand SwitchThemeCommand { get; }
    private static bool IsPackaged;
    private const string AppName = "LLabsPowerSwitch";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupTaskId = "StartMyApp";
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
  
    [ObservableProperty]
    private bool includePowerSaver = true;

    private async Task SaveAllPlanTogglesAsync()
    {
        await _settingsService.SaveSettingAsync(IncludePowerSaverKey, IncludePowerSaver);
        await _settingsService.SaveSettingAsync(IncludeBalancedKey, IncludeBalanced);
        await _settingsService.SaveSettingAsync(IncludeHighPerformanceKey, IncludeHighPerformance);
    }

    partial void OnIncludePowerSaverChanged(bool value)
    {
        _logr.LogInformation($"IncludePowerSaver changed to: {value}");
        if (_suppressPlanFallback) return;
        if (!value && !IncludeBalanced && !IncludeHighPerformance)
        {
            PowerPlanSelectionError = "At least one power plan must be selected.";
            _isRevertingPlanSelection = true;
            // Revert to first available plan
            if (IncludeHighPerformance)
                IncludeHighPerformance = true;
            else if (IncludeBalanced)
                IncludeBalanced = true;
            else
                IncludePowerSaver = true;
            _isRevertingPlanSelection = false;
            return;
        }
        if (!_isRevertingPlanSelection)
            PowerPlanSelectionError = string.Empty;
        _ = SaveAllPlanTogglesAsync();
    }
    [ObservableProperty]
    private bool includeBalanced = true;
    partial void OnIncludeBalancedChanged(bool value)
    {
        _logr.LogInformation($"IncludeBalanced changed to: {value}");
        if (_suppressPlanFallback) return;
        if (!value && !IncludePowerSaver && !IncludeHighPerformance)
        {
            PowerPlanSelectionError = "At least one power plan must be selected.";
            _isRevertingPlanSelection = true;
            if (IncludeHighPerformance)
                IncludeHighPerformance = true;
            else if (IncludePowerSaver)
                IncludePowerSaver = true;
            else
                IncludeBalanced = true;
            _isRevertingPlanSelection = false;
            return;
        }
        if (!_isRevertingPlanSelection)
            PowerPlanSelectionError = string.Empty;
        _ = SaveAllPlanTogglesAsync();
    }
    [ObservableProperty]
    private bool includeHighPerformance = false;
    partial void OnIncludeHighPerformanceChanged(bool value)
    {
        _logr.LogInformation($"IncludeHighPerformance changed to: {value}");
        if (_suppressPlanFallback) return;
        if (!value && !IncludePowerSaver && !IncludeBalanced)
        {
            PowerPlanSelectionError = "At least one power plan must be selected.";
            _isRevertingPlanSelection = true;
            if (IncludeBalanced)
                IncludeBalanced = true;
            else if (IncludePowerSaver)
                IncludePowerSaver = true;
            else
                IncludeHighPerformance = true;
            _isRevertingPlanSelection = false;
            return;
        }
        if (!_isRevertingPlanSelection)
            PowerPlanSelectionError = string.Empty;
        _ = SaveAllPlanTogglesAsync();
    }

    private string _powerPlanSelectionError;
    public string PowerPlanSelectionError
    {
        get => _powerPlanSelectionError;
        set
        {
            _logr?.LogInformation($"PowerPlanSelectionError set to: {value}");
            SetProperty(ref _powerPlanSelectionError, value);
        }
    }

    private const string IncludePowerSaverKey = "IncludePowerSaver";
    private const string IncludeBalancedKey = "IncludeBalanced";
    private const string IncludeHighPerformanceKey = "IncludeHighPerformance";

    private bool _isRevertingPlanSelection = false;
    private bool _suppressPlanFallback = false;

    public async Task LoadPlanToggleSettingsAsync()
    {
        _suppressPlanFallback = true;
        var powerSaverSetting = await _settingsService.GetSettingAsync<bool?>(IncludePowerSaverKey);
        var balancedSetting = await _settingsService.GetSettingAsync<bool?>(IncludeBalancedKey);
        var highPerfSetting = await _settingsService.GetSettingAsync<bool?>(IncludeHighPerformanceKey);
        IncludePowerSaver = powerSaverSetting ?? false;
        IncludeBalanced = balancedSetting ?? false;
        IncludeHighPerformance = highPerfSetting ?? false;
        // Ensure at least one plan is included
        if (!IncludePowerSaver && !IncludeBalanced && !IncludeHighPerformance)
        {
            // Prefer HighPerformance, then Balanced, then PowerSaver
            IncludeHighPerformance = true;
        }
        _suppressPlanFallback = false;
    }

    [ObservableProperty]
    private Common.Models.ServiceStatus _serviceStatus = Common.Models.ServiceStatus.Unknown;

    [ObservableProperty]
    private Microsoft.UI.Xaml.Media.Brush _serviceStatusBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

    partial void OnServiceStatusChanged(Common.Models.ServiceStatus value)
    {
        ServiceStatusBrush = value == Common.Models.ServiceStatus.Running
            ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen)
            : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
    }

    public SettingsViewModel(ISettingsService settingsService, ILogger<SettingsViewModel> logr)
    {
        _logr = logr;
        _settingsService = settingsService;
        _elementTheme = _settingsService.Theme;
        _versionDescription = GetVersionDescription();
        _ = LoadTemperatureUnitAsync();
        //PowerPlanSelectionError = "Test error: If you see this, binding works.";
        _logr.LogInformation("SettingsViewModel initialized. Theme: {Theme}, Version: {Version}", _elementTheme, _versionDescription);
        try
        {
            IsPackaged = RuntimeHelper.IsMSIX;
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
                    await settingsService.SetThemeAsync(param);
                    _logr.LogInformation("Theme changed to {Theme}", param);
                }
            });
        _ = LoadStartupStateAsync();
        UpdateServiceStatus();
        _ = PollServiceStatusAsync();
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
                await Task.Delay(1000);
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
                        task.Disable();
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
        var persisted = await _settingsService.GetSettingAsync<TemperatureUnit>(TemperatureUnitKey);
        TemperatureUnit = persisted;
        _logr.LogInformation("Loaded unit: {TemperatureUnit}", TemperatureUnit);
    }
    public TemperatureUnit[] TemperatureUnits => (TemperatureUnit[])Enum.GetValues(typeof(TemperatureUnit));

    public string GaugeIconUri
    {
        get
        {
            // Use the system theme to select the correct icon
            var theme = ElementTheme;
            if (theme == ElementTheme.Dark)
                return "ms-appx:///Assets/ico/gauge-wh.ico";
            else
                return "ms-appx:///Assets/ico/gauge.ico";
        }
    }

    private async Task PollServiceStatusAsync()
    {
        while (true)
        {
            try
            {
                var status = GetServiceStatus();
                if (ServiceStatus != status)
                {
                    ServiceStatus = status;
                }
            }
            catch { }
            await Task.Delay(3000);
        }
    }

    private Common.Models.ServiceStatus GetServiceStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            return sc.Status switch
            {
                ServiceControllerStatus.Stopped => Common.Models.ServiceStatus.Stopped,
                ServiceControllerStatus.StartPending => Common.Models.ServiceStatus.StartPending,
                ServiceControllerStatus.StopPending => Common.Models.ServiceStatus.StopPending,
                ServiceControllerStatus.Running => Common.Models.ServiceStatus.Running,
                ServiceControllerStatus.ContinuePending => Common.Models.ServiceStatus.ContinuePending,
                ServiceControllerStatus.PausePending => Common.Models.ServiceStatus.PausePending,
                ServiceControllerStatus.Paused => Common.Models.ServiceStatus.Paused,
                _ => Common.Models.ServiceStatus.Unknown
            };
        }
        catch
        {
            return Common.Models.ServiceStatus.Unknown;
        }
    }
}