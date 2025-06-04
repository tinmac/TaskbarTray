using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace TaskbarTray
{

    public static class PowerModeChanger
    {
        static readonly Guid GUID_PROCESSOR_SETTINGS_SUBGROUP = new("54533251-82be-4824-96c1-47b60b740d00");
        static readonly Guid GUID_PROCESSOR_PERF_PREFERENCE_POLICY = new("36687f9e-e3a5-4dbf-b1dc-15eb381c6863");

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, IntPtr SchemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerWriteACValueIndex(IntPtr RootPowerKey, IntPtr SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerWriteDCValueIndex(IntPtr RootPowerKey, IntPtr SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint DcValueIndex);


        public static void SetPowerModeToBestEfficiency()
        {
            IntPtr activeSchemePtr;
            uint result = PowerGetActiveScheme(IntPtr.Zero, out activeSchemePtr);
            if (result != 0)
            {
                Console.WriteLine("Failed to get active power scheme.");
                return;
            }

            Guid subGroup = GUID_PROCESSOR_SETTINGS_SUBGROUP;
            Guid setting = GUID_PROCESSOR_PERF_PREFERENCE_POLICY;
            uint value = 3; // 0 = Best performance, 3 = Best power efficiency

            PowerWriteACValueIndex(IntPtr.Zero, activeSchemePtr, ref subGroup, ref setting, value);
            PowerWriteDCValueIndex(IntPtr.Zero, activeSchemePtr, ref subGroup, ref setting, value);

            PowerSetActiveScheme(IntPtr.Zero, activeSchemePtr);

            Console.WriteLine("Power mode set to Best Power Efficiency.");
        }

    }
}
