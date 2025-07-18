using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using PowerSwitch.Persistance;

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

    // Generic methods for any property
    public async Task<T?> GetSettingAsync<T>(string key)
    {
        return await _localSettingsService.ReadSettingAsync<T>(key);
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        await _localSettingsService.SaveSettingAsync(key, value);
    }
}
