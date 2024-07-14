using System;

namespace Esatto.Win32.RdpDvc
{
#if NETFRAMEWORK
    [Serializable]
#endif
    public class DvcChannelDisconnectedException : InvalidOperationException
    {
        public DvcChannelDisconnectedException() : this("Other end of DVC has disconnected") { }
        public DvcChannelDisconnectedException(string message) : base(message) { }
        public DvcChannelDisconnectedException(string message, Exception inner) : base(message, inner) { }
#if NETFRAMEWORK
        protected DvcChannelDisconnectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
