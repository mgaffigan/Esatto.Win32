using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Wpf
{
    static class NativeMethods
    {
        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern bool DrawMenuBar(IntPtr hMenu);

        public const uint MF_BYCOMMAND = 0x00000000;
        public const uint MF_GRAYED = 0x00000001;
        public const uint MF_ENABLED = 0x00000000;

        public const uint SC_CLOSE = 0xF060;

        public const int WM_SHOWWINDOW = 0x00000018;
        public const int WM_CLOSE = 0x10;
    }
}
