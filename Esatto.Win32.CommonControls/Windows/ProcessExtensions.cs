using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public static class ProcessExtensions
    {
        public static void AllowSetForegroundWindow(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process), "Contract assertion not met: process != null");
            }

            if (!NativeMethods.AllowSetForegroundWindow(process.Id))
            {
                throw new Win32Exception();
            }
        }
    }
}
