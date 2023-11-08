using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Esatto.Win32.Windows.NativeMethods;

namespace Esatto.Win32.Windows
{
    public static class OpenWithDialog
    {
        public static void Show(IntPtr hwndParent, Uri url)
        {
            var oas = new OPENASINFO();
            oas.pcszFile = url.ToString();
            oas.oaifInFlags = OPEN_AS_INFO_FLAGS.HIDE_REGISTRATION
                | OPEN_AS_INFO_FLAGS.EXEC
                | OPEN_AS_INFO_FLAGS.URL_PROTOCOL;
            SHOpenWithDialog(hwndParent, oas);
        }

        public static void Show(IntPtr hwndParent, string path)
        {
            var oas = new OPENASINFO();
            oas.pcszFile = path;
            oas.oaifInFlags = OPEN_AS_INFO_FLAGS.HIDE_REGISTRATION
                | OPEN_AS_INFO_FLAGS.EXEC;
            SHOpenWithDialog(hwndParent, oas);
        }
    }
}
