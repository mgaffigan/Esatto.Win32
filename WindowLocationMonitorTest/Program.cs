using Esatto.Win32.Windows;
using System.Windows;

var hwnd = IntPtr.Parse(Console.ReadLine()!, System.Globalization.NumberStyles.HexNumber);
using var monitor = new AbsoluteWindowLocationMonitor(new Win32Window(hwnd));
monitor.WindowMoved += (s, e) => Console.WriteLine(e.WindowRect);
monitor.DragBegin += (s, e) => Console.WriteLine("DragBegin");
monitor.DragEnd += (s, e) => Console.WriteLine("DragEnd");
monitor.Exception += (s, e) => Console.WriteLine(e.ExceptionObject);

var a = new Application();
a.Run();