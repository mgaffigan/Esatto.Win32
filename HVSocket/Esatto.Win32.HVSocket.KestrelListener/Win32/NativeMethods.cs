using System.Net.Sockets;

namespace Esatto.Win32.HVSocket;

internal static class NativeMethods
{
    public const ProtocolType HV_PROTOCOL_RAW = (ProtocolType)1;
    public const AddressFamily AF_HYPERV = (AddressFamily)34;
    public const int HYPERV_SOCK_ADDR_SIZE = 36;
}
