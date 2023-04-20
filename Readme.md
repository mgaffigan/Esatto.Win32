# Esatto Win32

Set of APIs for working with

* Windows Registry including Group Policy and ADMX Generation
* COM 
  * Clients to Out-of-process servers
  * Implementation of Out-of-process servers
  * Running object table (ROT)
  * `IStream` wrappers
* Process and Job objects
  * Create Job
  * Create process in other session
  * Privilege manipulation
* Reading Monitor layouts
* Native windows
  * `HWND`'s
  * SysMenu
  * `SetWinEventHook` as `IObservable`
  * Window locations as `IObservable`
* Windows Shell
  * `IPreviewHandler`'s
  * File type associations
* Printers
  * Saving and restoring printer settings
  * Creating RAW Print Jobs
  * Installing and administering Port Monitors
  * Installing Printer Drivers
* Installing and administering NT Services
* Windows Security access
  * Computer and user names (FQDN, UPN, etc...) without LDAP
  * Standard windows Username/Password prompt
  * Standard windows Certificate prompt
  * Mapping a network drive with or without a drive letter
  * Setting a file's Mark-of-the-web

# Other options

Many of the wrappers in this solution have been obsoleted by the 
[C#/Win32 P/Invoke Source Generator](https://github.com/Microsoft/CsWin32) 
which permits easy generation of P/Invokes.  This solution continues to be
actively used and developed, CsWin32 may better fit your use case.