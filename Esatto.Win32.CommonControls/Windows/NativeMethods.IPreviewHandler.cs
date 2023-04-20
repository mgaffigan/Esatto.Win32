using Esatto.Win32.Com;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Esatto.Win32.Windows.NativeMethods;

namespace Esatto.Win32.Windows
{
    internal static partial class NativeMethods
    {
        const string Shellwapi = "Shlwapi.dll";

        public const int
            ASSOCF_INIT_DEFAULTTOSTAR = 0x00000004,
            ASSOCF_NOTRUNCATE = 0x00000020,
            ASSOCSTR_SHELLEXTENSION = 16;

        [DllImport(Shellwapi, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void AssocQueryString(int flags, int str,
            [MarshalAs(UnmanagedType.LPWStr)] string pszAssoc,
            [MarshalAs(UnmanagedType.LPWStr)] string pszExtra,
            IntPtr pszOut, ref int pcchOut);

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
        public interface IPreviewHandler
        {
            void SetWindow(IntPtr hwnd, ref Rectangle rect);
            void SetRect(ref Rectangle rect);
            void DoPreview();
            void Unload();
            void SetFocus();
            void QueryFocus(out IntPtr phwnd);
            [PreserveSig]
            uint TranslateAccelerator(ref Message pmsg);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
        public interface IInitializeWithFile
        {
            void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
        public interface IInitializeWithStream
        {
            void Initialize(IStream pstream, uint grfMode);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7F73BE3F-FB79-493C-A6C7-7EE14E245841")]
        public interface IInitializeWithItem
        {
            void Initialize(IShellItem psi, uint grfMode);
        }
    }

    internal static class IPreviewHandlerExtensions
    {
        public static void Initialize(this IPreviewHandler previewHandler, string filePath, out Stream openedStream)
        {
            // File
            {
                var iwf = previewHandler as IInitializeWithFile;
                if (iwf != null)
                {
                    iwf.Initialize(filePath, 0);
                    openedStream = null;
                    return;
                }
            }

            // Stream
            {
                var iws = previewHandler as IInitializeWithStream;
                if (iws != null)
                {
                    openedStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    try
                    {
                        iws.Initialize(openedStream.AsIStream(), 0);
                    }
                    catch
                    {
                        openedStream.Dispose();
                        openedStream = null;
                        throw;
                    }
                    openedStream = null;
                    return;
                }
            }

            // Item
            {
                var iwi = previewHandler as IInitializeWithItem;
                if (iwi != null)
                {
                    var item = (IShellItem)SHCreateItemFromParsingName(filePath, null, typeof(IShellItem).GUID);
                    iwi.Initialize(item, 0);
                    openedStream = null;
                    return;
                }
            }

            throw new NotSupportedException("Unknown initializer for IPreviewHandler");
        }
    }
}
