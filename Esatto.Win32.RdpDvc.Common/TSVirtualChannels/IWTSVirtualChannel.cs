using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230207-d6a7-11d8-b9fd-000bdbd1f198"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSVirtualChannel
    {
        /*
         *  The plugin requests to send data with specific size
         */
        void Write(
            uint cbSize,
            IntPtr pBuffer,
            IntPtr pReserved          // must be NULL
        );

        /*
         *  The plugin requests to close the channel
         *  This will result in TSVirtualChannelCallback::OnClose() call.
         *  All I/O will cease.
         */
        void Close();
    }
}
