using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;

namespace Esatto.Win32.Printing
{
    using static NativeMethods;

#if ESATTO_WIN32
    public
#else
    internal
#endif
        class PortMonitor
    {
        private string computerName;
        private MONITOR_INFO_2 info;

        public string Name => info.pName;
        public string DllName => info.pDLLName;

        public string DllPath => Path.Combine(Environment.SystemDirectory, DllName);

        public string EnvironmentName => info.pEnvironment;

        public string PrintServer => computerName ?? Environment.MachineName;

        internal PortMonitor(string computerName, MONITOR_INFO_2 info)
        {
            this.computerName = computerName;
            this.info = info;
        }

        public void Remove()
        {
            if (!DeleteMonitor(computerName, info.pEnvironment, info.pName))
            {
                throw new Win32Exception();
            }
        }
    }
}
