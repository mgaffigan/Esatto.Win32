using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Esatto.Win32.Printing.NativeMethods;

namespace Esatto.Win32.Printing
{
    public static class PrinterDriver
    {
        public static void InstallInf(string inf, string driverName)
        {
            int cchDest = 1024;
            string sDest;
            var pDest = Marshal.AllocHGlobal(cchDest * 2);
            try
            {
                UploadPrinterDriverPackage(null, inf, null, UPDP_SILENT_UPLOAD | UPDP_UPLOAD_ALWAYS, IntPtr.Zero, pDest, ref cchDest);
                sDest = Marshal.PtrToStringUni(pDest, cchDest);
            }
            finally
            {
                Marshal.FreeHGlobal(pDest);
            }

            InstallPrinterDriverFromPackage(null, sDest, driverName, null, IPDFP_COPY_ALL_FILES);
        }
    }
}
