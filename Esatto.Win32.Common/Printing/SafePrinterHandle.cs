using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.Printing
{
    internal sealed class SafePrinterHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafePrinterHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.ClosePrinter(handle);
        }
    }
}
