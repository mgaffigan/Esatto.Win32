using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Esatto.Win32.CommonControls.PnP;

namespace Esatto.Win32.CommonControls
{
    public class DetectedPort
    {
        public string PortName { get; internal set; }

        public string DeviceName { get; internal set; }

        public string HardwareID { get; internal set; }

        public static IEnumerable<DetectedPort> GetAllPorts()
        {
            return NativeMethods.EnumeratePorts();
        }
    }
}
