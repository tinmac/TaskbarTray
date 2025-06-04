using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TaskbarTray;

namespace PowerPlanSwitcher
{

    public static class PowerPlanManager
    {
        private static readonly Guid SUB_PROCESSOR = new Guid("54533251-82be-4824-96c1-47b60b740d00");
        private static readonly Guid PROCESSOR_MAX = new Guid("bc5038f7-23e0-4960-96da-33abaf5935ec");
        private const uint ACCESS_SCHEME = 16;

        // AC CPU % Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, out uint AcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerWriteACValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, uint AcValueIndex);

        // DC CPU % Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadDCValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, out uint DcValueIndex);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerWriteDCValueIndex(IntPtr RootPowerKey, ref Guid SchemeGuid, ref Guid SubGroupOfPowerSettingsGuid,
            ref Guid PowerSettingGuid, uint DcValueIndex);


        // Power Plan Management
        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerSetActiveScheme(IntPtr UserRootPowerKey, ref Guid SchemeGuid);


        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid,
            uint AccessFlags, uint Index, IntPtr Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingsGuid,
            IntPtr PowerSettingGuid, byte[] Buffer, ref uint BufferSize);

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);


        [DllImport("kernel32.dll")]
        private static extern void LocalFree(IntPtr ptr);

        public static List<PowerScheme> LoadPowerPlans()
        {
            var plans = new List<PowerScheme>();
            Guid activeGuid = GetActivePlanGuid();

            uint index = 0;
            IntPtr guidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
            uint bufferSize;

            while (true)
            {
                bufferSize = (uint)Marshal.SizeOf(typeof(Guid));
                if (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ACCESS_SCHEME, index, guidPtr, ref bufferSize) != 0)
                    break;

                Guid planGuid = (Guid)Marshal.PtrToStructure(guidPtr, typeof(Guid));

                uint nameSize = 1024;
                byte[] nameBuffer = new byte[nameSize];
                string name = "(Unnamed)";
                if (PowerReadFriendlyName(IntPtr.Zero, ref planGuid, IntPtr.Zero, IntPtr.Zero, nameBuffer, ref nameSize) == 0)
                    name = Encoding.Unicode.GetString(nameBuffer, 0, (int)nameSize - 2);

                plans.Add(new PowerScheme
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
            PowerGetActiveScheme(IntPtr.Zero, out IntPtr ptr);
            Guid guid = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
            LocalFree(ptr);
            return guid;
        }

        public static bool SetActivePowerPlan(Guid planGuid)
        {
            return PowerSetActiveScheme(IntPtr.Zero, ref planGuid) == 0;
        }

        // Fix for CS0199: Create local variables to hold the values of static readonly fields
        public static int GetCpuMaxPercentage(Guid planGuid)
        {
            uint value;
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerReadACValueIndex(IntPtr.Zero, ref planGuid, ref subProcessor, ref processorMax, out value);
            if (res != 0)
                throw new Exception("Unable to read CPU max percent.");
            return (int)value;
        }

        public static void SetCpuMaxPercentage(Guid planGuid, int percent)
        {
            uint val = (uint)Math.Clamp(percent, 0, 100);
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerWriteACValueIndex(IntPtr.Zero, ref planGuid, ref subProcessor, ref processorMax, val);
            if (res != 0)
                throw new Exception("Failed to write CPU max percent.");

            // Apply changes by re-setting active scheme
            PowerSetActiveScheme(IntPtr.Zero, ref planGuid);
        }


        // DC
        public static int GetCpuMaxPercentageDC(Guid planGuid)
        {
            uint value;
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerReadDCValueIndex(IntPtr.Zero, ref planGuid, ref subProcessor, ref processorMax, out value);
            if (res != 0)
                throw new Exception("Unable to read CPU max percent (DC).");
            return (int)value;
        }

        public static void SetCpuMaxPercentageDC(Guid planGuid, int percent)
        {
            uint val = (uint)Math.Clamp(percent, 0, 100);
            Guid subProcessor = SUB_PROCESSOR;
            Guid processorMax = PROCESSOR_MAX;

            var res = PowerWriteDCValueIndex(IntPtr.Zero, ref planGuid, ref subProcessor, ref processorMax, val);
            if (res != 0)
                throw new Exception("Failed to write CPU max percent (DC).");

            PowerSetActiveScheme(IntPtr.Zero, ref planGuid); // Apply
        }

    }
}
