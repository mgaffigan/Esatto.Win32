#pragma warning disable CA1707 // Identifiers should not contain underscores: PInvoke keeping the name of native type

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    public enum WTS_CHANNEL_OPTION : uint
    {
        DYNAMIC = 0x00000001,
        DYNAMIC_PRI_LOW = 0x00000000,
        DYNAMIC_PRI_MED = 0x00000002,
        DYNAMIC_PRI_HIGH = 0x00000004,
        DYNAMIC_PRI_REAL = 0x00000006,
        DYNAMIC_NO_COMPRESS = 0x00000008,
    }
}
