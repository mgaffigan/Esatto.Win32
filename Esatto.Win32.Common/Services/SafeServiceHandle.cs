using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esatto.Win32.Services
{
#if ESATTO_WIN32
    public
#else
    internal
#endif
        sealed class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeServiceHandle()
            : base(true)
        {
        }

        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }
    }
}
