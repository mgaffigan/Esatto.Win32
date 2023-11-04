#pragma warning disable CA1707 // Identifiers should not contain underscores: PInvoke keeping the name of native type

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Esatto.Win32.RdpDvc.TSVirtualChannels.NativeMethods;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    internal sealed class WtsVitrualChannelSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public WtsVitrualChannelSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return WTSVirtualChannelClose(handle);
        }
    }
}
