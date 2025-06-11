using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using LibreHardwareMonitor.Hardware;

namespace TaskbarTray.Services;

public interface IHardwareMonitorService
{
    Task<IEnumerable<HardwareInfo>> GetHardwareInfoAsync();
    Task StartMonitoringAsync();
    Task StopMonitoringAsync();
    event EventHandler<HardwareInfo>? HardwareInfoUpdated;
}

public class HardwareInfo
{
    public string Name { get; set; } = string.Empty;
    public float Temperature { get; set; }
    public float FanSpeed { get; set; }
    public HardwareType Type { get; set; }
}

public class HardwareMonitorService : IHardwareMonitorService, IDisposable
{
    private readonly Computer _computer;
    private readonly ConcurrentDictionary<string, HardwareInfo> _hardwareInfo;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _isDisposed;

    public event EventHandler<HardwareInfo>? HardwareInfoUpdated;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true
        };

        _hardwareInfo = new ConcurrentDictionary<string, HardwareInfo>();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task StartMonitoringAsync()
    {
        _computer.Open();
        _computer.Accept(new UpdateVisitor());

        while (await _timer.WaitForNextTickAsync(_cancellationTokenSource.Token))
        {
            await UpdateHardwareInfoAsync();
        }
    }

    public async Task StopMonitoringAsync()
    {
        _cancellationTokenSource.Cancel();
        _computer.Close();
    }

    public async Task<IEnumerable<HardwareInfo>> GetHardwareInfoAsync()
    {
        return _hardwareInfo.Values;
    }

    private async Task UpdateHardwareInfoAsync()
    {
        _computer.Accept(new UpdateVisitor());

        foreach (var hardware in _computer.Hardware)
        {
            await ProcessHardwareAsync(hardware);
        }
    }

    private async Task ProcessHardwareAsync(IHardware hardware)
    {
        hardware.Update();

        var info = new HardwareInfo
        {
            Name = hardware.Name,
            Type = hardware.HardwareType
        };

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType == SensorType.Temperature)
            {
                info.Temperature = sensor.Value ?? 0;
            }
            else if (sensor.SensorType == SensorType.Fan)
            {
                info.FanSpeed = sensor.Value ?? 0;
            }
        }

        _hardwareInfo.AddOrUpdate(hardware.Identifier.ToString(), info, (_, _) => info);
        HardwareInfoUpdated?.Invoke(this, info);

        foreach (var subHardware in hardware.SubHardware)
        {
            await ProcessHardwareAsync(subHardware);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _timer.Dispose();
            _computer.Close();
        }

        _isDisposed = true;
    }
}

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
        {
            subHardware.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
} 