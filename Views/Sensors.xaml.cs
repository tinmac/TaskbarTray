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
using TaskbarTray.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TaskbarTray.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Sensors : Page
    {
        private readonly ILogger<Sensors> _logr;

        public TaskbarIcon TrayIcon { get; set; }

        public SensorsViewModel ViewModel { get; }// = new TrayIconVM();


        public Sensors()
        {
            InitializeComponent();

            _logr = Ioc.Default.GetRequiredService<ILogger<Sensors>>();

            ViewModel = Ioc.Default.GetRequiredService<SensorsViewModel>();

        }
    }
}
