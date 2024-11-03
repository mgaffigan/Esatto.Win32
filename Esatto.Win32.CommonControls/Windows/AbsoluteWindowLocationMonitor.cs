using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Esatto.Win32.Windows;

public class AbsoluteWindowLocationMonitor : IDisposable
{
    private readonly List<WindowLocationMonitor> Monitors;
    private readonly Win32Window Window;

    public AbsoluteWindowLocationMonitor(Win32Window window)
    {
        this.Window = window;
        var mons = new List<WindowLocationMonitor>();
        var cWindow = window;
        while (cWindow.Handle != IntPtr.Zero)
        {
            var monitor = new WindowLocationMonitor(cWindow);
            monitor.DragBegin += Monitor_DragBegin;
            monitor.DragEnd += Monitor_DragEnd;
            monitor.WindowMoved += Monitor_WindowMoved;
            monitor.Exception += Monitor_Exception;
            mons.Add(monitor);
            cWindow = cWindow.GetParent();
        }
        this.Monitors = mons;
    }

    public void Dispose()
    {
        var exceptions = new List<Exception>();
        foreach (var monitor in Monitors)
        {
            try { monitor.Dispose(); }
            catch (Exception ex) { exceptions.Add(ex); }
        }
        if (exceptions.Any()) throw new AggregateException(exceptions);
    }

    public event EventHandler DragBegin;

    public event EventHandler DragEnd;



    public event EventHandler<UnhandledExceptionEventArgs> Exception;
    private void Monitor_Exception(object? sender, UnhandledExceptionEventArgs e) => Exception?.Invoke(this, e);

    public event EventHandler<WindowLocationUpdatedEventArgs> WindowMoved;
    private Rect lastRect;
    private bool lastIsVisible;
    private void Monitor_WindowMoved(object? sender, WindowLocationUpdatedEventArgs e)
    {
        var newIsVisible = Window.GetIsVisible();
        var newRect = Window.GetBounds();
        if (newRect == lastRect && lastIsVisible == newIsVisible) return;
        lastRect = newRect;
        lastIsVisible = newIsVisible;

        WindowMoved?.Invoke(this, new WindowLocationUpdatedEventArgs(newIsVisible, newRect));
    }

    private void Monitor_DragEnd(object? sender, EventArgs e) => DragEnd?.Invoke(this, e);
    private void Monitor_DragBegin(object? sender, EventArgs e) => DragBegin?.Invoke(this, e);
}
