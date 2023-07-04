// define debug for Debug.WriteLine to work
#define DEBUG

using Esatto.Win32.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace Esatto.Win32.Wpf
{
    public static class WindowPlacement
    {
        public static string GetSavePath(DependencyObject obj) => (string)obj.GetValue(SavePathProperty);

        public static void SetSavePath(DependencyObject obj, string value) => obj.SetValue(SavePathProperty, value);

        public static readonly DependencyProperty SavePathProperty =
            DependencyProperty.RegisterAttached("SavePath", typeof(string), typeof(WindowPlacement),
                new PropertyMetadata(null, SavePath_Changed));

        private static void SavePath_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d)) return;
            if (d is not Window wnd) throw new InvalidOperationException("WindowPlacement can only be applied to a Window");

            // Ensure idempotence
            wnd.Closing -= Window_Closing;
            wnd.Closing += Window_Closing;

            var newValue = (string)e.NewValue;
            if (!string.IsNullOrWhiteSpace(newValue))
            {
                try
                {
                    RestoreWindowLocation(wnd, newValue);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not restore window location for {wnd}: {ex}");
                }
            }
        }

        private static void Window_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel) return;

            var window = (Window)sender;
            var savePath = GetSavePath(window);
            try
            {
                if (string.IsNullOrWhiteSpace(savePath)) return;
                if (window.WindowState == WindowState.Minimized) return;

                using (var key = Registry.CurrentUser.CreateSubKey(savePath))
                {
                    var wp = Windows.NativeMethods.GetWindowPlacement(new WindowInteropHelper(window).Handle);
                    key.SetValue(nameof(window.WindowState), (int)window.WindowState);
                    key.SetValue(nameof(window.Left), wp.NormalPosition.left);
                    key.SetValue(nameof(window.Top), wp.NormalPosition.top);
                    key.SetValue(nameof(window.Width), wp.NormalPosition.Width);
                    key.SetValue(nameof(window.Height), wp.NormalPosition.Height);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not save window location for {window}: {ex}");
            }
        }

        private static void RestoreWindowLocation(Window window, string savePath)
        {
            WindowState state;
            var pos = new Rect();
            using (var key = Registry.CurrentUser.CreateSubKey(savePath))
            {
                var oState = key.GetValue(nameof(window.WindowState));
                if (oState is null) return;
                state = (WindowState)(int)oState;

                pos.X = (int)key.GetValue(nameof(window.Left), 0);
                pos.Y = (int)key.GetValue(nameof(window.Top), 0);
                pos.Width = (int)key.GetValue(nameof(window.Width), 0);
                pos.Height = (int)key.GetValue(nameof(window.Height), 0);
            }

            // verify that the size is "reasonable"
            if ((pos.Width * pos.Height) < 2 /* in^2 */ * 96 * 96 /* dpi^2 */)
            {
                Debug.WriteLine($"Omitting restore of location for {window} due to {pos} being too small to restore");
                return;
            }

            double IntersectionPercentage(Rect a, Rect b)
            {
                if (!a.IntersectsWith(b)) return 0d;
                b.Intersect(a);
                return (b.Width * b.Height) / (a.Width * a.Height);
            }
            var mostlyOn = (
                from monitor in MonitorInfo.GetAllMonitors()
                let overlap = IntersectionPercentage(pos, monitor.ViewportBounds)
                // only consider a monitor if more than 75% of the window is showing
                where overlap > 0.75d
                // pick the highest overlapping monitor
                orderby overlap descending
                select monitor
            ).FirstOrDefault();

            if (mostlyOn is null)
            {
                Debug.WriteLine($"Omitting restore of location for {window} due to {pos} being offscreen");
                return;
            }

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = pos.Left;
            window.Top = pos.Top;
            window.Width = pos.Width;
            window.Height = pos.Height;
            window.WindowState = WindowState.Normal;

            // https://nikola-breznjak.com/blog/quick-tips/maximizing-a-wpf-window-to-second-monitor/
            if (state == WindowState.Maximized)
            {
                window.Loaded += Window_MaximizeOnFirstLoaded;
            }
        }

        private static void Window_MaximizeOnFirstLoaded(object sender, RoutedEventArgs e)
        {
            var window = (Window)sender;
            window.Loaded -= Window_MaximizeOnFirstLoaded;

            window.WindowState = WindowState.Maximized;
        }
    }
}
