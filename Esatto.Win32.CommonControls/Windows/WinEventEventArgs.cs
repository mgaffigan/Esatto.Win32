using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Windows
{
    public sealed class WinEventEventArgs : EventArgs
    {
        internal WinEventEventArgs(WinEventHook hook, WinEvent @event, IntPtr hwnd, WinObject @object, WinChild child, int threadId, uint time)
        {
            this.Hook = hook;
            this.Event = @event;
            this.Window = new Win32Window(hwnd);
            this.WinObject = @object;
            this.WinChild = child;
            this.ThreadId = threadId;
            this.Timestamp = time;
        }

        public WinEvent Event { get; }
        public WinEventHook Hook { get; }
        public Win32Window Window { get; }
        public int ThreadId { get; }
        public uint Timestamp { get; }
        public WinChild WinChild { get; }
        public WinObject WinObject { get; }

        public override string ToString() => $"Event {Event} for {Window} for {WinChild} of {WinObject}";
    }
}
