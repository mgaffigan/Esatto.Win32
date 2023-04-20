# Esatto Win32 Common

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
  * Standard windows Username/Password prompt
  * Standard windows Certificate prompt
  * Mapping a network drive with or without a drive letter
  * Setting a file's Mark-of-the-web

## Create Job

Ensures that even if you crash or things go wrong, child processes will not be left around.

	using (var job = new Job())
	{
		var child = Process.Start(/* blah */);
		// small opportunity between Process.Start and job.AddProcess.  Use 
		// CREATE_SUSPENDED to avoid the bug
		job.AddProcess(child);
		throw new InvalidOperationException("Example");
	}
	// since Job.Dispose was called, the child process was terminated by the OS

# Create process in other session

While running as a user with rights to `SE_TCB_NAME` (typically `SYSTEM`), you 
can create a process in a different user's session with `CreateProcessForSession`.

    var otherUserSession = /* Get another user's session ID */;
    ProcessInterop.CreateProcessForSession(otherUserSession, "calc.exe", "");

# Privilege manipulation

To run a piece of code [with privileges](https://learn.microsoft.com/en-us/windows/win32/secauthz/privilege-constants)

    var privs = new[] 
	{
		Privilege.TrustedComputingBase, 
		Privilege.AssignPrimaryToken,
		Privilege.IncreaseQuota
	};
    Privilege.RunWithPrivileges(() => 
	{
		Console.WriteLine("I got the power!");
	}, privs);

## Write RAW Print job

To send RAW data to a windows print queue, as frequently required for thermal label printers.

    using (var windowsJob = new GdiPrintJob("Zebra TLP384", GdiPrintJobDataType.Raw, "Example Job Name", null))
    {
		var exampleLabelData = new MemoryStream(Encoding.ASCII.GetBytes(@"^XA
	^LT120
	^FX Top section
	^CFB,25
	^FO50,173^FDFROM:^FS
	^FO200,173^FDTest sender^FS
	^FO200,228^FD10 MOUNTAIN PKWY^FS
	^FO200,283^FDTN, COLLIERVILLE, 38017^FS
	^FO50,343^GB706,1,3^FS
	^XZ"));
        windowsJob.WritePage(exampleLabelData);
    }