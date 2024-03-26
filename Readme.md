# Esatto Win32

Set of APIs for working with windows OS primitives

## Esatto.Win32.Registry

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.Registry)](https://www.nuget.org/packages/Esatto.Win32.Registry)

Windows Registry including Group Policy and ADMX Generation

[More information](Esatto.Win32.Registry/Readme.md)

## Esatto.Win32.Com

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.Com)](https://www.nuget.org/packages/Esatto.Win32.Com)

* Clients to Out-of-process servers
* Implementation of Out-of-process servers
* Running object table (ROT)
* `IStream` wrappers

[More information](Esatto.Win32.Com/Readme.md)

## Esatto.Win32.Common

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.Common)](https://www.nuget.org/packages/Esatto.Win32.Common)

* Process and Job objects
  * Create Job
  * Create process in other session
  * Privilege manipulation
* Printers
  * Creating RAW Print Jobs
  * Installing and administering Port Monitors
  * Installing Printer Drivers
* Installing and administering NT Services
* Windows Security access
  * Computer and user names (FQDN, UPN, etc...) without LDAP
  * Mapping a network drive with or without a drive letter
  * Setting a file's Mark-of-the-web

[More information](Esatto.Win32.Common/Readme.md)

## Esatto.Win32.CommonControls

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.CommonControls)](https://www.nuget.org/packages/Esatto.Win32.CommonControls)

* Reading Monitor layouts
* Native windows
  * `HWND`'s
  * SysMenu
  * `SetWinEventHook` as `IObservable`
  * Window locations as `IObservable`
  * Standard windows Username/Password prompt
* Windows Shell
  * `IPreviewHandler`'s
  * File type associations
* Printers
  * Saving and restoring printer settings

[More information](Esatto.Win32.CommonControls/Readme.md)

## Esatto.Win32.NetInjector

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.NetInjector)](https://www.nuget.org/packages/Esatto.Win32.NetInjector)

Inject a .Net DLL into a remote process.

[More information](Esatto.Win32.NetInjector/Readme.md)

## Esatto.Utilities

[![Nuget](https://img.shields.io/nuget/v/Esatto.Utilities)](https://www.nuget.org/packages/Esatto.Utilities)

* TBD

[More information](Esatto.Utilities/Readme.md)

## Esatto.Win32.RdpDvc.Common

[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.RdpDvc.Common)](https://www.nuget.org/packages/Esatto.Win32.RdpDvc.Common)

Core API's for working with Remote Desktop Protocol Dynamic Virtual Channels.  See 
[Esatto.Rdp.DvcApi](https://github.com/mgaffigan/Esatto.Rdp.DvcApi) for a WCF-over-RDP
built on this library.  See [Esatto.AppCoordination](https://github.com/mgaffigan/Esatto.AppCoordination) 
for a distributed application coordination framework built on top of this.

[More information](Esatto.Win32.RdpDvc.Common/Readme.md)

# Other options

Many of the wrappers in this solution have been obsoleted by the 
[C#/Win32 P/Invoke Source Generator](https://github.com/Microsoft/CsWin32) 
which permits easy generation of P/Invokes.  This solution continues to be
actively used and developed, CsWin32 may better fit your use case.