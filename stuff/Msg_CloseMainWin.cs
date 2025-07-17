using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;

namespace PowerSwitch.stuff
{
    public class MyMessage
    {
        public string Content { get; set; }
        public bool CloseMainWin { get; set; }
        public bool ThemeChanged_Light { get; set; }
       // public bool ThemeChanged_Dark { get; set; }
    }

    // Updated Msg_Readings to include ActivePlanGuid
    public class Msg_Readings
    {
        public List<SensorReading> SensorReadings { get; set; }
        public Guid ActivePlanGuid { get; set; } // NEW
    }

    public class Msg_CloseMainWin
    {
        public bool CloseMainWin { get; set; }
    }
    // SensorPipePayload and SensorReading moved to Common.Models
}
