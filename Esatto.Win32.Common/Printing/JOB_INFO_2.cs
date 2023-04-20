using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Printing
{
    using System.ComponentModel;
    using static NativeMethods;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
#if ESATTO_WIN32
    public
#else
    internal
#endif
        struct JOB_INFO_2
    {
        public int JobId;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPrinterName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pMachineName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pUserName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDocument;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pNotifyName;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDatatype;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pPrintProcessor;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pParameters;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pDriverName;
        public IntPtr pDevMode;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pStatus;
        public IntPtr pSecurityDescriptor;
        public int Status;
        public int Priority;
        public int Position;
        public uint StartTime;
        public uint UntilTime;
        public int TotalPages;
        public int Size;
        private SYSTEMTIME _Submitted;
        public DateTime Submitted => _Submitted.ToDateTime(DateTimeKind.Local);
        public uint Time;
        public int PagesPrinted;

        public static JOB_INFO_2 GetJobInfo(string printerName, int jobId)
        {
            if (printerName == "Spooler Simulator")
            {
                return new JOB_INFO_2();
            }

            SafePrinterHandle hPrinter;
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                throw new Win32Exception();
            }
            using (hPrinter)
            {
                int cbBuf;
                if (GetJob(hPrinter, jobId, 2, IntPtr.Zero, 0, out cbBuf))
                {
                    throw new InvalidOperationException($"GetJob succeeded on pBuf = 0");
                }
                if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception();
                }

                var pBuf = Marshal.AllocHGlobal(cbBuf);
                try
                {
                    if (!GetJob(hPrinter, jobId, 2, pBuf, cbBuf, out cbBuf))
                    {
                        throw new Win32Exception();
                    }

                    return Marshal.PtrToStructure<JOB_INFO_2>(pBuf);
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuf);
                }
            }
        }
    }
}
