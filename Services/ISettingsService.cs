using System.Threading.Tasks;

namespace PowerSwitch.Services;

public interface ISettingsService
{
    // Theme
    //
    ElementTheme Theme
    {
        get;
    }

    Task GetThemeAsync();

    Task SetThemeAsync(ElementTheme theme);

    Task SetRequestedThemeAsync();


    // Windows Data - Size & Position 
    //
    Task<WindowsData> GetWindowDataAsync();

    Task SetWindowDataAsync(WindowsData windowsData);

    // Generic methods for any property
    Task<T?> GetSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);
}
