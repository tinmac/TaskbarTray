﻿using System.Threading.Tasks;

namespace PowerSwitch.Persistance;

public interface ILocalSettingsService
{
    Task<T?> ReadSettingAsync<T>(string key);

    Task SaveSettingAsync<T>(string key, T value);
}
