using Esatto.Win32.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Esatto.Win32.Windows
{
    /// <summary>
    /// Monitors location events for a <see cref=""Win32Window/>
    /// </summary>
    public sealed class WindowLocationMonitor : IDisposable
    {
        private readonly Process Process;
        private readonly WinEventHook WindowDraggingHook;
        private readonly WinEventHook WindowMovedHook;
        private readonly Win32Window Window;
        private readonly Win32Window RelativeToWindow;

        private bool IsDragging;
        private bool IsDisposed;

        public event EventHandler DragBegin;

        public event EventHandler DragEnd;

        public event EventHandler<WindowLocationUpdatedEventArgs> WindowMoved;

        public event EventHandler<UnhandledExceptionEventArgs> Exception;

        public bool IsEnabled
        {
            get
            {
                return WindowDraggingHook.IsEnabled;
            }
            set
            {
                this.WindowDraggingHook.IsEnabled = value;
                this.WindowMovedHook.IsEnabled = value;
            }
        }

        public WindowLocationMonitor(Win32Window window, Win32Window relativeTo = null)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window), "Contract assertion not met: window != null");
            }

            this.Window = window;
            this.RelativeToWindow = relativeTo ?? new Win32Window(IntPtr.Zero);

            this.Process = Window.GetProcess();
            this.WindowDraggingHook = new WinEventHook(this.Process, null, 
                WinEvent.EVENT_SYSTEM_FOREGROUND, WinEvent.EVENT_SYSTEM_MINIMIZEEND, 
                syncCtx: SynchronizationContext.Current);
            this.WindowDraggingHook
                .Where(c => c.WinObject == WinObject.OBJID_WINDOW && (
                c.Event == WinEvent.EVENT_SYSTEM_FOREGROUND ||
                c.Event == WinEvent.EVENT_SYSTEM_MOVESIZESTART ||
                c.Event == WinEvent.EVENT_SYSTEM_MOVESIZEEND ||
                c.Event == WinEvent.EVENT_SYSTEM_MINIMIZEEND ||
                c.Event == WinEvent.EVENT_SYSTEM_MINIMIZESTART
                ))
                .Subscribe(Hook_EventReceived);

            // LocationChange is required to support session connect rearrange, Win+Left, Minimize, Restore
            this.WindowMovedHook = new WinEventHook(Window.GetProcess(), null, 
                WinEvent.EVENT_OBJECT_LOCATIONCHANGE, WinEvent.EVENT_OBJECT_LOCATIONCHANGE,
                syncCtx: SynchronizationContext.Current);
            this.WindowMovedHook
                .Where(c => c.WinObject == WinObject.OBJID_WINDOW && c.Window.Handle == Window.Handle)
                .Subscribe(Hook_EventReceived);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;

            this.DragBegin = null;
            this.DragEnd = null;
            this.WindowMoved = null;
            this.Exception = null;
            this.WindowDraggingHook.Dispose();
            this.WindowMovedHook.Dispose();
        }

        private void Hook_EventReceived(WinEventEventArgs args)
        {
            try
            {
                switch (args.Event)
                {
                    case WinEvent.EVENT_SYSTEM_MOVESIZESTART:
                        if (args.Window.Equals(Window))
                        {
                            IsDragging = true;
                            DragBegin?.Invoke(this, new EventArgs());
                        }
                        break;

                    case WinEvent.EVENT_SYSTEM_MOVESIZEEND:
                        if (args.Window.Equals(Window))
                        {
                            OnWindowMoved();
                            DragEnd?.Invoke(this, new EventArgs());
                            IsDragging = false;
                        }
                        break;

                    case WinEvent.EVENT_OBJECT_LOCATIONCHANGE:
                    case WinEvent.EVENT_SYSTEM_FOREGROUND:
                        if (!IsDragging)
                        {
                            OnWindowMoved();
                        }
                        break;

                    case WinEvent.EVENT_SYSTEM_MINIMIZEEND:
                    case WinEvent.EVENT_SYSTEM_MINIMIZESTART:
                        OnWindowMoved();
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Exception?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
            }
        }

        private void OnWindowMoved()
        {
            var relativeRect = RelativeToWindow.GetBounds();
            var myRect = Window.GetBounds();
            myRect.X -= relativeRect.X;
            myRect.Y -= relativeRect.Y;

            WindowMoved?.Invoke(this, new WindowLocationUpdatedEventArgs(Window.GetIsVisible(), myRect));

            //WindowMoved?.Invoke(this, new WindowLocationUpdatedEventArgs(Window.GetIsVisible(), Window.TransformTo(RelativeToWindow, Window.GetBounds())));
        }
    }
}