using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    using static ComInterop;
    using static NativeMethods;

    internal sealed class ClassFactory : IClassFactory
    {
        private readonly Func<object> Constructor;
        private readonly Type ClassType;
        private readonly Dictionary<Guid, Type> InterfaceMap;

        public ClassFactory(Type tClass, Func<object> constructor)
        {
            if (constructor == null)
            {
                throw new ArgumentNullException(nameof(constructor), "Contract assertion not met: constructor != null");
            }
            if (tClass == null)
            {
                throw new ArgumentNullException(nameof(tClass), "Contract assertion not met: tClass != null");
            }
            if (!(tClass.IsClass))
            {
                throw new ArgumentException("Contract assertion not met: tClass.IsClass", nameof(tClass));
            }

            this.Constructor = constructor;
            this.ClassType = tClass;

            this.InterfaceMap = new Dictionary<Guid, Type>();
            foreach (var tInt in tClass.GetInterfaces())
            {
                InterfaceMap.Add(tInt.GUID, tInt);
            }
        }

        IntPtr IClassFactory.CreateInstance(IntPtr pUnkOuter, Guid riid)
        {
            if (pUnkOuter != IntPtr.Zero)
            {
                throw GetNoAggregationException();
            }

            var instance = Constructor();
            if (instance == null)
            {
                throw new InvalidOperationException("Constructor returned null");
            }

            Type tInterface;
            if (riid == IID_IUnknown)
            {
                return Marshal.GetIUnknownForObject(instance);
            }
            else if (riid == IID_IDispatch)
            {
                return Marshal.GetIDispatchForObject(instance);
            }
            else if (InterfaceMap.TryGetValue(riid, out tInterface))
            {
                return Marshal.GetComInterfaceForObject(instance, tInterface);
            }
            else throw GetNoInterfaceException();
        }

        void IClassFactory.LockServer(bool fLock)
        {
            // no-op
        }
    }
}
