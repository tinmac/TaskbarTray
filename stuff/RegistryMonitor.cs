using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarTray.stuff
{
    using Microsoft.Win32;
    using System;
    using System.Threading;

    public class RegistryMonitor
    {
        private readonly string _keyPath;
        private readonly RegistryHive _hive;
        private Thread? _thread;
        private bool _running;

        public event Action? RegChanged;

        public RegistryMonitor(RegistryHive hive, string keyPath)
        {
            _hive = hive;
            _keyPath = keyPath;
        }

        public void Start()
        {
            _running = true;
            _thread = new Thread(() =>
            {
                using var key = RegistryKey.OpenBaseKey(_hive, RegistryView.Default)
                    .OpenSubKey(_keyPath, writable: false);

                var notifyEvent = new AutoResetEvent(false);
                while (_running)
                {
                    Microsoft.Win32.SafeHandles.SafeRegistryHandle handle = key.Handle;
                    NativeMethods.RegNotifyChangeKeyValue(
                        handle,
                        true,
                        NativeMethods.REG_NOTIFY_CHANGE_NAME | NativeMethods.REG_NOTIFY_CHANGE_LAST_SET,
                        notifyEvent.SafeWaitHandle,
                        true);

                    notifyEvent.WaitOne();
                    RegChanged?.Invoke();
                }
            });
            _thread.IsBackground = true;
            _thread.Start();
        }

        public void Stop() => _running = false;

        private static class NativeMethods
        {
            public const int REG_NOTIFY_CHANGE_NAME = 0x1;
            public const int REG_NOTIFY_CHANGE_LAST_SET = 0x4;

            [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegNotifyChangeKeyValue(
                Microsoft.Win32.SafeHandles.SafeRegistryHandle hKey,
                bool bWatchSubtree,
                int dwNotifyFilter,
                Microsoft.Win32.SafeHandles.SafeWaitHandle hEvent,
                bool fAsynchronous);
        }
    }
}
