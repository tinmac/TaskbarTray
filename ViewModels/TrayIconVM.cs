using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PowerSwitch.Services;
using PowerSwitch.Power;
using PowerSwitch.stuff;
using Windows.Devices.Power;
using Windows.Security.Isolation;
using Windows.System.Power;
using Common.Models;
using System.Text.Json;

namespace PowerSwitch.ViewModels
{
    public enum PowerMode
    {
        None,
        Eco,
        Balanced,
        High
    }

    public partial class TrayIconVM : ObservableObject
    {
        private readonly ILogger<TrayIconVM> _logr;
        private readonly ISettingsService _settingsService;
        private const string TemperatureUnitKey = "TemperatureUnit";

        [ObservableProperty]
        private BitmapImage _selectedImage;

        [ObservableProperty]
        private PowerMode _selectedPowerMode;

        [ObservableProperty]
        private bool _show_OpenWindowMenuItem;

        [ObservableProperty]
        private PowerPlan _activeScheme;

        [ObservableProperty]
        private bool _isSaverChecked;

        [ObservableProperty]
        private bool _isBalancedChecked;

        [ObservableProperty]
        private bool _isHighChecked;

        [ObservableProperty]
        private string _batteryPercentage;

        [ObservableProperty]
        private Common.Models.ServiceStatus _serviceStatus = Common.Models.ServiceStatus.Unknown;

        public Microsoft.UI.Xaml.Media.Brush ServiceStatusBrush =>
            ServiceStatus == Common.Models.ServiceStatus.Running
                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGreen)
                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

        List<PowerPlan> PowerPlans = new();

        private float? _latestCpuTemp;
        public float? LatestCpuTemp
        {
            get => _latestCpuTemp;
            private set
            {
                if (SetProperty(ref _latestCpuTemp, value))
                {
                    OnPropertyChanged(nameof(CpuTempTooltip));
                }
            }
        }

