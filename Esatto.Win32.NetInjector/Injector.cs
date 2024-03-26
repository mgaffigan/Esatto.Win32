using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.NetInjector
{
    public static class Injector
    {
        public static void Inject(IntPtr hWnd, EntryPointReference entryPoint, string? argValue = null, string? runtimeVersion = null)
        {
            RunAssemblyRemote(
                hWnd, runtimeVersion,
                entryPoint.AssemblyPath, entryPoint.TypeName, entryPoint.MethodName,
                argValue
            );
        }

        [DllImport("Esatto.Win32.NetInjector.NetFx.dll", ExactSpelling = true, PreserveSig = false)]
        private static extern void RunAssemblyRemote(
            IntPtr hWnd,
            [MarshalAs(UnmanagedType.LPWStr)] string? pwzPreferredVersion,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzAssemblyPath,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzTypeName,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzMethodName,
            [MarshalAs(UnmanagedType.LPWStr)] string? pwzArgument
        );
    }
}
