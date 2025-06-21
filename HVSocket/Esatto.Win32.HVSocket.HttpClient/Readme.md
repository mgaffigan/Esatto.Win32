# HyperV HttpClient Transport

Provides a binding and transport to allow HyperV communication across VM boundaries without network connections using [Hyper-V Sockets](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/make-integration-service#register-a-new-application).

Example Client running on a guest:

    using Esatto.Win32.HVSocket;

    using var httpClient = HyperVSocketHttpClient.Create(Guid.Parse("C7240163-6E2B-4466-9E41-FF74E7F0DE47"));
    var response = await httpClient.GetStringAsync("/");
    Console.WriteLine(response);
	
Example Client running on the Hypervisor connecting to VM 642d4719-f5d7-477d-9ca3-2c46c280052d:

    using Esatto.Win32.HVSocket;

    using var httpClient = HyperVSocketHttpClient.Create(
        Guid.Parse("642d4719-f5d7-477d-9ca3-2c46c280052d"),
        Guid.Parse("C7240163-6E2B-4466-9E41-FF74E7F0DE47"));
    var response = await httpClient.GetStringAsync("/");
    Console.WriteLine(response);
	
Notes:

 - Replace `C7240163-6E2B-4466-9E41-FF74E7F0DE47` with the service ID you register in the registry (see Example Hypervisor Service Registration.reg).  When developing a new app, the service ID should be unique and generated specifically for that application.
   - Register your service ID in the hypervisor registry
   - Enable "Guest Services" in the VM settings under "Integration Services"
 - Replace `642d4719-f5d7-477d-9ca3-2c46c280052d` with the ID of the VM to which you want to connect or any of the VMID wildcards.  The VM ID can be found with Powershell using `Get-VM`.
 - See Esatto.Win32.HVSocket.KestrelListener for a Kestrel listener implementation
