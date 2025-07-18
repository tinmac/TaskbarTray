using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using PowerSwitch.Persistance;
using PowerSwitch.stuff;

namespace PowerSwitch.Services;

public class WindowsData
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

public class SettingsService : ISettingsService
{
    private const string BgThemeKey = "AppBackgroundRequestedTheme";
    private const string WindowSizePosKey = "AppWindowData";
    private const string TemperatureUnitKey = "TemperatureUnit";

    public ElementTheme Theme { get; set; } = ElementTheme.Default;
    public WindowsData WindowsData { get; set; }

    private readonly ILocalSettingsService _localSettingsService;

    public SettingsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task GetThemeAsync()
    {
        Theme = await LoadThemeFromSettingsAsync();
        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;
        await SetRequestedThemeAsync();
        await SaveThemeInSettingsAsync(Theme);
    }

    public async Task SetRequestedThemeAsync()
    {
        if (App.Main_Window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;
        }
        await Task.CompletedTask;
    }

    private async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        var themeName = await _localSettingsService.ReadSettingAsync<string>(BgThemeKey);
        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }
        return ElementTheme.Default;
    }

    private async Task SaveThemeInSettingsAsync(ElementTheme theme)
    {
        await _localSettingsService.SaveSettingAsync(BgThemeKey, theme.ToString());
    }

    // Window Size & Position
    public async Task<WindowsData> GetWindowDataAsync()
    {
        var windowsData = await _localSettingsService.ReadSettingAsync<WindowsData>(WindowSizePosKey);
        return windowsData;
    }

    public async Task SetWindowDataAsync(WindowsData windowsData)
    {
        await _localSettingsService.SaveSettingAsync(WindowSizePosKey, windowsData);
    }

    // TemperatureUnit methods
    public async Task<TemperatureUnit> GetTemperatureUnitAsync()
    {
        var persisted = await _localSettingsService.ReadSettingAsync<TemperatureUnit>(TemperatureUnitKey);
        return persisted;
    }

    public async Task SetTemperatureUnitAsync(TemperatureUnit unit)
    {
        await _localSettingsService.SaveSettingAsync(TemperatureUnitKey, unit);
    }

    // Generic methods for any property
    public async Task<T?> GetSettingAsync<T>(string key)
    {
        var value = await _localSettingsService.ReadSettingAsync<string>(key);
        if (typeof(T).IsEnum && value != null)
            return (T)Enum.Parse(typeof(T), value);
        return await _localSettingsService.ReadSettingAsync<T>(key);
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (typeof(T).IsEnum)
            await _localSettingsService.SaveSettingAsync(key, value.ToString());
        else
            await _localSettingsService.SaveSettingAsync(key, value);
    }
}
