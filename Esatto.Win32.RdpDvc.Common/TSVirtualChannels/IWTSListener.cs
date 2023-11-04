using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230206-9a39-4d58-8674-cdb4dff4e73b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSListener
    {
        // returns IPropertyBag
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object GetConfiguration();
    }
}
