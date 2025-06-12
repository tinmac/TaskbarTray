using System.Threading.Tasks;

namespace TaskbarTray.Persistance;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);
}
