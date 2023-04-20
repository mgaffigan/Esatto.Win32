using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public enum WinEvent : uint
    {
        EVENT_MIN = 0x00000001,
        EVENT_MAX = 0x7FFFFFFF,
        EVENT_SYSTEM_FOREGROUND = 0x0003,
        EVENT_SYSTEM_MOVESIZESTART = 0x000a,
        EVENT_SYSTEM_MOVESIZEEND = 0x000b,
        EVENT_SYSTEM_MINIMIZEEND = 0x0017,
        EVENT_SYSTEM_MINIMIZESTART = 0x0016,
        EVENT_OBJECT_CREATE = 0x8000,
        EVENT_OBJECT_DESTROY = 0x8001,
        EVENT_OBJECT_SHOW = 0x8002,
        EVENT_OBJECT_HIDE = 0x8003,
        EVENT_OBJECT_REORDER = 0x8004,
        EVENT_OBJECT_FOCUS = 0x8005,
        EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
        EVENT_OBJECT_DRAGSTART = 0x8021,
        EVENT_OBJECT_DRAGCANCEL = 0x8022,
        EVENT_OBJECT_DRAGCOMPLETE = 0x8023
    }
}