//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Drawing;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using TaskbarTray.Views;


//namespace TaskbarTray.Power
//{

//    public class PowerSchemeManager
//    {
//        private static readonly Guid SUB_PROCESSOR = new Guid("54533251-82be-4824-96c1-47b60b740d00");
//        private static readonly Guid PROCESSOR_MAX = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");

//        private const uint ACCESS_SCHEME = 16;


//        #region DllImport Declarations
//        //
//        // AC CPU % Management
//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerReadACValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
//            ref Guid PowerSettingGuid, out uint AcValueIndex);

//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerWriteACValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
//            ref Guid PowerSettingGuid, uint AcValueIndex);

//        // DC CPU % Management
//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerReadDCValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
//            ref Guid PowerSettingGuid, out uint DcValueIndex);

//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerWriteDCValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
//            ref Guid PowerSettingGuid, uint DcValueIndex);


//        // Power Plan Management
//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerSetActiveScheme(nint UserRootPowerKey, ref Guid SchemeGuid);


//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerEnumerate(nint RootPowerKey, nint SchemeGuid, nint SubGroupOfPowerSettingsGuid,
//            uint AccessFlags, uint Index, nint Buffer, ref uint BufferSize);

//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerReadFriendlyName(nint RootPowerKey, ref Guid SchemeGuid, nint SubGroupOfPowerSettingsGuid,
//            nint PowerSettingGuid, byte[] Buffer, ref uint BufferSize);

//        [DllImport("powrprof.dll", SetLastError = true)]
//        private static extern uint PowerGetActiveScheme(nint UserRootPowerKey, out nint ActivePolicyGuid);


//        [DllImport("kernel32.dll")]
//        private static extern void LocalFree(nint ptr);

//        #endregion


//        #region Power Schemes

//        public static List<PowerPlan> LoadPowerSchemes()
//        {
//            var plans = new List<PowerPlan>();
//            Guid activeGuid = GetActivePlanGuid();

//            uint index = 0;
//            nint guidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
//            uint bufferSize;

//            while (true)
//            {
//                bufferSize = (uint)Marshal.SizeOf(typeof(Guid));
//                if (PowerEnumerate(nint.Zero, nint.Zero, nint.Zero, ACCESS_SCHEME, index, guidPtr, ref bufferSize) != 0)
//                    break;

//                Guid planGuid = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));

//                uint nameSize = 1024;
//                byte[] nameBuffer = new byte[nameSize];
//                string name = "(Unnamed)";
//                if (PowerReadFriendlyName(nint.Zero, ref planGuid, nint.Zero, nint.Zero, nameBuffer, ref nameSize) == 0)
//                    name = Encoding.Unicode.GetString(nameBuffer, 0, (int)nameSize - 2);


//                // PowerMode is derived from the Plan Guid, in case language is different
              
//                #region Plan GUIDs notes...
//                //
//                // Power Saver            a1841308-3541-4fab-bc81-f71556f20b4a
//                // Balanced               381b4222-f694-41f0-9685-ff5bb260df2e
//                // High Performance       8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
//                // Ultimate Performance   8c5e7fda-e8bf-4a96-b3b9-1b0c2d0f2d3a  - not on MLAP - sited on tinternet as Windows 10 Pro for Workstations only 
//                //
//                // I havent included the plans below as they are not commonly used 
//                //
//                // found on my laptop but may not be present on all systems:
//                // Ultimate Performance   e9a42b02-d5df-448d-aa00-03f14749eb61  - on MLAP
//                //
//                // may not be present on all systems, but found on some AMD systems:
//                // AMD Ryzen Balanced     45bcc044-d885-43e2-8605-ee558b2a56b0 (varies by driver/version)
//                //
//                #endregion

//                PowerMode pm = PowerMode.None;
//                if (planGuid == Guid.Parse("a1841308-3541-4fab-bc81-f71556f20b4a"))
//                    pm = PowerMode.Eco;
//                else if (planGuid == Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e"))
//                    pm = PowerMode.Balanced;
//                else if (planGuid == Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"))
//                    pm = PowerMode.High;

//                //Debug.WriteLine($"Power Mode [{pm}]");

