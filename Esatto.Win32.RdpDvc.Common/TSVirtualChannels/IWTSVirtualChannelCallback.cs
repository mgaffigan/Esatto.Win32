using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230204-d6a7-11d8-b9fd-000bdbd1f198"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSVirtualChannelCallback
    {
        /*
         *  Called whenever a full message from the server is received
         *  The message is fully reassembled and has the exact size
         *  as the Write() call on the server
         */
        void OnDataReceived(int cbSize, IntPtr pBuffer);

        /*
         *  The channel is disconnected, all Write() calls will fail
         *  no more incomming data is expected. 
         */
        void OnClose();
    }
}
