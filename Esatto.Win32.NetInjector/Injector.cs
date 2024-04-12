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
            // struct, so TypeName might be null despite the constructor
            if (entryPoint.TypeName is null)
            {
                throw new ArgumentNullException(nameof(entryPoint));
            }
            if (argValue is not null && argValue.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("argValue cannot contain null characters", nameof(argValue));
            }
            if (runtimeVersion is not null && runtimeVersion.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("runtimeVersion cannot contain null characters", nameof(runtimeVersion));
            }

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
