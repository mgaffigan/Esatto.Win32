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

## Compatibility

The injector must be the same bitness as the target process:

| Injector \ Target | x86 | x64 |
|-------------------|-----|-----|
| x86               | ✅ | ❌ |
| x64               | ❌ | ✅ |

The injector and target may use different versions of .NET:

| Injector \ Target | .NET Core | .NET Framework  | None (native) |
|-------------------|-----------|-----------------|---------------|
| .NET Core         | ✅        | ✅             | ✅            |
| .NET Framework    | ✅        | ✅             | ✅            |

**Note:** Starting a second version of .NET Core in a target process will fail.  This is
due to a limitation in the .NET Core runtime.  You can work around this by specifying
`RuntimeVersions.LoadedNetCore` which always uses the existing version.

**Note:** Starting a version of .NET Core in a .NET Framework process will succeed but is
not supported.  See [dotnet/runtime#53729](https://github.com/dotnet/runtime/issues/53729).

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

## .NET Runtime Version

The injector and the target process do not need to run the same .NET Runtime.  Use the 
optional `runtimeVersion` parameter of `Inject` to specify the target runtime.

Values for `runtimeVersion` may be:

- `RuntimeVersions.NetFxAny`<br>Use the loaded version of .NET Framework or start the latest available version
- `RuntimeVersions.NetFx4`<br>Use or start the latest version of .NET 4 installed on the machine
- `"v4.0.30319"` or similar<br>Use or start the specified version of .NET 4
- `RuntimeVersions.NetCoreAny`<br>Use or start the latest installed version of .NET Core
- `@"C:\foo\bar\baz.runtimeconfig.json"`<br>Use the version of .NET Core specified in the
   runtimeconfig.json path provided.  If a different incompatible runtime is already loaded
   in the target process, an error will be returned.  Only later versions of .NET are
   supported in this manner.  You can create `*.runtimeconfig.json` using
   `<EnableDynamicLoading>true</EnableDynamicLoading>` property in the hook csproj.
- `RuntimeVersions.LoadedNetCore`<br>Use the .NET Core runtime already loaded in the target
   process will be used.  This is compatible with older versions of .NET Core through at
   least .NET 8, but [there are plans](https://github.com/dotnet/runtime/issues/52688) to 
   remove this support from future versions of the runtime.
- `null`<br>Use heuristics to determine the best runtime to use

The default runtime (`runtimeVersion == null`) is:

1. If `<AssemblyName>.runtimeconfig.json` exists, use it
1. If `<AssemblyName>.deps.json` exists, use `RuntimeVersions.LoadedNetCore`
1. Use `RuntimeVersions.NetFxAny`

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

## Redistribution

Include the `Esatto.Win32.NetInjector.dll` and `Esatto.Win32.NetInjector.NetFx.dll` 
in your setup installer.  If you are using `RuntimeVersions.NetCoreAny` or passing
a `runtimeconfig.json` file, you must also include `nethost.dll`. `nethost.dll` is
not needed for `RuntimeVersions.LoadedNetCore`.