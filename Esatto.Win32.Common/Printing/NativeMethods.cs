using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Printing
{
    internal static class NativeMethods
    {
        #region Constants

        internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

        #endregion

        #region winspool.drv

        private const string Winspool = "winspool.drv";

#if ESATTO_WIN32_COMMON
        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool OpenPrinter(string szPrinter, out SafePrinterHandle hPrinter, IntPtr pd);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint StartDocPrinter(SafePrinterHandle hPrinter, int level, IntPtr di);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool EndDocPrinter(SafePrinterHandle hPrinter);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool StartPagePrinter(SafePrinterHandle hPrinter);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool EndPagePrinter(SafePrinterHandle hPrinter);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool WritePrinter(
            // 0
            SafePrinterHandle hPrinter,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBytes,
            // 2
            uint dwCount,
            out uint dwWritten);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GetJob(SafePrinterHandle hPrinter, int JobId, int Level, IntPtr pJob, int cbBuf, out int pcbNeeded);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool AddMonitor(string portName, uint level, ref MONITOR_INFO_2 monitors);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool DeleteMonitor(string serverName, string pEnvironment, string pMonitorName);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool EnumMonitors(string pName, uint level, IntPtr pMonitorBuf, int cbMonitorBuf, out int pcbNeeded, out int pcReturned);

        public const int
            UPDP_SILENT_UPLOAD = 1,
            UPDP_UPLOAD_ALWAYS = 2,
            IPDFP_COPY_ALL_FILES = 1;

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void UploadPrinterDriverPackage(string serverName, string infPath, string environment,
            int flags, IntPtr hwnd, IntPtr pDestInfPath, ref int cchDestInfPath);

        [DllImport(Winspool, SetLastError = true, CharSet = CharSet.Unicode, PreserveSig = false)]
        internal static extern void InstallPrinterDriverFromPackage(string serverName, string infPath, string driverName,
            string environment, int flags);
#endif

        #endregion

        #region Kernel32

        private const string Kernel32 = "kernel32.dll";

        [DllImport(Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport(Kernel32, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern bool GlobalUnlock(IntPtr handle);

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        internal struct DOC_INFO_1
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string DocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string OutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Datatype;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;

            public DateTime ToDateTime(DateTimeKind kind)
            {
                return new DateTime(wYear, wMonth, wDay, wHour, wMinute, wSecond, wMilliseconds, kind);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITOR_INFO_2
        {
            public string pName;
            public string pEnvironment;
            public string pDLLName;
        }

        #endregion
    }
}
