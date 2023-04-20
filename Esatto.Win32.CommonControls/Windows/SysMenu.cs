using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Esatto.Win32.Windows
{
    public class SysMenu
    {
        private readonly Window Window;
        private readonly List<MenuItem> Items;
        private bool isInitialized;
        private IntPtr NextID = (IntPtr)1000;
        private int StartPosition = 5;

        public SysMenu(Window window)
        {
            this.Items = new List<MenuItem>();
            this.Window = window ?? throw new ArgumentNullException(nameof(window));
            this.Window.SourceInitialized += this.Window_SourceInitialized;
        }

        class MenuItem
        {
            public IntPtr ID;
            public string Text;
            public Action OnClick;
        }

        public void AddSysMenuItem(string text, Action onClick)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (onClick == null)
            {
                throw new ArgumentNullException(nameof(onClick));
            }

            var thisId = NextID;
            NextID += 1;

            var newItem = new MenuItem()
            {
                ID = thisId,
                Text = text,
                OnClick = onClick
            };
            Items.Add(newItem);
            var thisPosition = StartPosition + Items.Count;

            if (isInitialized)
            {
                var hwndSource = PresentationSource.FromVisual(Window) as HwndSource;
                if (hwndSource == null)
                {
                    return;
                }
                var hSysMenu = GetSystemMenu(hwndSource.Handle, false);
                InsertMenu(hSysMenu, thisPosition, MF_BYPOSITION, thisId, text);
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(Window) as HwndSource;
            if (hwndSource == null)
            {
                return;
            }

            hwndSource.AddHook(WndProc);

            var hSysMenu = GetSystemMenu(hwndSource.Handle, false);

            /// Create our new System Menu items just before the Close menu item
            InsertMenu(hSysMenu, StartPosition, MF_BYPOSITION | MF_SEPARATOR, IntPtr.Zero, string.Empty);
            int pos = StartPosition + 1;
            foreach (var item in Items)
            {
                InsertMenu(hSysMenu, pos, MF_BYPOSITION, item.ID, item.Text);
                pos += 1;
            }

            isInitialized = true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SYSCOMMAND)
            {
                var item = Items.FirstOrDefault(d => d.ID == wParam);
                if (item != null)
                {
                    item.OnClick();
                    handled = true;
                    return IntPtr.Zero;
                }
            }

            return IntPtr.Zero;
        }

        #region Win32

        private const Int32 WM_SYSCOMMAND = 0x112;
        private const Int32 MF_SEPARATOR = 0x800;
        private const Int32 MF_BYPOSITION = 0x400;
        private const Int32 MF_STRING = 0x0;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool InsertMenu(IntPtr hMenu, int wPosition, int wFlags, IntPtr wIDNewItem, string lpNewItem);

        #endregion
    }
}
