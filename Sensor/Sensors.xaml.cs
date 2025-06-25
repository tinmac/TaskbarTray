using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using TaskbarTray.Sensor;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace TaskbarTray.Views
{
    public sealed partial class Sensors : Page
    {
        private readonly ILogger<Sensors> _logr;

        public TaskbarIcon TrayIcon { get; set; }

        public SensorsViewModel ViewModel { get; }

        public Sensors()
        {
            InitializeComponent();

            _logr = Ioc.Default.GetRequiredService<ILogger<Sensors>>();

            ViewModel = Ioc.Default.GetRequiredService<SensorsViewModel>();

            ViewModel.TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        }
    }
}
