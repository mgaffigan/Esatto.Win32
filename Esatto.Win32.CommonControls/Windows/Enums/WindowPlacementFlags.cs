using System;

namespace Esatto.Win32.Windows
{
    [Flags]
    public enum WindowPlacementFlags : uint
    {
        None = 0,
        SetMinPosition = 0x0001,
        RestoreToMaximized = 0x0002,
        AsyncWindowPlacement = 0x0004,
    }
}
