using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.AspNetCore.Connections;
using static Esatto.Win32.HVSocket.NativeMethods;
using System.Net.Sockets;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Esatto.Win32.HVSocket;

public static class KestrelExtensions
{
    /// <summary>
    /// Configures the application to listen for incoming Hyper-V socket connections.
    /// </summary>
    /// <remarks>This method enables the application to accept Hyper-V socket connections by registering a
    /// listener for the specified service ID.</remarks>
    /// <param name="svcs">The <see cref="IServiceCollection"/> used to configure the application.</param>
    /// <param name="serviceId">The unique identifier of the service to listen for. This identifies the specific service that the application
    /// will handle connections for, similar to a TCP Port number.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance, allowing for further configuration chaining.</returns>
    public static IServiceCollection AddHyperVSocketListener(this IServiceCollection svcs, Guid serviceId)
        => AddHyperVSocketListener(svcs, Guid.Empty /* wildcard */, serviceId);

    /// <summary>
    /// Configures the host to listen for incoming connections over a Hyper-V socket.
    /// </summary>
    /// <remarks>This method adds a Hyper-V socket listener to the host, allowing the application to accept 
    /// connections over Hyper-V sockets. It configures the necessary services and options for  Hyper-V socket transport
    /// and binds the listener to the specified virtual machine and service.</remarks>
    /// <param name="svcs">The <see cref="IServiceCollection"/> to configure.</param>
    /// <param name="vmId">The VMID to bind the listener to.  See https://learn.microsoft.com/en-us/virtualization/hyper-v-on-windows/user-guide/make-integration-service#vmid-wildcards</param>
    /// <param name="serviceId">The unique identifier of the service to listen for. This identifies the specific service that the application
    /// will handle connections for, similar to a TCP Port number.</param>
    /// <returns>The configured <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddHyperVSocketListener(this IServiceCollection svcs, Guid vmId, Guid serviceId)
    {
        ArgumentNullException.ThrowIfNull(svcs);

        svcs.AddSingleton<IConnectionListenerFactory, HyperVSocketTransportFactory>();
        svcs.Configure<SocketTransportOptions>(options =>
        {
            var defaultBinding = options.CreateBoundListenSocket;
            options.CreateBoundListenSocket = (endPoint) =>
            {
                if (endPoint is HyperVSocketEndPoint hyperVEndPoint)
                {
                    var listener = new Socket(AF_HYPERV, SocketType.Stream, HV_PROTOCOL_RAW);
                    listener.Bind(hyperVEndPoint);
                    return listener;
                }
                else return defaultBinding(endPoint);
            };
        });
        svcs.Configure<KestrelServerOptions>(options =>
        {
            options.Listen(new HyperVSocketEndPoint(vmId, serviceId));
        });

        return svcs;
    }
}