        public string CpuTempTooltip
        {
            get
            {
                if (!LatestCpuTemp.HasValue)
                    return "CPU: N/A";
                float temp = LatestCpuTemp.Value;
                if (TemperatureUnit == TemperatureUnit.Fahrenheit)
                    temp = temp * 9 / 5 + 32;
                string unit = TemperatureUnit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
                return $"CPU: {temp:F1} {unit}";
            }
        }

        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }

        private TemperatureUnit _temperatureUnit = TemperatureUnit.Celsius;

        public TemperatureUnit TemperatureUnit
        {
            get => _temperatureUnit;
            set
            {
                if (SetProperty(ref _temperatureUnit, value))
                {
                    _ = _settingsService.SaveSettingAsync(TemperatureUnitKey, value);
                    OnPropertyChanged(nameof(CpuTempTooltip));
                }
            }
        }



        // Constructor
        public TrayIconVM(ILogger<TrayIconVM> logr, ISettingsService settingsService)
        {
            _logr = logr;
            _settingsService = settingsService;

            SelectedImage = new BitmapImage(new Uri("ms-appx:///Assets/ico/gauge.ico")); // Default icon
            ActiveScheme = new PowerPlan();
            SelectedPowerMode = PowerMode.None;
            Show_OpenWindowMenuItem = false;
            BatteryPercentage = string.Empty;

            _ = LoadTemperatureUnitAsync();

            WeakReferenceMessenger.Default.Register<MyMessage>(this, (r, message) =>
            {
                if (message.CloseMainWin)
                    Show_OpenWindowMenuItem = true;

                TheDispatcher.TryEnqueue(() =>
                {
                    if (SelectedImage == null)
                        return;

                    if (message.ThemeChanged_Light)
                    {
                        SelectedImage = new BitmapImage(new Uri(ActiveScheme.IconPath_DarkFG));
                        _logr.LogInformation($" Switched to dark foreground icons");
                    }
                    else
                    {
                        SelectedImage = new BitmapImage(new Uri(ActiveScheme.IconPath_WhiteFG));
                        _logr.LogInformation($" Switched to light foreground icons");
                    }
                });
            });


            Show_OpenWindowMenuItem = true;

            PowerManager.BatteryStatusChanged += OnBatteryStatusChanged;
            PowerManager.PowerSupplyStatusChanged += OnPowerSupplyStatusChanged;

            // Start background sensor pipe reading worker service
            Task.Run(ReceiveSensorUpdates);
            Task.Run(PollServiceStatusAsync); // Start polling service status
        }


        public async Task InitAsync()
        {
            try
            {
                CreatePlans();
                await LoadPlansAsync();

                // Ensure settings are loaded before startup check
                var settingsVM = Ioc.Default.GetService<SettingsViewModel>();
                if (settingsVM != null)
                    await settingsVM.LoadPlanToggleSettingsAsync();
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception during TrayIconVM initialization");
            }
        }

        private bool _startupPlanCheckDone = false;

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

                        SensorPipePayload payload = null;
                        try
                        {
                            payload = JsonSerializer.Deserialize<SensorPipePayload>(line);
                        }
                        catch { }
                        if (payload == null || payload.Sensors == null) continue;

                        // Run startup plan selection logic only once after first valid payload
                        if (!_startupPlanCheckDone && payload.ActivePlanGuid != Guid.Empty)
                        {
                            RunStartupPlanSelection(payload.ActivePlanGuid);
                            _startupPlanCheckDone = true;
                        }

                        var readings = payload.Sensors;

                        // Change to recieved Power Plan       
                        var SelectedPlan = PowerPlans.FirstOrDefault(p => p.Guid == payload.ActivePlanGuid);
                        SetPowerPlan(SelectedPlan);


                        App.Main_Window.DispatcherQueue.TryEnqueue(() =>
                        {
                            var temp = readings?.FirstOrDefault(s => s.Name.Contains("Tctl/Tdie", StringComparison.OrdinalIgnoreCase))
                                       ?? readings?.FirstOrDefault(s => s.Category == "Temperature");
                           
                            if (temp != null)
                            {
                                //_logr.LogInformation($"Received CPU Temp: {LatestCpuTemp}");
                                LatestCpuTemp = temp.Value;
                            }
                        });

                        //if (!output_shown_once)
                        Debug.WriteLine($"\nReadings...");

                        foreach (var r in readings)
                        {
                            //if (!output_shown_once)
                            {
                                string debugLine = $"{r.Timestamp:HH:mm:ss} [{r.Category}] {r.Name}: {r.Value.ToString()}";
                                Debug.WriteLine(debugLine);
                            }
                        }
                        output_shown_once = true;

                        // Send to TrayIconVM to use in the popup
                        //WeakReferenceMessenger.Default.Send(new Msg_Readings { SensorReadings = readings, ActivePlanGuid = activePlanGuid });
                    }
                }
                catch
                {
                    await Task.Delay(2000);
                }
            }
        }

        private void CreatePlans()
        {
            try
            {
                _logr.LogInformation("Creating power plans");
                var power_saver = new PowerPlan
                {
                    Name = "Power Saver",
                    Guid = Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"),
                    IsActive = true,
                    PowerMode = PowerMode.Eco,
                    IconPath_DarkFG = "ms-appx:///Assets/ico/gauge-min.ico",
                    IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-min-wh.ico"
                };

                var balanced = new PowerPlan
                {
                    Name = "Balanced",
                    Guid = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"),
                    IsActive = true,
                    PowerMode = PowerMode.Balanced,
                    IconPath_DarkFG = "ms-appx:///Assets/ico/gauge.ico",
                    IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-wh.ico"
                };

                var high_performance = new PowerPlan
                {
                    Name = "High Performance",
                    Guid = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
                    IsActive = false,
                    PowerMode = PowerMode.High,
                    IconPath_DarkFG = "ms-appx:///Assets/ico/gauge-max.ico",
                    IconPath_WhiteFG = "ms-appx:///Assets/ico/gauge-max-wh.ico"
                };

                PowerPlans.Add(power_saver);
                PowerPlans.Add(balanced);
                PowerPlans.Add(high_performance);
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception creating power plans");
                throw;
            }
        }

        public async Task LoadPlansAsync()
        {
            try
            {
                var active_plan_guid = await Task.Run(() => PowerPlanManager.GetActivePlanGuid());
                ActiveScheme = PowerPlans.First(p => p.Guid == active_plan_guid);
                _logr.LogInformation($"Loaded Plan: {ActiveScheme?.Name}");
                UpdateUi();
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception loading power plans");
                _logr.LogError($"Exception loading power plans: {ex.Message}");
            }
        }

        public async void SetPowerPlan(PowerPlan Scheme)
        {
            try
            {
                //if (Scheme == null)
                //{
                //    //_logr.LogWarning("Attempted to set power plan to null.");
                //    return;
                //}
               
                if (Scheme == null || Scheme?.Guid == ActiveScheme?.Guid)
                {
                    //_logr.LogInformation($"Already on the selected power plan: {Scheme.Name}");
                    return;
                }

                bool success = await Task.Run(() => PowerPlanManager.SetActivePowerPlan(Scheme.Guid));

                ActiveScheme = Scheme;
                if (success)
                {
                    UpdateUi();
                    _logr.LogInformation($"Switched to: {Scheme.Name}");
                }
                else
                {
                    _logr.LogWarning("Failed to set power plan to {PlanName}", Scheme.Name);
                    _logr.LogWarning($"Failed to set power plan.");
                }

            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception setting power plan to {PlanName}", Scheme.Name);
                _logr.LogError($"Exception while switching plans: {ex.Message}");
            }
        }

        private void UpdateUi()
        {
            try
            {
                App.Main_Window.DispatcherQueue.TryEnqueue(async () =>
                {

                    var isWinDark = ThemeHelper.IsWindowsInDarkMode();
                    if (isWinDark)
                    {
                        var img = new BitmapImage(new Uri(ActiveScheme.IconPath_WhiteFG));
                        SelectedImage = img;
                    }
                    else
                    {
                        var img = new BitmapImage(new Uri(ActiveScheme.IconPath_DarkFG));
                        SelectedImage = img;
                    }
                    if (ActiveScheme.PowerMode == PowerMode.Eco)
                    {
                        IsSaverChecked = true;
                        IsBalancedChecked = false;
                        IsHighChecked = false;
                    }
                    else if (ActiveScheme.PowerMode == PowerMode.Balanced)
                    {
                        IsSaverChecked = false;
                        IsBalancedChecked = true;
                        IsHighChecked = false;
                    }
                    else if (ActiveScheme.PowerMode == PowerMode.High)
                    {
                        IsSaverChecked = false;
                        IsBalancedChecked = false;
                        IsHighChecked = true;
                    }
                    else if (ActiveScheme.PowerMode == PowerMode.None)
                    {
                        IsSaverChecked = false;
                        IsBalancedChecked = false;
                        IsHighChecked = false;
                    }
                    else
                    {
                        IsSaverChecked = false;
                        IsBalancedChecked = false;
                        IsHighChecked = false;
                        _logr.LogInformation($"No known plan!");
                    }
                    GetBatteryPercentage();
                });
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception updating UI");
            }
        }

        private void GetBatteryPercentage()
        {
            try
            {
                var report = Battery.AggregateBattery.GetReport();
                var remaining = report.RemainingCapacityInMilliwattHours;
                var full = report.FullChargeCapacityInMilliwattHours;
                var status = report.Status;
                if (remaining.HasValue && full.HasValue && full != 0)
                {
                    int percentage = (int)((remaining.Value / (double)full.Value) * 100);
                    BatteryPercentage = $"Battery: {percentage}% ({status})";
                }
                else
                {
                    BatteryPercentage = "Battery info not available.";
                }
                _logr.LogInformation($"{BatteryPercentage}");
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception getting battery percentage");
                BatteryPercentage = "Exception getting battery info";
            }
        }

        private async Task LoadTemperatureUnitAsync()
        {
            var persisted = await _settingsService.GetSettingAsync<TemperatureUnit>(TemperatureUnitKey);
            TemperatureUnit = persisted;
            _logr.LogInformation("Temperature unit: {TemperatureUnit}", TemperatureUnit);
        }

        private void OnBatteryStatusChanged(object sender, object e)
        {
            try
            {
                _logr.LogInformation("Battery status changed");
                TheDispatcher.TryEnqueue(() => { GetBatteryPercentage(); });
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception handling battery status change");
            }
        }

        private void OnPowerSupplyStatusChanged(object sender, object e)
        {
            try
            {
                _logr.LogInformation("Power supply status changed");
                TheDispatcher.TryEnqueue(() => { GetBatteryPercentage(); });
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception handling power supply status change");
            }
        }



        #region RELAY COMMANDS

        [RelayCommand]
        public void Set_PowerSaver()
        {
            if (ActiveScheme.PowerMode == PowerMode.Eco)
            {
                UpdateUi();
                return;
            }
            var power_saver = PowerPlans.First(p => p.PowerMode == PowerMode.Eco);
            SetPowerPlan(power_saver);
        }

        [RelayCommand]
        public void Set_Balanced()
        {
            if (ActiveScheme.PowerMode == PowerMode.Balanced)
            {
                UpdateUi();
                return;
            }
            var balanced = PowerPlans.First(p => p.PowerMode == PowerMode.Balanced);
            SetPowerPlan(balanced);
        }

        [RelayCommand]
        public void Set_HighPerformance()
        {
            if (ActiveScheme.PowerMode == PowerMode.High)
            {
                UpdateUi();
                return;
            }
            var high_performance = PowerPlans.First(p => p.PowerMode == PowerMode.High);
            SetPowerPlan(high_performance);
        }

        private List<PowerPlan> GetIncludedPlans()
        {
            var included = new List<PowerPlan>();
            var settingsVM = Ioc.Default.GetService<SettingsViewModel>();
            if (settingsVM == null)
                return PowerPlans; // fallback: all plans
            if (settingsVM.IncludePowerSaver)
                included.Add(PowerPlans.FirstOrDefault(p => p.PowerMode == PowerMode.Eco));
            if (settingsVM.IncludeBalanced)
                included.Add(PowerPlans.FirstOrDefault(p => p.PowerMode == PowerMode.Balanced));
            if (settingsVM.IncludeHighPerformance)
                included.Add(PowerPlans.FirstOrDefault(p => p.PowerMode == PowerMode.High));
            return included.Where(p => p != null).ToList();
        }

        private PowerMode? _previousPowerMode = null;
     
        [RelayCommand]
        public void ToggleSpeed()
        {
            var includedPlans = GetIncludedPlans();
            if (includedPlans.Count < 2)
            {
                // If only one plan is included and it's not active, switch to it
                if (includedPlans.Count == 1 && ActiveScheme.Guid != includedPlans[0].Guid)
                {
                    SetPowerPlan(includedPlans[0]);
                }
                // Otherwise, do nothing
                return;
            }

            // If the active plan is not in the included plans, switch to the first included plan
            if (!includedPlans.Any(p => p.Guid == ActiveScheme.Guid))
            {
                SetPowerPlan(includedPlans[0]);
                return;
            }

            if (includedPlans.Count == 3)
            {
                var eco = includedPlans.FirstOrDefault(p => p.PowerMode == PowerMode.Eco);
                var balanced = includedPlans.FirstOrDefault(p => p.PowerMode == PowerMode.Balanced);
                var high = includedPlans.FirstOrDefault(p => p.PowerMode == PowerMode.High);
                var current = ActiveScheme;

                PowerPlan nextPlan = null;
                if (current.PowerMode == PowerMode.High)
                {
                    nextPlan = balanced;
                }
                else if (current.PowerMode == PowerMode.Eco)
                {
                    nextPlan = balanced;
                }
                else if (current.PowerMode == PowerMode.Balanced)
                {
                    if (_previousPowerMode == PowerMode.Eco)
                        nextPlan = high;
                    else if (_previousPowerMode == PowerMode.High)
                        nextPlan = eco;
                    else
                        nextPlan = high; // default direction
                }
                else
                {
                    nextPlan = balanced; // fallback
                }
                _previousPowerMode = current.PowerMode;
                SetPowerPlan(nextPlan);
            }
            else
            {
                var currentIndex = includedPlans.FindIndex(p => p.Guid == ActiveScheme.Guid);
                var nextIndex = (currentIndex + 1) % includedPlans.Count;
                SetPowerPlan(includedPlans[nextIndex]);
            }
        }

        [RelayCommand]
        public void ShowHideWindow(string show)
        {
            try
            {
                if (show == "True")
                {
                    _logr.LogInformation("Showing main window");
                    _logr.LogInformation($"Show Main Window...");
                    if (App.Main_Window == null)
                    {
                        App.Main_Window = new MainWindow();
                    }
                    App.Main_Window.Show();
                    Show_OpenWindowMenuItem = false;
                }
                else
                {
                    _logr.LogInformation("Hiding main window");
                    _logr.LogInformation($"Hide Main Window...");
                    App.Main_Window?.Close();
                    Show_OpenWindowMenuItem = true;
                }
            }
            catch (Exception ex)
            {
                _logr.LogError(ex, "Exception showing/hiding window");
            }
        }

        [RelayCommand]
        public async void OpenContextMenu()
        {
            var persisted = await _settingsService.GetSettingAsync<TemperatureUnit>(TemperatureUnitKey);
            TemperatureUnit = persisted;
            _logr.LogInformation("Temperature unit: {TemperatureUnit}", TemperatureUnit);
            GetBatteryPercentage();
        }

        [RelayCommand]
        public void ShowCpuTempPopup()
        {
            string tempText = LatestCpuTemp.HasValue ? $"CPU: {LatestCpuTemp:F1} °C" : "CPU: N/A";
            _logr.LogInformation($"Show popup: {tempText}");
        }

        #endregion


        #region OTHER

        private BitmapImage ConvertEnumToImage(PowerPlan Schene)
        {
            var PowerMode = Schene.PowerMode;
            _logr.LogInformation($"Active PowerMode {PowerMode}");
            if (PowerMode == PowerMode.None)
            {
                _logr.LogInformation($"PowerMode is None, returning null image.");
                return null;
            }
            string uri = PowerMode switch
            {
                PowerMode.Eco => "ms-appx:///Assets/gauge_low.ico",
                PowerMode.Balanced => "ms-appx:///Assets/Inactive.ico",
                PowerMode.High => "ms-appx:///Assets/gauge_high.ico",
            };
            return new BitmapImage(new Uri(uri));
        }
        private void SetCPUPercentage()
        {
            Guid plan = PowerPlanManager.GetActivePlanGuid();
            int acCpu = PowerPlanManager.GetCpuMaxPercentage(plan);
            int dcCpu = PowerPlanManager.GetCpuMaxPercentageDC(plan);
            Console.WriteLine($"AC Max CPU: {acCpu}%");
            Console.WriteLine($"DC Max CPU: {dcCpu}%");
            PowerPlanManager.SetCpuMaxPercentage(plan, 65);
            PowerPlanManager.SetCpuMaxPercentageDC(plan, 63);
        }

        #endregion

        private void RunStartupPlanSelection(Guid activePlanGuid)
        {
            var includedPlans = GetIncludedPlans();
            var activePlan = PowerPlans.FirstOrDefault(p => p.Guid == activePlanGuid);
            if (includedPlans.Count == 1 && activePlan?.Guid != includedPlans[0].Guid)
            {
                SetPowerPlan(includedPlans[0]);
                _logr.LogInformation($"Startup: Only one plan included, switched to {includedPlans[0].Name}");
            }
            else if (!includedPlans.Any(p => p.Guid == activePlanGuid))
            {
                var lowest = includedPlans.OrderBy(p => p.PowerMode).First();
                SetPowerPlan(lowest);
                _logr.LogInformation($"Startup: Active plan not included, switched to lowest included plan {lowest.Name}");
            }
        }

        private async Task PollServiceStatusAsync()
        {
            while (true)
            {
                try
                {
                    var status = GetServiceStatus();
                    if (ServiceStatus != status)
                    {
                        ServiceStatus = status;
                    }
                }
                catch { }
                await Task.Delay(3000);
            }
        }

        private Common.Models.ServiceStatus GetServiceStatus()
        {
            try
            {
                using var sc = new System.ServiceProcess.ServiceController("PowerSwitchService");
                return sc.Status switch
                {
                    System.ServiceProcess.ServiceControllerStatus.Stopped => Common.Models.ServiceStatus.Stopped,
                    System.ServiceProcess.ServiceControllerStatus.StartPending => Common.Models.ServiceStatus.StartPending,
                    System.ServiceProcess.ServiceControllerStatus.StopPending => Common.Models.ServiceStatus.StopPending,
                    System.ServiceProcess.ServiceControllerStatus.Running => Common.Models.ServiceStatus.Running,
                    System.ServiceProcess.ServiceControllerStatus.ContinuePending => Common.Models.ServiceStatus.ContinuePending,
                    System.ServiceProcess.ServiceControllerStatus.PausePending => Common.Models.ServiceStatus.PausePending,
                    System.ServiceProcess.ServiceControllerStatus.Paused => Common.Models.ServiceStatus.Paused,
                    _ => Common.Models.ServiceStatus.Unknown
                };
            }
            catch
            {
                return Common.Models.ServiceStatus.Unknown;
            }
        }
    }
}

