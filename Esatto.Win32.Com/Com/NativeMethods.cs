using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Com
{
    internal static class NativeMethods
    {
        private const string Ole32 = "ole32.dll";
        private const string User32 = "user32.dll";

        public const int ROTFLAGS_REGISTRATIONKEEPSALIVE = 1;
        public const int ROTFLAGS_ALLOWANYCLIENT = 2;
        public const string IID_IClassFactory = "00000001-0000-0000-C000-000000000046";
        public static readonly Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
        public static readonly Guid IID_IDispatch = new Guid("00020400-0000-0000-C000-000000000046");
        public const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
        public const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
        public const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
        public const int RPC_E_CANTCALLOUT_ININPUTSYNCCALL = unchecked((int)0x8001010D);
        public const int E_NOINTERFACE = unchecked((int)0x80004002);
        public const int MK_E_UNAVAILABLE = unchecked((int)0x800401E3);
        public const int S_OK = 0;
        public const int ASFW_ANY = -1;

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CoResumeClassObjects();

        [DllImport(Ole32, PreserveSig = false)]
        public static extern void CoRevokeClassObject(uint dwRegister);

        [DllImport(Ole32, PreserveSig = false)]
        public static extern uint CoRegisterClassObject(
            [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
            [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
            CLSCTX dwClsContext, REGCLS flags);

        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        public static extern IRunningObjectTable GetRunningObjectTable(int reserved);

        [DllImport(Ole32, CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        public static extern IMoniker CreateItemMoniker([In] string lpszDelim, [In] string lpszItem);

        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        [return: MarshalAs(UnmanagedType.IUnknown)]
        public static extern object CoCreateInstance(
           [In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
           [MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter,
           CLSCTX dwClsContext,
           [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid);

        [DllImport(Ole32, CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void CLSIDFromProgID(string progId, out Guid rclsid);

        [DllImport(User32, SetLastError = true, ExactSpelling = true)]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        public static extern void CoAllowSetForegroundWindow([MarshalAs(UnmanagedType.IUnknown)] object unk, IntPtr reserved);

        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        public static extern void CoInitializeSecurity([MarshalAs(UnmanagedType.LPStruct)] Guid pSecDesc, int cAuthSvc, IntPtr asAuthSvc, IntPtr pReserved1, RpcAuthnLevel level, RpcImpLevel impers, IntPtr pAuthList, EoAuthnCap dwCapabilities, IntPtr pReserved3);
        
        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        public static extern void CoImpersonateClient();

        [DllImport(Ole32, ExactSpelling = true, PreserveSig = false)]
        public static extern void CoRevertToSelf();

        public enum RpcAuthnLevel
        {
            Default = 0,
            None = 1,
            Connect = 2,
            Call = 3,
            Pkt = 4,
            PktIntegrity = 5,
            PktPrivacy = 6
        }

        public enum RpcImpLevel
        {
            Default = 0,
            Anonymous = 1,
            Identify = 2,
            Impersonate = 3,
            Delegate = 4
        }

        public enum EoAuthnCap
        {
            None = 0x00,
            MutualAuth = 0x01,
            StaticCloaking = 0x20,
            DynamicCloaking = 0x40,
            AnyAuthority = 0x80,
            MakeFullSIC = 0x100,
            Default = 0x800,
            SecureRefs = 0x02,
            AccessControl = 0x04,
            AppID = 0x08,
            Dynamic = 0x10,
            RequireFullSIC = 0x200,
            AutoImpersonate = 0x400,
            NoCustomMarshal = 0x2000,
            DisableAAA = 0x1000
        }
    }
}
