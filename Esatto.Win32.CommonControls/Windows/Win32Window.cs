using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Esatto.Win32.Windows
{
    public sealed class Win32Window : IEquatable<Win32Window>
    {
        public IntPtr Handle { get; }

        private readonly Lazy<string> _CachedClass;
        public string CachedClass => _CachedClass.Value;

        private readonly Lazy<string> _CachedText;
        public string CachedName => _CachedText.Value;

        public Win32Window(IntPtr hWnd)
        {
            this.Handle = hWnd;
            this._CachedClass = new Lazy<string>(GetClassNameOrDefault);
            this._CachedText = new Lazy<string>(GetWindowText);
        }

        public static Win32Window GetForegroundWindow()
        {
            return new Win32Window(NativeMethods.GetForegroundWindow());
        }

        public static Win32Window GetActiveWindow()
        {
            return new Win32Window(NativeMethods.GetActiveWindow());
        }

        public WindowPlacement GetWindowPlacement()
        {
            var wp = NativeMethods.GetWindowPlacement(this.Handle);
            return new WindowPlacement(wp);
        }

        public void SetWindowPlacement(WindowPlacement placement)
        {
            NativeMethods.SetWindowPlacement(this.Handle, placement);
        }

        public Win32Window GetActiveMdiChild()
        {
            return new Win32Window(NativeMethods.MdiGetActive(Handle));
        }

        public Win32Menu GetMenu()
        {
            var hMenu = NativeMethods.GetMenu(this.Handle);
            if (hMenu == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
            return new Win32Menu(hMenu);
        }

        public void InvokeMenuItem(params string[] menus)
        {
            var menu = GetMenu().GetPath(menus) as Win32MenuItem;
            if (menu == null)
            {
                throw new InvalidOperationException("Menu item is not a command");
            }
            menu.Invoke(this);
        }

        public Point ScreenToClient(Point screen)
        {
            var pt = new System.Drawing.Point((int)screen.X, (int)screen.Y);
            if (!NativeMethods.ScreenToClient(Handle, ref pt))
            {
                throw new Win32Exception();
            }
            return new Point(pt.X, pt.Y);
        }

        public Point TransformTo(Win32Window other, Point client)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            return NativeMethods.MapWindowPoint(Handle, other.Handle, client);
        }

        public Rect TransformTo(Win32Window other, Rect client)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            var topLeft = TransformTo(other, client.Location);
            return new Rect(topLeft, client.Size);
        }

        public IEnumerable<Win32Window> GetChildWindows() => NativeMethods.EnumChildWindows(this.Handle).Select(hWnd => new Win32Window(hWnd));

        public Win32Window FindChildOrDefault(Predicate<Win32Window> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate), "Contract assertion not met: predicate != null");
            }

            Win32Window findResult = null;
            NativeMethods.FirstChildWindow(this.Handle, hwnd =>
            {
                var child = new Win32Window(hwnd);
                if (predicate(child))
                {
                    findResult = child;
                    return true;
                }

                return false;
            });

            return findResult;
        }

        public int GetThreadId()
        {
            uint unused_procID;
            return (int)NativeMethods.GetWindowThreadProcessId(this.Handle, out unused_procID);
        }

        public Win32Window FindChild(Predicate<Win32Window> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate), "Contract assertion not met: predicate != null");
            }

            var findResult = FindChildOrDefault(predicate);
            if (findResult == null)
            {
                throw new InvalidOperationException("No child window found matching predicate");
            }
            return findResult;
        }

        public static Win32Window Find(Process process, Predicate<Win32Window> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate), "Contract assertion not met: predicate != null");
            }

            Win32Window findResult = null;
            NativeMethods.FirstChildWindow(process, hwnd =>
            {
                var child = new Win32Window(hwnd);
                if (predicate(child))
                {
                    findResult = child;
                    return true;
                }

                return false;
            });

            if (findResult == null)
            {
                throw new InvalidOperationException("No child window found matching predicate");
            }
            return findResult;
        }

        public Rect GetBounds() => NativeMethods.GetWindowRect(this.Handle);

        public Rect GetBoundsWithoutShadow()
        {
            NativeMethods.DwmGetWindowAttribute(this.Handle, 9 /* DWMWA_EXTENDED_FRAME_BOUNDS */,
                out var bounds, Marshal.SizeOf<NativeMethods.RECT>());
            return (Rect)bounds;
        }

        public (Rect bounds, Thickness shadowThickness) GetBoundsWithShadowThickness()
        {
            var frameBounds = GetBoundsWithoutShadow();
            var clientBounds = GetBounds();
            var thickness = new Thickness(
                frameBounds.Left - clientBounds.Left,
                frameBounds.Top - clientBounds.Top,
                clientBounds.Right - frameBounds.Right,
                clientBounds.Bottom - frameBounds.Bottom);
            return (clientBounds, thickness);
        }

        public void SetBounds(Rect bounds) => NativeMethods.SetWindowPos(this.Handle, 0,
            (int)bounds.Left, (int)bounds.Top, (int)bounds.Width, (int)bounds.Height,
            NativeMethods.SWP_NOZORDER);

        public bool Show() => NativeMethods.ShowWindow(this.Handle, ShowWindowCommand.Show);

        public bool Minimize() => NativeMethods.ShowWindow(this.Handle, ShowWindowCommand.Minimize);

        public bool Maximize() => NativeMethods.ShowWindow(this.Handle, ShowWindowCommand.Maximize);

        public bool Restore() => NativeMethods.ShowWindow(this.Handle, ShowWindowCommand.Restore);

        public bool Hide() => NativeMethods.ShowWindow(this.Handle, ShowWindowCommand.Hide);

        public WindowStyles GetWindowStyle() => (WindowStyles)NativeMethods.GetWindowLong(this.Handle, NativeMethods.GWL_STYLE);

        public string GetClassName() => NativeMethods.GetClassName(this.Handle);

        public string GetClassNameOrDefault() => NativeMethods.GetClassName(this.Handle, throwOnError: false);

        public string GetWindowText() => NativeMethods.GetWindowText(this.Handle);

        public bool SetWindowText(string text) => NativeMethods.SetWindowText(this.Handle, text);

        public string WMGetText() => NativeMethods.WMGetText(this.Handle);

        public Win32Window GetParent() => new Win32Window(NativeMethods.GetParent(this.Handle));

        public void SetParent(Win32Window hwndNewParent)
        {
            _ = NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWLParameter.GWL_HWNDPARENT, hwndNewParent.Handle.ToInt32());
        }

        public Process GetProcess()
        {
            uint procID;
            _ = NativeMethods.GetWindowThreadProcessId(this.Handle, out procID);
            return Process.GetProcessById((int)procID);
        }

        public bool GetIsShown()
        {
            var windowPlacement = NativeMethods.GetWindowPlacement(this.Handle);
            return windowPlacement.ShowCmd != ShowWindowCommand.Hide;
        }

        public bool GetIsMaximized()
        {
            var windowPlacement = NativeMethods.GetWindowPlacement(this.Handle);
            return windowPlacement.ShowCmd == ShowWindowCommand.Maximize
                || windowPlacement.ShowCmd == ShowWindowCommand.ShowMaximized;
        }

        public bool GetIsVisible()
        {
            return NativeMethods.IsWindowVisible(this.Handle);
        }

        public void Ping(TimeSpan timeSpan)
        {
            ulong totalMilliseconds;
            if (timeSpan == Timeout.InfiniteTimeSpan
                || ((uint)timeSpan.TotalMilliseconds) > uint.MaxValue)
            {
                totalMilliseconds = uint.MaxValue;
            }
            else if (timeSpan.TotalMilliseconds <= 0)
            {
                totalMilliseconds = 5000;
            }
            else
            {
                totalMilliseconds = (uint)timeSpan.TotalMilliseconds;
            }

            NativeMethods.PingWindow(this.Handle, checked((uint)totalMilliseconds));
        }

        #region Equals

        public override string ToString() => $"HWND {Handle.ToString("x8")} class '{CachedClass}': '{CachedName}'";

        public override bool Equals(object obj) => this.Equals(obj as Win32Window);

        public bool Equals(Win32Window other) => other?.Handle == this.Handle;

        public override int GetHashCode() => Handle.GetHashCode();

        public static bool operator ==(Win32Window a, Win32Window b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (ReferenceEquals(a, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Win32Window a, Win32Window b)
        {
            return !(a == b);
        }

        #endregion Equals
    }
}