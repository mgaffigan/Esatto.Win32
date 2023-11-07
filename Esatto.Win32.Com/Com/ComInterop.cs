using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using static NativeMethods;

#if ESATTO_WIN32
    public
#else
    internal
#endif
        static class ComInterop
    {
        public const int RPC_S_SERVER_UNAVAILABLE = NativeMethods.RPC_S_SERVER_UNAVAILABLE;

        public static Exception GetNoAggregationException() => Marshal.GetExceptionForHR(CLASS_E_NOAGGREGATION);

        public static Exception GetNoInterfaceException() => Marshal.GetExceptionForHR(E_NOINTERFACE);

        public static void SetAppId(Guid appId)
        {
            CoInitializeSecurity(appId, -1, IntPtr.Zero, IntPtr.Zero,
                RpcAuthnLevel.Pkt, RpcImpLevel.Identify, IntPtr.Zero,
                EoAuthnCap.AppID, IntPtr.Zero);
        }

        public static IClassFactory CreateClassFactoryFor<TClass>(Func<TClass> constructor)
        {
            return new ClassFactory(typeof(TClass), () => constructor());
        }

        public static IClassFactory CreateStaClassFactoryFor<TClass>(Func<TClass> constructor)
        {
            return new StaClassFactory(typeof(TClass), () => constructor());
        }

        public static object CreateLocalServer(Guid clsid)
        {
            return CoCreateInstance(clsid, null, CLSCTX.LOCAL_SERVER, IID_IUnknown);
        }

        public static TInterface CreateLocalServer<TInterface>(Guid clsid)
        {
            return (TInterface)CoCreateInstance(clsid, null, CLSCTX.LOCAL_SERVER, typeof(TInterface).GUID);
        }

        public static object CreateLocalServer(string progid)
        {
            if (string.IsNullOrEmpty(progid))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrEmpty(progid)", nameof(progid));
            }

            Guid clsid;
            CLSIDFromProgID(progid, out clsid);
            return CreateLocalServer(clsid);
        }

        public static TInterface CreateLocalServer<TInterface>(string progid)
        {
            if (string.IsNullOrEmpty(progid))
            {
                throw new ArgumentException("Contract assertion not met: !string.IsNullOrEmpty(progid)", nameof(progid));
            }

            Guid clsid;
            CLSIDFromProgID(progid, out clsid);
            return CreateLocalServer<TInterface>(clsid);
        }

        public static void CoAllowSetForegroundWindow(object obj) => NativeMethods.CoAllowSetForegroundWindow(obj, IntPtr.Zero);
        public static void AllowSetForegroundWindow(int pid) => NativeMethods.AllowSetForegroundWindow(pid);
        public static void AllowSetForegroundWindow() => NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);

        public static void CoResumeClassObjects() => NativeMethods.CoResumeClassObjects();

        public static void RunImpersonated(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "Contract assertion not met: action != null");
            }

            try { /* cer */}
            finally
            {
                bool isReverted = false;
                CoImpersonateClient();
                try
                {
                    action();
                }
                // we must use catch to prevent exceptions from hitting code outside
                // of the "RunImpersonated" actor
                catch
                {
                    CoRevertToSelf();
                    isReverted = true;
                }
                finally
                {
                    if (!isReverted)
                    {
                        CoRevertToSelf();
                    }
                }
            }
        }

        public static bool IsEmbedding(string[] args) => args.Any(a => string.Equals(a, "-embedding", StringComparison.OrdinalIgnoreCase));
    }
}
