using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


// https://github.com/dotnet/runtime/issues/94749
namespace DotnetRuntimeIssue94749
{
#if !NET

    [ComVisible(true)]
    // netfx does not have this issue, proxy real impl.
    public class StandardOleMarshalObject : System.Runtime.InteropServices.StandardOleMarshalObject
    {
    }

#else

    // This is the implementation from the repository, just with [ComVisible(true)] added
    [ComVisible(true)]
    public class StandardOleMarshalObject : MarshalByRefObject, IMarshal
    {
        private static readonly Guid CLSID_StdMarshal = new Guid("00000017-0000-0000-c000-000000000046");

        protected StandardOleMarshalObject()
        {
        }

        [DllImport("ole32.dll")]
        internal static extern int CoGetStandardMarshal(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out IntPtr ppMarshal);

        private IntPtr GetStdMarshaler(ref Guid riid, int dwDestContext, int mshlflags)
        {
            IntPtr pUnknown = Marshal.GetIUnknownForObject(this);
            if (pUnknown != IntPtr.Zero)
            {
                try
                {
                    IntPtr pStandardMarshal = IntPtr.Zero;
                    int hr = CoGetStandardMarshal(ref riid, pUnknown, dwDestContext, IntPtr.Zero, mshlflags, out pStandardMarshal);
                    if (hr == 0)
                    {
                        Debug.Assert(pStandardMarshal != IntPtr.Zero, $"Failed to get marshaler for interface '{riid}', CoGetStandardMarshal returned S_OK");
                        return pStandardMarshal;
                    }
                }
                finally
                {
                    Marshal.Release(pUnknown);
                }
            }

            throw new InvalidOperationException($"Failed to get marshaler for interface '{riid}'");
        }

        int IMarshal.GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid)
        {
            pCid = CLSID_StdMarshal;
            return 0;
        }

        unsafe int IMarshal.GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize)
        {
            IntPtr pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // We must not wrap pStandardMarshal with an RCW because that
                // would trigger QIs for random IIDs and the marshaler (aka stub
                // manager object) does not really handle these well and we would
                // risk triggering an AppVerifier break
                fixed (Guid* riidPtr = &riid)
                fixed (int* pSizePtr = &pSize)
                {
                    // GetMarshalSizeMax is 5th slot (zero-based indexing)
                    return ((delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr, int, IntPtr, int, int*, int>)(*(IntPtr**)pStandardMarshal)[4])(pStandardMarshal, riidPtr, pv, dwDestContext, pvDestContext, mshlflags, pSizePtr);
                }
            }
            finally
            {
                Marshal.Release(pStandardMarshal);
            }
        }

        unsafe int IMarshal.MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags)
        {
            IntPtr pStandardMarshal = GetStdMarshaler(ref riid, dwDestContext, mshlflags);

            try
            {
                // We must not wrap pStandardMarshal with an RCW because that
                // would trigger QIs for random IIDs and the marshaler (aka stub
                // manager object) does not really handle these well and we would
                // risk triggering an AppVerifier break
                fixed (Guid* riidPtr = &riid)
                {
                    // MarshalInterface is 6th slot (zero-based indexing)
                    return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, IntPtr, int, IntPtr, int, int>)(*(IntPtr**)pStandardMarshal)[5])(pStandardMarshal, pStm, riidPtr, pv, dwDestContext, pvDestContext, mshlflags);
                }
            }
            finally
            {
                Marshal.Release(pStandardMarshal);
            }
        }

        int IMarshal.UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::UnmarshalInterface should not be called.");
            ppv = IntPtr.Zero;
            return unchecked((int)0x80004001 /* E_NOTIMPL */);
        }

        int IMarshal.ReleaseMarshalData(IntPtr pStm)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::ReleaseMarshalData should not be called.");
            return unchecked((int)0x80004001 /* E_NOTIMPL */);
        }

        int IMarshal.DisconnectObject(int dwReserved)
        {
            // this should never be called on this interface, but on the standard one handed back by the previous calls.
            Debug.Fail("IMarshal::DisconnectObject should not be called.");
            return unchecked((int)0x80004001 /* E_NOTIMPL */);
        }
    }

    [ComImport]
    [Guid("00000003-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMarshal
    {
        [PreserveSig]
        int GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid);
        [PreserveSig]
        int GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize);
        [PreserveSig]
        int MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags);
        [PreserveSig]
        int UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv);
        [PreserveSig]
        int ReleaseMarshalData(IntPtr pStm);
        [PreserveSig]
        int DisconnectObject(int dwReserved);
    }
#endif
}
