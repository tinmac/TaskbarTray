using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;


namespace TaskbarTray.ViewModels
{
    public class SensorsViewModel : ObservableObject
    {
        private readonly ILogger<SensorsViewModel> _logr;
        public Microsoft.UI.Dispatching.DispatcherQueue TheDispatcher { get; set; }

        public string Temo_Cpu { get; set; }

        public SensorsViewModel()
        {
              
        }


    }
}
