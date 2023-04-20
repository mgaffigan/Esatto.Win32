using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Esatto.Win32.Windows
{
    public sealed class WindowLocationUpdatedEventArgs : EventArgs
    {
        public Rect WindowRect { get; }

        public bool IsVisible { get; }

        public WindowLocationUpdatedEventArgs(bool isVisible, Rect newlocation)
        {
            IsVisible = isVisible;
            WindowRect = newlocation;
        }
    }
}