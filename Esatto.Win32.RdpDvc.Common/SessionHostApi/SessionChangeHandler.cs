using Esatto.Win32.RdpDvc.TSVirtualChannels;
using System.ComponentModel;
using System.Diagnostics;
using static Esatto.Win32.RdpDvc.TSVirtualChannels.NativeMethods;

namespace Esatto.Win32.RdpDvc.SessionHostApi;

public sealed class SessionChangeHandler : IDisposable
{
    private readonly NotifyWindow Window;

    public SessionChangeHandler()
    {
        this.Window = new NotifyWindow((_, e) => SessionChange?.Invoke(this, e));
        try
        {
            if (!WTSRegisterSessionNotification(Window.Handle, NOTIFY_FOR_THIS_SESSION))
            {
                throw new Win32Exception();
            }
        }
        catch
        {
            Window.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (!WTSUnRegisterSessionNotification(Window.Handle))
        {
            throw new Win32Exception();
        }
        Window.Dispose();
    }

    public event EventHandler<SessionChangeEventArgs>? SessionChange;

    private sealed class NotifyWindow : NativeWindow, IDisposable
    {
        private readonly EventHandler<SessionChangeEventArgs> SessionChange;

        public NotifyWindow(EventHandler<SessionChangeEventArgs> sessionChange)
        {
            this.SessionChange = sessionChange;
            CreateHandle(new() { Caption = "WTSRegisterSessionNotification", Parent = new IntPtr(-3) });
        }

        public void Dispose() => DestroyHandle();

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_WTSSESSION_CHANGE)
            {
                SessionChange?.Invoke(this, new SessionChangeEventArgs((WTS_EVENT)(int)m.WParam));
                return;
            }

            base.WndProc(ref m);
        }
    }
}

public sealed class SessionChangeEventArgs : EventArgs
{
    public SessionChangeEventArgs(WTS_EVENT eventID)
    {
        this.Event = eventID;
    }

    public WTS_EVENT Event { get; }
}