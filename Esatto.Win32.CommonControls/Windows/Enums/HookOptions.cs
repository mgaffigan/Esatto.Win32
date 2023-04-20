using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    [Flags]
    public enum HookOptions
    {
        None = 0,
        StartDisabled = 1,
        AsyncCallbacks = 2
    }
}
