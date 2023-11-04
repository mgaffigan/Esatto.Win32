using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230203-d6a7-11d8-b9fd-000bdbd1f198"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSListenerCallback
    {
        void OnNewChannelConnection(
            IWTSVirtualChannel pChannel,
            [MarshalAs(UnmanagedType.BStr)] string data,
            [MarshalAs(UnmanagedType.Bool)] out bool pAccept,
            out IWTSVirtualChannelCallback? pCallback
        );
    }
}
