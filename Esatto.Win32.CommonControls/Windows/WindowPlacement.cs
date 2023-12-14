using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Esatto.Win32.Windows
{
    public struct WindowPlacement
    {
        public WindowPlacementFlags Flags;
        public ShowWindowCommand ShowCmd;
        public Point MinPosition;
        public Point MaxPosition;
        public Rect NormalPosition;

        public WindowPlacement()
        {
        }

        internal WindowPlacement(NativeMethods.WINDOWPLACEMENT wp)
        {
            this.Flags = wp.Flags;
            this.ShowCmd = wp.ShowCmd;
            this.MinPosition = (Point)wp.MinPosition;
            this.MaxPosition = (Point)wp.MaxPosition;
            this.NormalPosition = (Rect)wp.NormalPosition;
        }
    }
}
