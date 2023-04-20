using Esatto.Win32.CommonControls.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Net
{
    public static class NetworkDrives
    {
        public static void MapNetworkDrive(string uncPath, string localName, string username, string password)
            => NativeMethods.MapNetworkDrive(uncPath, localName, username, password);
    }
}
