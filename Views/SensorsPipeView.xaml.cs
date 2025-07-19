using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.Mvvm.DependencyInjection;
using PowerSwitch.SensorPipeService;
using PowerSwitch.ViewModels;



namespace PowerSwitch.Views;


public sealed partial class SensorsPipeView : Page
{
    public TaskbarIcon TrayIcon { get; set; }

    public SensorsPipeViewModel ViewModel { get; }

    public SensorsPipeView()
    {
        InitializeComponent();

        ViewModel = Ioc.Default.GetRequiredService<SensorsPipeViewModel>();

        //ViewModel.TheDispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    }
}
