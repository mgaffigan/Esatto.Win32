using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Esatto.Win32.HVSocket;

internal sealed class HyperVSocketTransportFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
{
    private readonly SocketTransportFactory _inner;

    public HyperVSocketTransportFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
    {
        _inner = new SocketTransportFactory(options, loggerFactory);
    }

    public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        return _inner.BindAsync(endpoint, cancellationToken);
    }

    public bool CanBind(EndPoint endpoint) => endpoint is HyperVSocketEndPoint;
}