
using Esatto.Win32.NetInjector;
using System.Diagnostics;

var process = Process.GetProcessesByName("HookTarget").Single();
var ep = new EntryPointReference(
#if DEBUG
    "..\\..\\..\\..\\HookExample\\bin\\Debug\\net8.0-windows\\HookExample.dll",
#else
    "..\\..\\..\\..\\HookExample\\bin\\Release\\net8.0-windows\\HookExample.dll",
#endif
    "HookExample.SampleHook", "Inject");
Injector.Inject(process.MainWindowHandle, ep, "Hello world");