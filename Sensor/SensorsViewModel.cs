using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using TaskbarTray.stuff;
using CommunityToolkit.Mvvm.Input;


namespace TaskbarTray.Sensor
{
    public partial class SensorsViewModel : ObservableObject
    {
        private readonly ILogger<SensorsViewModel> _logr;
        private Computer _computer;
      
        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }

        [ObservableProperty]
        private bool isRelaunchButtonVisible;
        
        [ObservableProperty]
        private string tempCpu;

        public SensorsViewModel(ILogger<SensorsViewModel> logr)
        {
            _logr = logr;

            IsRelaunchButtonVisible = !ElevationHelper.IsRunningAsAdmin();

            if (ElevationHelper.IsRunningAsAdmin())
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true
                };
                _computer.Open();

                StartMonitoring();
            }

        }

        private void StartMonitoring()
        {
            Task.Run(async () =>
            {
                _logr.LogInformation($"Cpu Temp monitoring begun...");
                while (true)
                {
                    try
                    {
                        float? temp = GetCpuTemperature();

                        if (temp == null)
                        {
                            _logr.LogInformation($"temp is null");
                        }

                        if (temp.HasValue && TheDispatcher != null)
                        {
                            TheDispatcher.TryEnqueue(() =>
                            {
                                TempCpu = $"{temp.Value:F1} °C";
                                _logr.LogInformation($"Temp {temp.Value:F1} °C");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logr?.LogError(ex, "Error reading CPU temperature.");
                    }

                    await Task.Delay(2000); // Refresh every 2 seconds
                }
            });
        }

        private float? GetCpuTemperature()
        {
            float? highestTemp = null;

            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (var sub in hardware.SubHardware)
                    sub.Update();

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                    {
                        _logr?.LogInformation($"Sensor Found: {sensor.Name}, Value: {sensor.Value}");

                        if (highestTemp == null || sensor.Value > highestTemp)
                        {
                            highestTemp = sensor.Value;
                        }
                    }
                }
            }

            return highestTemp;
        }


        [RelayCommand]
        private void RelaunchAsAdmin()
        {
            ElevationHelper.RelaunchAsAdmin();
        }
    }
}
