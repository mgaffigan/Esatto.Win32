using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    [ComImport, Guid("A1230201-1439-4e62-a414-190d0ac3d40e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IWTSPlugin
    {
        /*
         *  Called immediately after instantiating the COM class
         */
        void Initialize(IWTSVirtualChannelManager pChannelMgr);

        /*
         *  Called when the TS client is connected to the TS server
         */
        void Connected();

        /*
         *  Called when the TS client is disconnected to the TS server
         *  Might be followed by another Connected() call
         */
        void Disconnected(uint dwDisconnectCode);

        /*
         *  The last method called by the TS client before 
         *  terminating the object
         */
        void Terminated();
    }
}
