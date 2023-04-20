using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Printing
{
    /// <summary>
    /// Represents a print job in a spooler queue
    /// </summary>
    public sealed class GdiPrintJob : IDisposable
    {
        SafePrinterHandle PrinterHandle;

        /// <summary>
        /// The ID assigned by the print spooler to identify the job
        /// </summary>
        public UInt32 PrintJobID { get; private set; }

        private bool isFinished = false;

        /// <summary>
        /// Create a print job with a enumerated datatype
        /// </summary>
        /// <param name="PrinterName"></param>
        /// <param name="dataType"></param>
        /// <param name="jobName"></param>
        /// <param name="outputFileName"></param>
        public GdiPrintJob(string PrinterName, GdiPrintJobDataType dataType, string jobName, string outputFileName)
            : this(PrinterName, translateType(dataType), jobName, outputFileName)
        {
        }

        /// <summary>
        /// Create a print job with a string datatype
        /// </summary>
        /// <param name="PrinterName"></param>
        /// <param name="dataType"></param>
        /// <param name="jobName"></param>
        /// <param name="outputFileName"></param>
        public GdiPrintJob(string PrinterName, string dataType, string jobName, string outputFileName)
        {
            if (String.IsNullOrEmpty(PrinterName))
            {
                throw new ArgumentException("Contract assertion not met: !String.IsNullOrEmpty(PrinterName)", nameof(PrinterName));
            }
            if (String.IsNullOrEmpty(dataType))
            {
                throw new ArgumentException("Contract assertion not met: !String.IsNullOrEmpty(dataType)", nameof(dataType));
            }

            SafePrinterHandle hPrinter;
            if (!NativeMethods.OpenPrinter(PrinterName, out hPrinter, IntPtr.Zero))
            {
                throw new Win32Exception();
            }
            this.PrinterHandle = hPrinter;

            NativeMethods.DOC_INFO_1 docInfo = new NativeMethods.DOC_INFO_1() 
            {
                DocName = jobName,
                Datatype = dataType,
                OutputFile = outputFileName
            };
            IntPtr pDocInfo = Marshal.AllocHGlobal(Marshal.SizeOf(docInfo));
            try
            {
                Marshal.StructureToPtr(docInfo, pDocInfo, false);
                UInt32 docid = NativeMethods.StartDocPrinter(hPrinter, 1, pDocInfo);
                if (docid == 0)
                {
                    throw new Win32Exception();
                }
                this.PrintJobID = docid;
            }
            finally
            {
                Marshal.FreeHGlobal(pDocInfo);
            }
        }

        public void Dispose()
        {
            if (isFinished)
                return;
            isFinished = true;

            if (!NativeMethods.EndDocPrinter(this.PrinterHandle))
            {
                throw new Win32Exception();
            }

            // for some reason the printerhandle is disposed elsewhere.
            PrinterHandle.Dispose();
        }

        /// <summary>
        /// Write the data of a single page or a precomposed PCL document.  
        /// Disposes the stream when complete.
        /// </summary>
        /// <param name="data"></param>
        public void WritePage(Stream data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Contract assertion not met: data != null");
            }
            if (!(data.CanRead))
            {
                throw new ArgumentException("Contract assertion not met: data.CanRead", nameof(data));
            }

            // we own the stream once we have it
            using (data)
            {
                if (!NativeMethods.StartPagePrinter(this.PrinterHandle))
                {
                    throw new Win32Exception();
                }

                byte[] buffer = new byte[81920 /* 80k is Stream.CopyTo default */];
                uint read = 1;
                while ((read = (uint)data.Read(buffer, 0, buffer.Length)) != 0)
                {
                    UInt32 written;
                    if (!NativeMethods.WritePrinter(this.PrinterHandle, buffer, read, out written))
                    {
                        throw new Win32Exception();
                    }

                    if (written != read)
                    {
                        throw new InvalidOperationException("Error while writing to printer");
                    }
                }

                if (!NativeMethods.EndPagePrinter(this.PrinterHandle))
                {
                    throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Complete the current job (same as dispose)
        /// </summary>
        public void CompleteJob()
        {
            Dispose();
        }

        #region datatypes

        private readonly static string[] dataTypes = new string[] 
        { 
            // 0
            null, 
            "RAW", 
            // 2
            "RAW [FF appended]",
            "RAW [FF auto]",
            // 4
            "NT EMF 1.003", 
            "NT EMF 1.006",
            // 6
            "NT EMF 1.007", 
            "NT EMF 1.008", 
            // 8
            "TEXT", 
            "XPS_PASS", 
            // 10
            "XPS2GDI" 
        };

        private static string translateType(GdiPrintJobDataType type)
        {
            return dataTypes[(int)type];
        }

        #endregion
    }

    public enum GdiPrintJobDataType 
    {
        Unknown = 0,
        Raw = 1,
        RawAppendFF = 2,
        RawAuto = 3,
        NtEmf1003 = 4,
        NtEmf1006 = 5,
        NtEmf1007 = 6,
        NtEmf1008 = 7,
        Text = 8,
        XpsPass = 9,
        Xps2Gdi = 10
    }
}
