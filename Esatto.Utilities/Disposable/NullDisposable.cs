using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Utilities
{
    public sealed class NullDisposable : IDisposable
    {
        public static IDisposable Instance { get; } = new NullDisposable();
        public void Dispose()
        {
            // nop
        }
    }
}
