
using Esatto.Win32.NetInjector;
using HookExample;
using System.Diagnostics;

var process = Process.GetProcessesByName("HookTarget").Single();
//var ep = new EntryPointReference("HookExample.dll", "Type", nameof(SampleHook.Inject));
Injector.Inject(process.MainWindowHandle, new(SampleHook.Inject), "Hello world");