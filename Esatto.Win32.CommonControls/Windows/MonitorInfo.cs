using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static Esatto.Win32.Windows.NativeMethods;

namespace Esatto.Win32.Windows
{
    public sealed class MonitorInfo
    {
        public Rect ViewportBounds { get; }

        public Rect WorkAreaBounds { get; }

        public bool IsPrimary { get; }

        public string DeviceId { get; }

        internal MonitorInfo(MONITORINFOEX mex)
        {
            this.ViewportBounds = (Rect)mex.rcMonitor;
            this.WorkAreaBounds = (Rect)mex.rcWork;
            this.IsPrimary = mex.dwFlags.HasFlag(MONITORINFOF.PRIMARY);
            this.DeviceId = mex.szDevice;
        }

        public static IEnumerable<MonitorInfo> GetAllMonitors()
        {
            var monitors = new List<MonitorInfo>();
            MonitorEnumDelegate callback = delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                MONITORINFOEX mi = new MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                if (!GetMonitorInfo(hMonitor, ref mi))
                {
                    throw new Win32Exception();
                }

                monitors.Add(new MonitorInfo(mi));
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

            return monitors;
        }
    }
}
