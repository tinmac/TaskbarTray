using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray.Models
{
    public class SensorReading
    {
        public string Name { get; set; } = string.Empty;
        public float Value { get; set; }
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
