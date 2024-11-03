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

namespace Esatto.Win32.Windows;

public class FocusedWindowChangedEventArgs(Win32Window window) : EventArgs
{
    public Win32Window Window { get; } = window;
}

public sealed class ForegroundWindowMonitor : IDisposable
{
    private readonly WinEventHook ForegroundHook;

    private bool IsDisposed;

    public event EventHandler<FocusedWindowChangedEventArgs> ForegroundWindowChanged;

    public event EventHandler<UnhandledExceptionEventArgs> Exception;

    public bool IsEnabled
    {
        get => ForegroundHook.IsEnabled;
        set => this.ForegroundHook.IsEnabled = value;
    }

    public ForegroundWindowMonitor(Win32Window window, Win32Window relativeTo = null)
    {
        if (window == null)
        {
            throw new ArgumentNullException(nameof(window), "Contract assertion not met: window != null");
        }

        this.ForegroundHook = new WinEventHook(null, null,
            WinEvent.EVENT_SYSTEM_FOREGROUND, WinEvent.EVENT_SYSTEM_FOREGROUND,
            syncCtx: SynchronizationContext.Current);
        this.ForegroundHook.Subscribe(Hook_EventReceived);
    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        
        this.ForegroundHook.Dispose();
    }

    private Win32Window lastWindow;

    private void Hook_EventReceived(WinEventEventArgs args)
    {
        try
        {
            if (args.Window.Handle == lastWindow?.Handle) return;
            lastWindow = args.Window;

            ForegroundWindowChanged?.Invoke(this, new FocusedWindowChangedEventArgs(args.Window));
        }
        catch (Exception ex)
        {
            this.Exception?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
        }
    }
}