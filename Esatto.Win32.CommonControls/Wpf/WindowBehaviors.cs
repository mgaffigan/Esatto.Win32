using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using static Esatto.Win32.Wpf.NativeMethods;

namespace Esatto.Win32.Wpf
{
    public class NonClosableWindow : Window
    {
        private bool isWindowShown;

        public bool CanClose
        {
            get { return (bool)GetValue(CanCloseProperty); }
            set { SetValue(CanCloseProperty, value); }
        }

        public static readonly DependencyProperty CanCloseProperty =
            DependencyProperty.Register("CanClose", typeof(bool),
                typeof(NonClosableWindow), new PropertyMetadata(true, CanClose_Changed));

        private static void CanClose_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var @this = (NonClosableWindow)d;
            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;

            if (!@this.isWindowShown)
            {
                // no-op, show will be set in WM_SHOWWINDOW
            }
            else
            {
                @this.SetCloseButtonState(newValue);
            }
        }

        private void SetCloseButtonState(bool canClose)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            IntPtr hMenu = GetSystemMenu(hwndSource.Handle, false);
            if (hMenu == IntPtr.Zero)
            {
                throw new Win32Exception("GetSystemMenu returned null");
            }

            EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | (canClose ? MF_ENABLED : MF_GRAYED));
            if (isWindowShown)
            {
                DrawMenuBar(hwndSource.Handle);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null)
            {
                hwndSource.AddHook(Window_WndProc);
            }
        }

        private IntPtr Window_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                if (!CanClose)
                {
                    SetCloseButtonState(false);
                }

                this.isWindowShown = true;
            }

            return IntPtr.Zero;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!CanClose)
            {
                e.Cancel = true;
            }
        }
    }
}