//                plans.Add(new PowerPlan
//                {
//                    Guid = planGuid,
//                    Name = name,
//                    IsActive = planGuid == activeGuid,
//                    PowerMode = pm
//                });

//                index++;
//            }

//            Marshal.FreeHGlobal(guidPtr);
//            return plans;
//        }

//        public static Guid GetActivePlanGuid()
//        {
//            PowerGetActiveScheme(nint.Zero, out nint ptr);
//            Guid guid = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
//            LocalFree(ptr);
//            return guid;
//        }

//        public static bool SetActivePowerPlan(Guid planGuid)
//        {
//            return PowerSetActiveScheme(nint.Zero, ref planGuid) == 0;
//        }

//        #endregion


//        #region CPU

//        /// <summary>
//        /// Gets the CPU max percentage for Plugged-In (AC Power)
//        /// </summary>
//        public static int GetCpuMax_AC(Guid schemeGuid)
//        {
//            uint value;
//            Guid subProcessor = SUB_PROCESSOR;
//            Guid processorMax = PROCESSOR_MAX;

//            var res = PowerReadACValueIndex(nint.Zero, ref schemeGuid, ref subProcessor, ref processorMax, out value);
//            if (res != 0)
//            {
//                Debug.WriteLine($"AC Unable to read CPU max percent");
//                return -1; // Return -1 or throw an exception based on your error handling strategy
//            }

//            int percent = (int)value;

//            Debug.WriteLine($"Get AC CPU Max: {percent}%");

//            return percent;
//        }

//        /// <summary>
//        /// Sets the CPU max percentage for Plugged-In (AC Power)
//        /// </summary>
//        public static bool SetCpuMax_AC(Guid schemeGuid, int percent)
//        {
//            Debug.WriteLine($"\nSet AC Cpu Max to {percent}%...");
//            uint val = (uint)Math.Clamp(percent, 0, 100);
//            Guid subProcessor = SUB_PROCESSOR;
//            Guid processorMax = PROCESSOR_MAX;

//            var res = PowerWriteACValueIndex(nint.Zero, ref schemeGuid, ref subProcessor, ref processorMax, val);
//            if (res != 0)
//            {
//                Debug.WriteLine($"Failed to write AC CPU max percent.");
//                return false;
//            }
//            // Apply changes by re-setting active scheme
//            PowerSetActiveScheme(nint.Zero, ref schemeGuid);

//            return true;
//        }

//        /// <summary>
//        /// Gets the CPU max percentage for Battery-Powered (DC Power)
//        /// </summary>
//        public static int GetCpuMax_DC(Guid schemeGuid)
//        {
//            uint value;
//            Guid subProcessor = SUB_PROCESSOR;
//            Guid processorMax = PROCESSOR_MAX;

//            var res = PowerReadDCValueIndex(nint.Zero, ref schemeGuid, ref subProcessor, ref processorMax, out value);
//            if (res != 0)
//            {
//                Debug.WriteLine($"DC Unable to read CPU max percent");
//                return -1; // Return -1 or throw an exception based on your error handling strategy
//            }

//            int percent = (int)value;

//            Debug.WriteLine($"Get DC CPU Max: {percent}%");

//            return percent;
//        }

//        /// <summary>
//        /// Sets the CPU max percentage for Battery-Powered (DC Power)
//        /// </summary>
//        public static bool SetCpuMax_DC(Guid schemeGuid, uint percent)
//        {
//            Debug.WriteLine($"\nSet DC Cpu Max to {percent}%...");
//            uint val = Math.Clamp(percent, 0, 100);
//            Guid subProcessor = SUB_PROCESSOR;
//            Guid processorMax = PROCESSOR_MAX;

//            var res = PowerWriteDCValueIndex(nint.Zero, ref schemeGuid, ref subProcessor, ref processorMax, val);
//            if (res != 0)
//            {
//                Debug.WriteLine($"Failed to write DC CPU max percent.");
//                return false;
//            }
//            // Apply changes by re-setting active scheme
//            PowerSetActiveScheme(nint.Zero, ref schemeGuid);

//            return true;
//        }

//        #endregion

//    }
//}
