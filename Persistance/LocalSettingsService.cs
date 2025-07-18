using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace PowerSwitch.Persistance;

public class LocalSettingsService : ILocalSettingsService
{
    private const string _defaultApplicationDataFolder = "PowerSwitch/ApplicationData";
    private const string _defaultLocalSettingsFile = "LocalSettings.json";

    private readonly IFileService _fileService;
    private readonly LocalSettingsOptions _options;

    private readonly string _localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _applicationDataFolder;
    private readonly string _localsettingsFile;

    // Expose settings folder and file name
    public string SettingsFolderPath => _applicationDataFolder;
    public string SettingsFileName => _localsettingsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;

    public LocalSettingsService(IFileService fileService, IOptions<LocalSettingsOptions> options)
    {
        _fileService = fileService;
        _options = options.Value;

        _applicationDataFolder = Path.Combine(_localApplicationData, _options.ApplicationDataFolder ?? _defaultApplicationDataFolder);
        _localsettingsFile = _options.LocalSettingsFile ?? _defaultLocalSettingsFile;

        _settings = new Dictionary<string, object>();

        // Log the settings file path at startup
        var settingsPath = Path.Combine(_applicationDataFolder, _localsettingsFile);
        Log.Information($"Settings dir: \n{_applicationDataFolder}");
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() => _fileService.Read<IDictionary<string, object>>(_applicationDataFolder, _localsettingsFile)) ?? new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<T?> ReadSettingAsync<T>(string key)
    {
        try
        {
            if (RuntimeHelper.IsMSIX)
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
                {
                    return await Json.ToObjectAsync<T>((string)obj);
                }
            }
            else
            {
                await InitializeAsync();

                if (_settings != null && _settings.TryGetValue(key, out var obj))
                {
                    var strObj = (string)obj;

                    var ret = await Json.ToObjectAsync<T>(strObj);

                    return ret;
                }
                else
                {
                    Log.Warning($"Failed to get Key {key} from LocalSettingsService. Settings: {Json.StringifyAsync(_settings)}");

                    return default;
                }
            }

            return default;
        }
        catch (Exception ex)
        {
            Log.Error($"exception: {ex}");
            throw;
        }

    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        if (RuntimeHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value);
        }
        else
        {
            await InitializeAsync();

            _settings[key] = await Json.StringifyAsync(value);

            await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
        }
    }
}
