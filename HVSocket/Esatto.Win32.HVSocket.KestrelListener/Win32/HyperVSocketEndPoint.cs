using static Esatto.Win32.HVSocket.NativeMethods;
using System.Net;
using System.Net.Sockets;

namespace Esatto.Win32.HVSocket;

[Serializable]
internal sealed class HyperVSocketEndPoint : EndPoint
{
    public HyperVSocketEndPoint(Guid vmId, Guid serviceId)
    {
        this.VmId = vmId;
        this.ServiceId = serviceId;
    }

    public override AddressFamily AddressFamily => AF_HYPERV;

    public Guid VmId { get; set; }

    public Guid ServiceId { get; set; }

    public override EndPoint Create(SocketAddress sockAddr)
    {
        if (sockAddr == null ||
            sockAddr.Family != AF_HYPERV ||
            sockAddr.Size != HYPERV_SOCK_ADDR_SIZE)
        {
            throw new ArgumentException("Invalid socket address", nameof(sockAddr));
        }

        var buffer = sockAddr.Buffer.Span;
        var vmId = new Guid(buffer.Slice(4, 16));
        var serviceId = new Guid(buffer.Slice(20, 16));
        return new HyperVSocketEndPoint(vmId, serviceId);
    }

    public override bool Equals(object? obj)
    {
        return obj is HyperVSocketEndPoint endpoint
            && this.VmId == endpoint.VmId && this.ServiceId == endpoint.ServiceId;
    }

    public override int GetHashCode() => ServiceId.GetHashCode() ^ VmId.GetHashCode();

    public override SocketAddress Serialize()
    {
        var sockAddress = new SocketAddress(AF_HYPERV, HYPERV_SOCK_ADDR_SIZE);
        var buffer = sockAddress.Buffer.Span;
        buffer[2] = 0;
        _ = VmId.TryWriteBytes(buffer.Slice(4, 16));
        _ = ServiceId.TryWriteBytes(buffer.Slice(20, 16));
        return sockAddress;
    }

    public override string ToString() => $"{VmId:n}.{ServiceId:n}";
}