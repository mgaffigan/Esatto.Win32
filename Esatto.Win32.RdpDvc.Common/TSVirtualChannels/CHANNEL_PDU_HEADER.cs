#pragma warning disable CA1707 // Identifiers should not contain underscores: PInvoke keeping the name of native type
#pragma warning disable CA1051 // Do not declare visible instance fields: PInvoke

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc.TSVirtualChannels
{
    public struct CHANNEL_PDU_HEADER
    {
        public int length;
        public CHANNEL_FLAG flags;

        public static CHANNEL_PDU_HEADER FromBuffer(byte[] data, int offset)
        {
            return new CHANNEL_PDU_HEADER
            {
                length = BitConverter.ToInt32(data, offset),
                flags = (CHANNEL_FLAG)BitConverter.ToInt32(data, offset + 4)
            };
        }
    }
}
