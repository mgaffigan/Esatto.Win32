#pragma warning disable CA1707 // Identifiers should not contain underscores: PInvoke keeping the name of native type

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    public enum WTS_VIRTUAL_CLASS : uint
    {
        Client = 0,
        FileHandle = 1
    }
}
