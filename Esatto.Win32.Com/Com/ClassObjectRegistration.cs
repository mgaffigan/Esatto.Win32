using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    using static NativeMethods;

#if ESATTO_WIN32
    public
#else
    internal
#endif
        sealed class ClassObjectRegistration : IDisposable
    {
        private bool isDisposed;
        private uint cookie;

        public ClassObjectRegistration(Guid clsid, IClassFactory factory, CLSCTX dwClsContext, REGCLS flags)
        {
            cookie = CoRegisterClassObject(clsid, factory, dwClsContext, flags);
        }

        ~ClassObjectRegistration()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            isDisposed = true;

            CoRevokeClassObject(cookie);
        }
    }
}
