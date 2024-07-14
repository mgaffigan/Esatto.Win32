using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.RdpDvc
{
#if NETFRAMEWORK
    [Serializable]
#endif
    public class ChannelNotAvailableException : FileNotFoundException
    {
        public ChannelNotAvailableException() { }
        public ChannelNotAvailableException(string message) : base(message) { }
        public ChannelNotAvailableException(string message, Exception inner) : base(message, inner) { }
#if NETFRAMEWORK
        protected ChannelNotAvailableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif
    }
}
