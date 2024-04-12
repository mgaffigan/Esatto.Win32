using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace Esatto.Win32.Windows
{
    internal static partial class NativeMethods
    {
        private const string User32 = "user32.dll", Dwmapi = "Dwmapi.dll";
        internal const int MAX_PATH = 260;
        internal const int WM_GETTEXT = 0xD;
        internal const int WM_MDIGETACTIVE = 0x0229;
        internal const int WM_COMMAND = 0x0111;
        internal const int SM_SHUTTINGDOWN = 0x2000;

        [DllImport(User32, ExactSpelling = true, SetLastError = false)]
        public static extern int GetSystemMetrics(int index);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport(User32)]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public static IEnumerable<IntPtr> EnumChildWindows(IntPtr hWndParent)
        {
            var childWindows = new List<IntPtr>();
            EnumWindowsProc accumulator = (childHwnd, _1) =>
            {
                childWindows.Add(childHwnd);
                return true;
            };

            // "The return value is not used."
            EnumChildWindows(hWndParent, accumulator, IntPtr.Zero);

            return childWindows;
        }

        [DllImport(User32)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport(User32)]
        public static extern IntPtr GetActiveWindow();

        [DllImport(User32)]
        public static extern bool ScreenToClient(IntPtr handle, ref System.Drawing.Point screen);

        public static readonly IntPtr WindowNotFoundSentinel = new IntPtr(-1);

        // this is private because cPoint can only ever be 1 with our method signature
        [DllImport(User32, SetLastError = true, ExactSpelling = true)]
        private static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref Point point, int cPoints);

        public static Point MapWindowPoint(IntPtr hWndFrom, IntPtr hWndTo, Point point)
        {
            if (MapWindowPoints(hWndFrom, hWndTo, ref point, 1) == 0)
            {
                throw new Win32Exception();
            }
            return point;
        }

        public static IntPtr FirstChildWindow(IntPtr hWndParent, Predicate<IntPtr> predicate)
        {
            IntPtr foundWindow = WindowNotFoundSentinel;
            EnumWindowsProc searchFunc = (childHwnd, _1) =>
            {
                if (predicate(childHwnd))
                {
                    foundWindow = childHwnd;
                    // short circuit
                    return false;
                }

                return true;
            };

            // "The return value is not used."
            EnumChildWindows(hWndParent, searchFunc, IntPtr.Zero);

            return foundWindow;
        }

        [DllImport(User32)]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public static IntPtr FirstChildWindow(Process parent, Predicate<IntPtr> predicate)
        {
            IntPtr foundWindow = WindowNotFoundSentinel;
            EnumWindowsProc searchFunc = (childHwnd, _1) =>
            {
                if (predicate(childHwnd))
                {
                    foundWindow = childHwnd;
                    // short circuit
                    return false;
                }

                return true;
            };

            foreach (ProcessThread thread in parent.Threads)
            {
                // "The return value is not used."
                EnumThreadWindows(thread.Id, searchFunc, IntPtr.Zero);
            }

            return foundWindow;
        }

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static string GetClassName(IntPtr hWnd, bool throwOnError = true)
        {
            StringBuilder sb = new StringBuilder(MAX_PATH + 1);
            if (GetClassName(hWnd, sb, sb.Capacity) == 0)
            {
                if (throwOnError)
                {
                    throw new Win32Exception();
                }
                else
                {
                    return null;
                }
            }

            return sb.ToString();
        }

        [DllImport(User32, CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport(User32, CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport(User32, CharSet = CharSet.Auto)]
        internal static extern nint SendMessage(nint hWnd, int Msg, nint wParam, StringBuilder lParam);

        [DllImport(User32, CharSet = CharSet.Auto)]
        internal static extern nint SendMessage(nint hWnd, int Msg, nint wParam, nint lParam);

        public static string GetWindowText(IntPtr hWnd)
        {
            var sb = new StringBuilder(8192);

            if (GetWindowText(hWnd, sb, sb.Capacity) > 0)
            {
                // no-op, worked
            }
            else
            {
                // try WM_GETTEXT since https://blogs.msdn.microsoft.com/oldnewthing/20030821-00/?p=42833
                SendMessage(hWnd, WM_GETTEXT, sb.Capacity, sb);
            }

            return sb.ToString();
        }

        [DllImport(User32, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowText(IntPtr hWnd, string text);

        public static string WMGetText(IntPtr hWnd)
        {
            var sb = new StringBuilder(8192);
            SendMessage(hWnd, WM_GETTEXT, sb.Capacity, sb);
            return sb.ToString();
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern nint GetMenu(nint hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern int GetMenuItemID(nint hMenu, int nPos);

        [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "GetMenuStringW")]
        private static unsafe extern int GetMenuString(nint hMenu, int uIDItem, char* lpString, int cchMax, int flags);

        public const int MF_BYPOSITION = 0x400;

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern nint GetSubMenu(nint hMenu, int nPos);

        public static unsafe string GetMenuString(nint hMenu, int uIDItem, int flags)
        {
            const int cchMax = 256;
            char* buffer = stackalloc char[cchMax];
            int length = GetMenuString(hMenu, uIDItem, buffer, cchMax, flags);
            if (length == 0)
            {
                throw new Win32Exception();
            }
            return new string(buffer, 0, length);
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern int GetMenuItemCount(nint hMenu);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public int Width
            {
                get { return right - left; }
            }

            public int Height
            {
                get { return bottom - top; }
            }

            public void Offset(int dx, int dy)
            {
                left += dx;
                top += dy;
                right += dx;
                bottom += dy;
            }

            public bool IsEmpty
            {
                get
                {
                    return left >= right || top >= bottom;
                }
            }

            public static explicit operator RECT(Rect r)
            {
                return new RECT((int)r.Left, (int)r.Top, (int)r.Right, (int)r.Bottom);
            }

            public static explicit operator Int32Rect(RECT r)
            {
                return new Int32Rect(r.left, r.top, r.Width, r.Height);
            }

            public static explicit operator Rect(RECT r)
            {
                return new Rect(r.left, r.top, r.Width, r.Height);
            }
        }

        [DllImport(User32, SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        public static Rect GetWindowRect(IntPtr hWnd)
        {
            RECT w32Rect;
            if (!GetWindowRect(hWnd, out w32Rect))
            {
                throw new Win32Exception();
            }

            return (Rect)w32Rect;
        }

        [DllImport(Dwmapi, PreserveSig = false)]
        public static extern void DwmGetWindowAttribute(IntPtr hWnd, uint dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport(User32, ExactSpelling = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        public enum GWLParameter
        {
            GWL_EXSTYLE = -20,
            GWL_HINSTANCE = -6,
            GWL_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4
        }

        [DllImport(User32, CharSet = CharSet.Unicode)]
        public static extern int SetWindowLong(IntPtr windowHandle, GWLParameter nIndex, int dwNewLong);

        [DllImport(User32, SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [Flags]
        public enum WinEventFlags : uint
        {
            WINEVENT_OUTOFCONTEXT = 0x0000,
            WINEVENT_SKIPOWNTHREAD = 0x0001,
            WINEVENT_SKIPOWNPROCESS = 0x0002,
            WINEVENT_INCONTEXT = 0x0004
        }

        public delegate void WinEventProc(IntPtr hWinEventHook, WinEvent eventId, IntPtr hwnd, WinObject objectId, WinChild childId, int thread, uint time);

        // this does not use a safehandle because the finalizer would not run on the correct thread
        [DllImport(User32, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr SetWinEventHook(WinEvent eventMin, WinEvent eventMax, IntPtr hModule, WinEventProc eventProc, int processId, int threadId, WinEventFlags flags);

        [DllImport(User32, ExactSpelling = true, SetLastError = true)]
        public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public WindowPlacementFlags Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommand ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;

            /// <summary>
            /// Gets the default (empty) value.
            /// </summary>
            public static WINDOWPLACEMENT Default
            {
                get
                {
                    WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }

            public WINDOWPLACEMENT()
            {
            }

            public WINDOWPLACEMENT(WindowPlacement wp)
            {
                this.Length = Marshal.SizeOf<WINDOWPLACEMENT>();
                this.Flags = wp.Flags;
                this.ShowCmd = wp.ShowCmd;
                this.MinPosition = (POINT)wp.MinPosition;
                this.MaxPosition = (POINT)wp.MaxPosition;
                this.NormalPosition = (RECT)wp.NormalPosition;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y)
            {
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }

            public static explicit operator System.Windows.Point(POINT p)
            {
                return new System.Windows.Point(p.X, p.Y);
            }

            public static explicit operator POINT(System.Windows.Point p)
            {
                return new POINT((int)p.X, (int)p.Y);
            }
        }

        [DllImport(User32, SetLastError = true)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport(User32, SetLastError = true)]
        private static extern bool SetWindowPlacement(IntPtr hWnd, in WINDOWPLACEMENT lpwndpl);

        public static void SetWindowPlacement(IntPtr hWnd, WindowPlacement placement)
        {
            var wp = new WINDOWPLACEMENT(placement);
            if (!SetWindowPlacement(hWnd, wp))
            {
                throw new Win32Exception();
            }
        }

        public static WINDOWPLACEMENT GetWindowPlacement(IntPtr hWnd)
        {
            var wp = new WINDOWPLACEMENT();
            wp.Length = Marshal.SizeOf(wp);

            // If the function fails, the return value is zero
            if (!GetWindowPlacement(hWnd, ref wp))
            {
                throw new Win32Exception();
            }

            return wp;
        }

        [DllImport(User32, ExactSpelling = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        private const uint WM_NULL = 0;
        private const uint SMTO_NORMAL = 0;
        private const int ERROR_TIMEOUT = 0x5b4;

        [DllImport(User32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

        public static void PingWindow(IntPtr hWnd, uint timeoutMsec)
        {
            IntPtr result;
            if (SendMessageTimeout(hWnd, WM_NULL, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, timeoutMsec, out result) == IntPtr.Zero)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_TIMEOUT)
                {
                    throw new TimeoutException();
                }
                else
                {
                    throw new Win32Exception(lastError);
                }
            }
        }

        public static IntPtr MdiGetActive(IntPtr hWnd)
        {
            IntPtr result;
            if (SendMessageTimeout(hWnd, WM_MDIGETACTIVE, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL, 5 * 1000, out result) == IntPtr.Zero)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_TIMEOUT)
                {
                    throw new TimeoutException();
                }
                else
                {
                    throw new Win32Exception(lastError);
                }
            }
            return result;
        }

        [DllImport(User32, SetLastError = true, ExactSpelling = true)]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport(User32, ExactSpelling = true)]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        public const short SWP_NOMOVE = 0X2;
        public const short SWP_NOSIZE = 1;
        public const short SWP_NOZORDER = 0X4;
        public const int SWP_SHOWWINDOW = 0x0040;

        [DllImport(User32)]
        public static extern bool ShowWindow(IntPtr hwnd, ShowWindowCommand nCmdShow);

        [DllImport(User32)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;

        [Flags]
        public enum MONITORINFOF
        {
            PRIMARY = 0x1
        }

        private const int CCHDEVICENAME = 32;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public MONITORINFOF dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string szDevice;
        }

        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport(User32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport(User32)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);


        [DllImport("SHELL32.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void SHOpenWithDialog(IntPtr hwnd, in OPENASINFO poaInfo);

        public struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcszFile;
            [MarshalAs(UnmanagedType.LPWStr)]
#pragma warning disable CS0649 // Field 'NativeMethods.OPENASINFO.pcszClass' is never assigned to, and will always have its default value null
            public string pcszClass;
#pragma warning restore CS0649 // Field 'NativeMethods.OPENASINFO.pcszClass' is never assigned to, and will always have its default value null
            public OPEN_AS_INFO_FLAGS oaifInFlags;
        }

        public enum OPEN_AS_INFO_FLAGS
        {
            ALLOW_REGISTRATION = 0x00000001,
            REGISTER_EXT = 0x00000002,
            EXEC = 0x00000004,
            FORCE_REGISTRATION = 0x00000008,
            HIDE_REGISTRATION = 0x00000020,
            URL_PROTOCOL = 0x00000040,
            FILE_IS_URI = 0x00000080,
        }
    }
}