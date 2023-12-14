using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Esatto.Win32.Wpf
{
    public static class RectExtensions
    {
        public static Point GetCenter(this Rect rect) 
            => new Point((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
    }
}
