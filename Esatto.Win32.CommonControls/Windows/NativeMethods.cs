using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Windows.Point;
using Rect = System.Windows.Rect;

namespace Esatto.Win32.Windows
{
    internal static partial class NativeMethods
    {
        private const string User32 = "user32.dll";
        internal const int MAX_PATH = 260;
        internal const int WM_GETTEXT = 0xD;
        internal const int WM_MDIGETACTIVE = 0x0229;
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
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, StringBuilder lParam);

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

        public static string WMGetText(IntPtr hWnd)
        {
            var sb = new StringBuilder(8192);
            SendMessage(hWnd, WM_GETTEXT, sb.Capacity, sb);
            return sb.ToString();
        }

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

        public enum ShowWindowCommands : uint
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,

            /// <summary>
            /// Activates and displays a window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window
            /// for the first time.
            /// </summary>
            Normal = 1,

            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,

            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3,

            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>
            ShowMaximized = 3,

            /// <summary>
            /// Displays a window in its most recent size and position. This value
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,

            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,

            /// <summary>
            /// Minimizes the specified window and activates the next top-level
            /// window in the Z order.
            /// </summary>
            Minimize = 6,

            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,

            /// <summary>
            /// Displays the window in its current size and position. This value is
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the
            /// window is not activated.
            /// </summary>
            ShowNA = 8,

            /// <summary>
            /// Activates and displays the window. If the window is minimized or
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,

            /// <summary>
            /// Sets the show state based on the SW_* value specified in the
            /// STARTUPINFO structure passed to the CreateProcess function by the
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,

            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread
            /// that owns the window is not responding. This flag should only be
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

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
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommands ShowCmd;

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
        }

        [DllImport(User32, SetLastError = true)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

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
        public static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport(User32)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport(User32)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;

        public enum cmdShow
        {
            /// <summary>
            /// Minimizes a window, even if the thread that owns the window is not responding.This flag should only be used when minimizing windows from a different thread.
            /// </summary>
            SW_FORCEMINIMIZE = 11,

            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,

            /// <summary>
            ///  Maximizes the specified window.
            /// </summary>
            SW_MAXIMIZE = 3,

            /// <summary>
            /// Minimizes the specified window and activates the next top-level window in the Z order.
            /// </summary>
            SW_MINIMIZE = 6,

            /// <summary>
            /// Activate and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            SW_RESTORE = 9,

            /// <summary>
            /// Activates the window and displays it in its current size and position.
            /// </summary>
            SW_SHOW = 5,

            /// <summary>
            /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
            /// </summary>
            SW_SHOWDEFAULT = 10,

            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,

            /// <summary>
            /// Displays the window as a minimized window.This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,

            /// <summary>
            /// Displays the window in its current size and position.This value is similar to SW_SHOW, except that the window is not activated.
            /// </summary>
            SW_SHOWNA = 8,

            /// <summary>
            /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
            /// </summary>
            SW_SHOWNOACTIVATE = 4,

            /// <summary>
            /// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_SHOWNORMAL = 1,
        }

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
    }
}