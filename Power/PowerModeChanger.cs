using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TaskbarTray.Power
{

    public static class PowerModeChanger
    {
        static readonly Guid GUID_PROCESSOR_SETTINGS_SUBGROUP = new("54533251-82be-4824-96c1-47b60b740d00");
        static readonly Guid GUID_PROCESSOR_PERF_PREFERENCE_POLICY = new("36687f9e-e3a5-4dbf-b1dc-15eb381c6863");

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerGetActiveScheme(nint UserRootPowerKey, out nint ActivePolicyGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerSetActiveScheme(nint UserRootPowerKey, nint SchemeGuid);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerWriteACValueIndex(nint RootPowerKey, nint SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        static extern uint PowerWriteDCValueIndex(nint RootPowerKey, nint SchemeGuid,
            ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid,
            uint DcValueIndex);


        public static void SetPowerModeToBestEfficiency()
        {
            try
            {

                nint activeSchemePtr;
                uint result = PowerGetActiveScheme(nint.Zero, out activeSchemePtr);
                if (result != 0)
                {
                    Console.WriteLine("Failed to get active power scheme.");
                    return;
                }

                Guid subGroup = GUID_PROCESSOR_SETTINGS_SUBGROUP;
                Guid setting = GUID_PROCESSOR_PERF_PREFERENCE_POLICY;
                uint value = 3; // 0 = Best performance, 3 = Best power efficiency

                PowerWriteACValueIndex(nint.Zero, activeSchemePtr, ref subGroup, ref setting, value);
                PowerWriteDCValueIndex(nint.Zero, activeSchemePtr, ref subGroup, ref setting, value);

                PowerSetActiveScheme(nint.Zero, activeSchemePtr);

                Console.WriteLine("Power mode set to Best Power Efficiency.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception in SetPowerModeToBestEfficiency");
            }

        }
    }
}
