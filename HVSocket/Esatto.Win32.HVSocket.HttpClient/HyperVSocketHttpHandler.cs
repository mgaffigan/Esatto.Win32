using System.Net.Sockets;
using static Esatto.Win32.HVSocket.NativeMethods;

namespace Esatto.Win32.HVSocket;

public static class HyperVSocketHttpHandler
{
    public static SocketsHttpHandler Create(Guid vmId, Guid serviceId)
    {
        return new SocketsHttpHandler()
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                var remoteEp = new HyperVSocketEndPoint(vmId, serviceId);
                var socket = new Socket(AF_HYPERV, SocketType.Stream, HV_PROTOCOL_RAW);
                // AF_HYPERV throws WSA_INVALID_PARAMETER (10022) for ConnectAsync
                using (cancellationToken.Register(socket.Close))
                {
                    await Task.Run(() => socket.Connect(remoteEp), cancellationToken);
                }
                return new NetworkStream(socket, ownsSocket: true);
            }
        };
    }
}
