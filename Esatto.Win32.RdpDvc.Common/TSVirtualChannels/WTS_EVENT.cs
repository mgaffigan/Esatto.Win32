#pragma warning disable CA1707 // Identifiers should not contain underscores: PInvoke keeping the name of native type

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    public enum WTS_EVENT : uint
    {
        WTS_CONSOLE_CONNECT = 1,
        WTS_CONSOLE_DISCONNECT = 2,
        WTS_REMOTE_CONNECT = 3,
        WTS_REMOTE_DISCONNECT = 4,
        WTS_SESSION_LOGON = 5,
        WTS_SESSION_LOGOFF = 6,
        WTS_SESSION_LOCK = 7,
        WTS_SESSION_UNLOCK = 8,
        WTS_SESSION_REMOTE_CONTROL = 9,
        WTS_SESSION_CREATE = 10,
        WTS_SESSION_TERMINATE = 11,
    }
}
