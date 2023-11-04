using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Esatto.Win32.RdpDvc.TSVirtualChannels.NativeMethods;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    public sealed class WtsAllocSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public WtsAllocSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            WTSFreeMemory(handle);
            return true;
        }
    }
}
