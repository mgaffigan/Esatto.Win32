using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230205-d6a7-11d8-b9fd-000bdbd1f198"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSVirtualChannelManager
    {
        IWTSListener CreateListener(
            // this is always ASCII
            [MarshalAs(UnmanagedType.LPStr)] string pszChannelName,
            int uFlags,
            IWTSListenerCallback pListenerCallback
        );
    }
}
