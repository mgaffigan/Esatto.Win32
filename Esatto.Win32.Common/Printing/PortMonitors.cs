using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Printing
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using static NativeMethods;

#if ESATTO_WIN32
    public
#else
    internal
#endif
        static class PortMonitors
    {
        public static IEnumerable<PortMonitor> GetPortMonitors(string computerName = null)
        {
            int cbNeeded, cReturned;
            if (EnumMonitors(computerName, 2, IntPtr.Zero, 0, out cbNeeded, out cReturned)
                || Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
            {
                throw new Win32Exception();
            }

            IntPtr pBuffer = Marshal.AllocHGlobal(cbNeeded);
            try
            {
                if (!EnumMonitors(computerName, 2, pBuffer, cbNeeded, out cbNeeded, out cReturned))
                {
                    throw new Win32Exception();
                }

                var results = new List<PortMonitor>();
                for (int i = 0; i < cReturned; i++)
                {
                    MONITOR_INFO_2 pm = Marshal.PtrToStructure<MONITOR_INFO_2>(pBuffer);
                    results.Add(new PortMonitor(computerName, pm));
                    pBuffer = pBuffer + Marshal.SizeOf<MONITOR_INFO_2>();
                }
                return results;
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }
        }

        public static PortMonitor AddPortMonitor(string portName, string dllName, string environment = null, string computerName = null)
        {
            if (string.IsNullOrEmpty(portName))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrEmpty(portName)", nameof(portName));
            }
            if (string.IsNullOrEmpty(dllName))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrEmpty(dllName)", nameof(dllName));
            }

            if (computerName == null)
            {
                var portMonPath = Path.Combine(Environment.SystemDirectory, dllName);
                if (!File.Exists(portMonPath))
                {
                    throw new FileNotFoundException($"Port dll does not exist at '{portMonPath}'", portMonPath);
                }
            }

            var info = new MONITOR_INFO_2
            {
                pName = portName,
                pDLLName = dllName,
                pEnvironment = environment
            };
            if (!AddMonitor(computerName, 2, ref info))
            {
                throw new Win32Exception();
            }

            return new PortMonitor(computerName, info);
        }

        public static void RemovePortMonitor(string portName, string environment = null, string computerName = null)
        {
            if (string.IsNullOrEmpty(portName))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrEmpty(portName)", nameof(portName));
            }

            if (!DeleteMonitor(computerName, environment, portName))
            {
                throw new Win32Exception();
            }
        }
    }
}
