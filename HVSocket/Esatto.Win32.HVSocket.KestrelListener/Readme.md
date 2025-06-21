# Esatto Hyper V Kestrel Listener

Provides a binding and transport to allow HyperV communication across VM boundaries without network connections using [Hyper-V Sockets](https://docs.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/make-integration-service#register-a-new-application).

Example Server (running on guest or hypervisor):

    using Esatto.Win32.HVSocket;

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddHyperVSocketListener(Guid.Parse("C7240163-6E2B-4466-9E41-FF74E7F0DE47"));

    var app = builder.Build();

    // Example Minimal API route
    app.MapGet("/", () => "Hello!");

    app.Run();
	
Notes:

 - Replace `C7240163-6E2B-4466-9E41-FF74E7F0DE47` with the service ID you register in the registry (see Example Hypervisor Service Registration.reg).  When developing a new app, the service ID should be unique and generated specifically for that application.
   - Register your service ID in the hypervisor registry
   - Enable "Guest Services" in the VM settings under "Integration Services"
 - See Esatto.Win32.HVSocket.HttpClient for the client side of this server.
