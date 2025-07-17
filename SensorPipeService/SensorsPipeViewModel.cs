using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskbarTray.Models;

using LiveChartsCore.Kernel;
using TaskbarTray;
using System.Diagnostics;
using ExCSS;
using CommunityToolkit.Mvvm.Messaging;
using TaskbarTray.stuff;


public partial class SensorsPipeViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ISeries> temperatureSeries = new();
    [ObservableProperty] private string[] temperatureLabels = Array.Empty<string>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> xAxes = Enumerable.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> yAxes = Enumerable.Empty<ICartesianAxis>();

    [ObservableProperty] private ObservableCollection<ISeries> fanSeries = new();
    [ObservableProperty] private string[] fanLabels = Array.Empty<string>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> fanXAxes = Enumerable.Empty<ICartesianAxis>();
    [ObservableProperty] private IEnumerable<ICartesianAxis> fanYAxes = Enumerable.Empty<ICartesianAxis>();

    public DrawMarginFrame DrawMarginFrame { get; } = new() { Stroke = null };

    private readonly ConcurrentDictionary<string, List<(DateTime Timestamp, float Value)>> _sensorHistory = new();

    private readonly float[] _packageTempValues = new float[1];
    private readonly float[] _fanSpeedValues = new float[1];

    public float TemperatureMax { get; } = 110f;

    public SensorsPipeViewModel()
    {
        _sensorHistory["CPU Package"] = new List<(DateTime, float)> { (DateTime.Now, 60f) };
        _sensorHistory["fan #1"] = new List<(DateTime, float)> { (DateTime.Now, 1200f) };

        SetupInitialCharts();
        Task.Run(ReceiveSensorUpdates);
    }

    private void SetupInitialCharts()
    {
        TemperatureLabels = new[] { "CPU Package" };
        TemperatureSeries = new ObservableCollection<ISeries>
        {
            new ColumnSeries<float>
            {
                Name = "CPU Temp",
                Values = _packageTempValues,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Middle,
                Fill = GetTemperatureColor(_packageTempValues[0])
            }
        };

        XAxes = new[]
        {
            new Axis
            {
                Labels = TemperatureLabels,
                LabelsRotation = 0,
                Padding = new LiveChartsCore.Drawing.Padding(10)
            }
        };

        YAxes = new[]
        {
            new Axis { Name = "°C", MinLimit = 0, MaxLimit = TemperatureMax }
        };

        FanLabels = new[] { "fan #1" };
        FanSeries = new ObservableCollection<ISeries>
        {
            new RowSeries<float>
            {
                Name = "Fan RPM",
                Values = _fanSpeedValues,
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End
            }
        };

        FanXAxes = new[]
        {
            new Axis { Name = "RPM", MinLimit = 0 }
        };

        FanYAxes = new[]
        {
            new Axis { Labels = FanLabels }
        };
    }

    private async Task ReceiveSensorUpdates()
    {
        bool output_shown_once = false;
        while (true)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", "SensorPipe", PipeDirection.In);
                await pipe.ConnectAsync(2000);
                using var reader = new StreamReader(pipe);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var readings = JsonSerializer.Deserialize<List<SensorReading>>(line);
                    if (readings == null) continue;

                    App.Main_Window.DispatcherQueue.TryEnqueue(() =>
                    {
                        if(output_shown_once == false)
                            Debug.WriteLine($"\nReadings...");
                        
                        foreach (var r in readings)
                        {
                            var list = _sensorHistory.GetOrAdd(r.Name, _ => new());
                            list.Add((r.Timestamp, r.Value));
                            if (list.Count > 50) list.RemoveAt(0);

                            if (output_shown_once == false)
                            {
                                string line = $"\n{r.Timestamp:HH:mm:ss} [{r.Category}] {r.Name}: {r.Value}";
                                Debug.WriteLine($"{line}");
                            }
                        }

                        output_shown_once = true;

                        UpdateChartData();

                        // Send to TrayIconVM to use in the popup
                        WeakReferenceMessenger.Default.Send(new Msg_Readings { SensorReadings = readings});

                    });
                }
            }
            catch
            {
                await Task.Delay(2000);
            }
        }
    }

    private void UpdateChartData()
    {
        // CPU Temp
        if (_sensorHistory.TryGetValue("Temperature", out var pkg) && pkg.Count > 0)
        {
            var val = pkg.Last().Value;
            _packageTempValues[0] = val;

            if (TemperatureSeries[0] is ColumnSeries<float> tempSeries)
            {
                tempSeries.Fill = GetTemperatureColor(val);
            }
        }

        // Fan RPM
        var fan = _sensorHistory.FirstOrDefault(kv => kv.Key.Contains("fan", StringComparison.OrdinalIgnoreCase));
        if (fan.Value.Count > 0)
        {
            _fanSpeedValues[0] = fan.Value.Last().Value;
        }
    }

    private SolidColorPaint GetTemperatureColor(float value) =>
        value switch
        {
            <= 50f => new SolidColorPaint(SKColors.Green),
            <= 75f => new SolidColorPaint(SKColors.Orange),
            _ => new SolidColorPaint(SKColors.Red)
        };
}