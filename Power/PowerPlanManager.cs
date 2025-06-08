using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskbarTray.Power
{

    public static class PowerPlanManager
    {
        // This class provides methods to manage power plans in Windows.
        //
        // Docs:
        // https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/configure-power-settings
        //
        // Win32 Api - powrprof.h
        // https://learn.microsoft.com/en-us/windows/win32/api/powrprof/
        //
        // Attributes: 
        // https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerreadsettingattributes


        private static readonly Guid SUB_PROCESSOR = new Guid("54533251-82be-4824-96c1-47b60b740d00");
        private static readonly Guid PROCESSOR_MAX = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");
        private const uint ACCESS_SCHEME = 16;

        // AC CPU % Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadACValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, out uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerWriteACValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, uint AcValueIndex);

        // DC CPU % Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadDCValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, out uint DcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerWriteDCValueIndex(nint RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, uint DcValueIndex);


        // Power Plan Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(nint UserRootPowerKey, ref Guid SchemeGuid);


        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerEnumerate(nint RootPowerKey, nint SchemeGuid, nint SubGroupOfPowerSettingsGuid,
            uint AccessFlags, uint Index, nint Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadFriendlyName(nint RootPowerKey, ref Guid SchemeGuid, nint SubGroupOfPowerSettingsGuid,
            nint PowerSettingGuid, byte[] Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(nint UserRootPowerKey, out nint ActivePolicyGuid);


        [DllImport("kernel32.dll")]
        private static extern void LocalFree(nint ptr);

        public static List<PowerPlan> LoadPowerPlans()
        {
            var plans = new List<PowerPlan>();
            Guid activeGuid = GetActivePlanGuid();

            uint index = 0;
            nint guidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
            uint bufferSize;

            while (true)
            {
                bufferSize = (uint)Marshal.SizeOf(typeof(Guid));
                if (PowerEnumerate(nint.Zero, nint.Zero, nint.Zero, ACCESS_SCHEME, index, guidPtr, ref bufferSize) != 0)
                    break;

                Guid planGuid = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));

                uint nameSize = 1024;
                byte[] nameBuffer = new byte[nameSize];
                string name = "(Unnamed)";
                if (PowerReadFriendlyName(nint.Zero, ref planGuid, nint.Zero, nint.Zero, nameBuffer, ref nameSize) == 0)
                    name = Encoding.Unicode.GetString(nameBuffer, 0, (int)nameSize - 2);

                plans.Add(new PowerPlan
                {
                    Guid = planGuid,
                    Name = name,
                    IsActive = planGuid == activeGuid
                });

                index++;
            }

            Marshal.FreeHGlobal(guidPtr);
            return plans;
        }

        public static Guid GetActivePlanGuid()
        {
            PowerGetActiveScheme(nint.Zero, out nint ptr);
            Guid guid = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
            LocalFree(ptr);
            return guid;
        }

        public static bool SetActivePowerPlan(Guid planGuid)
        {
            return PowerSetActiveScheme(nint.Zero, ref planGuid) == 0;
        }


        public static int GetCpuMaxPercentage(Guid planGuid)
        {
            uint value;
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerReadACValueIndex(nint.Zero, ref planGuid, ref subProcessor, ref processorMax, out value);
            if (res != 0)
                throw new Exception("Unable to read CPU max percent.");
            return (int)value;
        }

        public static void SetCpuMaxPercentage(Guid planGuid, int percent)
        {
            uint val = (uint)Math.Clamp(percent, 0, 100);
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerWriteACValueIndex(nint.Zero, ref planGuid, ref subProcessor, ref processorMax, val);
            if (res != 0)
                throw new Exception("Failed to write CPU max percent.");

            // Apply changes by re-setting active scheme
            PowerSetActiveScheme(nint.Zero, ref planGuid);
        }


        // DC
        public static int GetCpuMaxPercentageDC(Guid planGuid)
        {
            uint value;
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerReadDCValueIndex(nint.Zero, ref planGuid, ref subProcessor, ref processorMax, out value);
            if (res != 0)
                throw new Exception("Unable to read CPU max percent (DC).");
            return (int)value;
        }

        public static void SetCpuMaxPercentageDC(Guid planGuid, int percent)
        {
            uint val = (uint)Math.Clamp(percent, 0, 100);
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerWriteDCValueIndex(nint.Zero, ref planGuid, ref subProcessor, ref processorMax, val);
            if (res != 0)
                throw new Exception("Failed to write CPU max percent (DC).");

            PowerSetActiveScheme(nint.Zero, ref planGuid); // Apply
        }

    }
}
