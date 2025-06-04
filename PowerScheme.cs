using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray
{
    public class PowerScheme
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
        public string DisplayName => IsActive ? $"{Name} (Active)" : Name;
        public bool IsActive { get; set; }
    }
}
