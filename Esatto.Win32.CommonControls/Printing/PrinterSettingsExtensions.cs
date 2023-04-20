using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.Printing
{
    public static class PrinterSettingsExtensions
    {
        public static byte[] GetDevModeData(this PrinterSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "Contract assertion not met: settings != null");
            }

            byte[] devModeData;
            try
            {
                // cer since hDevMode is not a SafeHandle
            }
            finally
            {
                var hDevMode = settings.GetHdevmode();
                try
                {
                    IntPtr pDevMode = NativeMethods.GlobalLock(hDevMode);
                    try
                    {
                        var devMode = (NativeMethods.DEVMODE)Marshal.PtrToStructure(
                            pDevMode, typeof(NativeMethods.DEVMODE));

                        var devModeSize = devMode.dmSize + devMode.dmDriverExtra;
                        devModeData = new byte[devModeSize];
                        Marshal.Copy(pDevMode, devModeData, 0, devModeSize);
                    }
                    finally
                    {
                        NativeMethods.GlobalUnlock(hDevMode);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(hDevMode);
                }
            }
            return devModeData;
        }

        public static void SetDevModeData(this PrinterSettings settings, byte[] data)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "Contract assertion not met: settings != null");
            }
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Contract assertion not met: data != null");
            }
            if (!(data.Length >= Marshal.SizeOf(typeof(NativeMethods.DEVMODE))))
            {
                throw new ArgumentException("Contract assertion not met: data.Length >= Marshal.SizeOf(typeof(NativeMethods.DEVMODE))", nameof(data));
            }

            try
            {
                // cer since AllocHGlobal does not return SafeHandle
            }
            finally
            {
                var pDevMode = Marshal.AllocHGlobal(data.Length);
                try
                {
                    // we don't have to worry about GlobalLock since AllocHGlobal only uses LMEM_FIXED
                    Marshal.Copy(data, 0, pDevMode, data.Length);
                    var devMode = (NativeMethods.DEVMODE)Marshal.PtrToStructure(
                            pDevMode, typeof(NativeMethods.DEVMODE));

                    // The printer name must match the original printer, otherwise an AV will be thrown
                    if (!settings.PrinterName.StartsWith(devMode.dmDeviceName))
                    {
                        throw new InvalidOperationException(string.Format("Printer name "
                            + "stored in options does not match the name stored in the "
                            + "print ticket.  DEVMODE cannot be used unless the device "
                            + "name matches exactly.  \r\nOptions file: '{0}'\r\nPrint "
                            + "ticket: '{1}'", devMode.dmDeviceName, settings.PrinterName));
                    }

                    // SetHDevmode creates a copy of the devmode, so we don't have to keep ours around
                    settings.SetHdevmode(pDevMode);
                }
                finally
                {
                    Marshal.FreeHGlobal(pDevMode);
                }
            }
        }
    }
}
