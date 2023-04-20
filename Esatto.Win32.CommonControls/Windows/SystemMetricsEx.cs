using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public static class SystemMetricsEx
    {
        public static bool IsSessionEnding => NativeMethods.GetSystemMetrics(NativeMethods.SM_SHUTTINGDOWN) != 0;
    }
}
