//using Microsoft.UI.Xaml;
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
}
