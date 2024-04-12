using System;
using System.Collections.Generic;
using System.Linq;

namespace Esatto.Win32.Windows
{
    public class Win32MenuBase
    {
        public nint Handle { get; }

        public string Text { get; }

        public Win32MenuBase(nint hMenu, string text)
        {
            this.Handle = hMenu;
            this.Text = text;
        }

        public override string ToString() => Text;
    }

    public class Win32Menu : Win32MenuBase
    {
        public Win32Menu(nint hMenu) 
            : this(hMenu, "")
        {
            // nop
        }

        public Win32Menu(nint hMenu, string text)
            : base(hMenu, text)
        {
            // nop
        }

        public IEnumerable<Win32MenuBase> Items
        {
            get
            {
                var count = NativeMethods.GetMenuItemCount(Handle);
                for (int i = 0; i < count; i++)
                {
                    var text = NativeMethods.GetMenuString(Handle, i, NativeMethods.MF_BYPOSITION);
                    var menu = NativeMethods.GetSubMenu(Handle, i);
                    yield return menu == 0 ? new Win32MenuItem(Handle, i, text) : new Win32Menu(menu, text);
                }
            }
        }

        public Win32MenuBase GetPath(params string[] names)
        {
            Win32Menu c = this;
            foreach (var elem in names)
            {
                var match = c.Items.FirstOrDefault(x => x.Text == elem) 
                    ?? throw new KeyNotFoundException($"Could not find menu item '{elem}' in menu '{c.Text}'");

                // If we hit a leaf node, return it
                if (match is Win32MenuItem item) return item;

                // Otherwise it better be a submenu
                c = (Win32Menu)match;
            }
            return c;
        }
    }

    public class Win32MenuItem : Win32Menu
    {
        public nint HMenu { get; }
        public int Index { get; }

        public Win32MenuItem(nint handle, int i, string text)
            : base(handle, text)
        {
            this.HMenu = handle;
            this.Index = i;
        }

        public void Invoke(Win32Window targetHwnd)
        {
            var itemId = NativeMethods.GetMenuItemID(HMenu, Index);
            if (itemId == -1) throw new InvalidOperationException(Text + " is not a command item.");
            NativeMethods.SendMessage(targetHwnd.Handle, NativeMethods.WM_COMMAND, itemId, IntPtr.Zero);
        }
    }
}
