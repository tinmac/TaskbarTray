using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
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
    private readonly FileService _fileService;
    private readonly string _settingsFolder;
    private readonly string _settingsFile;

    public ElementTheme Theme { get; set; } = ElementTheme.Default;
    public WindowsData WindowsData { get; set; }

    public SettingsService(FileService fileService)
    {
        _fileService = fileService;
        _settingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PowerSwitch/ApplicationData");
        _settingsFile = "LocalSettings.json";
    }

    private bool IsMSIX => RuntimeHelper.IsMSIX;

    private async Task<Dictionary<string, object>> LoadSettingsAsync()
    {
        return await Task.Run(() => _fileService.Read<Dictionary<string, object>>(_settingsFolder, _settingsFile)) ?? new();
    }

    private async Task SaveSettingsAsync(Dictionary<string, object> settings)
    {
        await Task.Run(() => _fileService.Save(_settingsFolder, _settingsFile, settings));
    }

    // Generic get
    public async Task<T?> GetSettingAsync<T>(string key)
    {
        object value = null;
        if (IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out value);
        }
        else
        {
            var dict = await LoadSettingsAsync();
            dict.TryGetValue(key, out value);
        }

        if (value == null)
            return default;

        var t = typeof(T);
        var underlyingEnum = Nullable.GetUnderlyingType(t);
        if (t.IsEnum || (underlyingEnum != null && underlyingEnum.IsEnum))
        {
            var enumType = underlyingEnum ?? t;
            if (value is long || value is int)
                return (T)Enum.ToObject(enumType, value);
            return (T)Enum.Parse(enumType, value.ToString());
        }
        if (t == typeof(string))
            return (T)(object)value.ToString();
        if (t == typeof(bool) || t == typeof(int) || t == typeof(double) || t == typeof(float) || t == typeof(long))
            return (T)Convert.ChangeType(value, t);

        // Handle JObject to T conversion for complex types
        if (value is Newtonsoft.Json.Linq.JObject jObj)
            return jObj.ToObject<T>();

        // Fallback: try to convert using JSON
        if (value is string s)
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s);

        return (T)value;
    }

    // Generic save
    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }
        else
        {
            var dict = await LoadSettingsAsync();
            dict[key] = value;
            await SaveSettingsAsync(dict);
        }
    }

    // Theme
    public async Task GetThemeAsync()
    {
        Theme = await GetSettingAsync<ElementTheme?>(BgThemeKey) ?? ElementTheme.Default;
    }
    public async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;
        await SetRequestedThemeAsync();
        await SaveSettingAsync(BgThemeKey, theme);
    }
    public async Task SetRequestedThemeAsync()
    {
        if (App.Main_Window.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;
        }
        await Task.CompletedTask;
    }

    // Window Size & Position
    public async Task<WindowsData> GetWindowDataAsync()
    {
        return await GetSettingAsync<WindowsData>(WindowSizePosKey);
    }
    public async Task SetWindowDataAsync(WindowsData windowsData)
    {
        await SaveSettingAsync(WindowSizePosKey, windowsData);
    }

    // TemperatureUnit methods
    public async Task<TemperatureUnit> GetTemperatureUnitAsync()
    {
        return await GetSettingAsync<TemperatureUnit>(TemperatureUnitKey);
    }
    public async Task SetTemperatureUnitAsync(TemperatureUnit unit)
    {
        await SaveSettingAsync(TemperatureUnitKey, unit);
    }

    private Dictionary<string, object> GetDefaultSettings()
    {
        return new Dictionary<string, object>
        {
            { "IncludePowerSaver", true },
            { "IncludeBalanced", true },
            { "IncludeHighPerformance", false },
            { TemperatureUnitKey, TemperatureUnit.Celsius },
            { BgThemeKey, ElementTheme.Default },
            { WindowSizePosKey, new WindowsData { Width = 700, Height = 888, X = 65, Y = 25 } }
        };
    }

    private async Task EnsureDefaultsAsync()
    {
        var defaults = GetDefaultSettings();
        if (IsMSIX)
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            bool changed = false;
            foreach (var kvp in defaults)
            {
                if (!values.ContainsKey(kvp.Key))
                {
                    values[kvp.Key] = kvp.Value;
                    changed = true;
                }
            }
            // No need to save, MSIX settings are live
        }
        else
        {
            var settings = await LoadSettingsAsync();
            bool changed = false;
            foreach (var kvp in defaults)
            {
                if (!settings.ContainsKey(kvp.Key))
                {
                    settings[kvp.Key] = kvp.Value;
                    changed = true;
                }
            }
            if (changed)
                await SaveSettingsAsync(settings);
        }
    }

    public async Task InitAsync()
    {
        await EnsureDefaultsAsync();
    }
}
