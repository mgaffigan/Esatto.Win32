# Esatto Win32 NetInjector
Wrapper for Esatto.Win32.NetInjector.NetFx or Esatto.Win32.NetInjector.NetCore
to invoke a static method in a remote process.

## Requirements

- Injecting process must be same bitness as target process
- Target must pump window messages on target hWnd
- The specified runtime must already be loaded in the target process
- The injecting process must have permission to write to the target process
- Type load will occur from the target process, so the injected method must not 
  rely upon assembly resolution of the source process.
- If you specify `RuntimeVersions.LoadedNetCore`, the target process must have already
  loaded the .NET Core loaded before injection.

## Example use

Add nuget reference to `Esatto.Win32.NetInjector`, find a target hWnd, and call `Injector.Inject`.

```csharp
var process = Process.GetProcessesByName("FrameworkLTC").Single();
// Alternatively: new EntryPointReference(@"c:\path\to\demohook.dll", "Namespace.DemoHook", "Inject");
var ep = new EntryPointReference(typeof(DemoHook), nameof(DemoHook.Inject));
Injetor.Inject(process.MainWindowHandle, ep, "Hello world!", RuntimeVersions.NetFxAny);

public class DemoHook
{
    public static int Inject(string arg)
    {
        MessageBox.Show(arg);
        return 0 /* S_OK */;
    }
}
```

Full examples [for .NET Framework](https://github.com/mgaffigan/Esatto.Win32/tree/master/Esatto.Win32.NetInjector.Demo.NetFx) 
and [.NET Core](https://github.com/mgaffigan/Esatto.Win32/tree/master/Esatto.Win32.NetInjector.Demo.NetCore).

## Notes

- You may need to install an assembly resolution hook in the target process. Be sure
  to install the resolver before calling any method which references a type in an
  unloadable assembly.
- You may need to specify the assembly path, type name, and method name explicitly
  if the hook type is not already loaded in the injecting process.
- To inject a 64-bit process from a 32-bit process (or 32- from 64-), make an thunk
  with a matching bitness
- The entrypoint must return before the call to `Injector.Inject` will return
- The entrypoint should not throw.  It may return an `HRESULT`, but they don't seem
  to reliably get returned to the injecting process.
- Check debug output on the target process if hooks fail, typically this is the result
  of a required dependency assembly not being locatable by the target CLR.
- The 