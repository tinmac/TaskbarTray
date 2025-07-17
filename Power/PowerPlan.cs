using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerSwitch.ViewModels;
using PowerSwitch.Views;

namespace PowerSwitch.Power
{
    public class PowerPlan
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string DisplayName => IsActive ? $"{Name} (Active)" : Name;
        public bool IsActive { get; set; }
        public PowerMode PowerMode { get; set; }
        public string IconPath_WhiteFG { get; set; } = string.Empty;
        public string IconPath_DarkFG { get; set; } = string.Empty;
    }

}
