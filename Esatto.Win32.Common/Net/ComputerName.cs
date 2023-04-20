using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esatto.Win32.CommonControls.Net
{
#if ESATTO_WIN32
    public
#else
    internal
#endif
        static class ComputerName
    {
        /// <summary>
        /// Fully qualified name as returned from GetComputerNameEx(DnsFullyQualified)
        /// </summary>
        public static string DnsFullyQualified
        {
            get
            {
                return NativeMethods.GetComputerName(
                    NativeMethods.ComputerNameFormat.DnsFullyQualified);
            }
        }
    }
}
