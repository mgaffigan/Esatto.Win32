using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    using static NativeMethods;

    [ComImport, ComVisible(false)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(IID_IClassFactory)]
#if ESATTO_WIN32
    public
#else
    internal
#endif
        interface IClassFactory
    {
        IntPtr CreateInstance(IntPtr pUnkOuter, [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        void LockServer(bool fLock);
    }
}
