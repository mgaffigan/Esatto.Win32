using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Esatto.Win32.NetInjector
{
    public static class Injector
    {
        /// <summary>
        /// Run the specified method in the target process using the specified runtime version.
        /// </summary>
        /// <param name="hWnd">Any HWND owned by the target process</param>
        /// <param name="entryPoint">The entry-point of the module to be loaded</param>
        /// <param name="argValue">An optional argument to be passed to the entry-point.  May not contain embedded null characters.</param>
        /// <param name="runtimeVersion">The version of the runtime to select or path to a *.runtimeconfig.json file</param>
        /// <exception cref="ArgumentNullException">One of the required arguments is null</exception>
        /// <exception cref="ArgumentException">The arguments contain embedded null characters</exception>
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

            // Auto-detect version for .NET Core
            if (runtimeVersion is null)
            {
                runtimeVersion = GetDefaultRuntimeVersion(entryPoint.AssemblyPath);
            }

            RunAssemblyRemote(
                hWnd, runtimeVersion,
                entryPoint.AssemblyPath, entryPoint.TypeName, entryPoint.MethodName,
                argValue
            );
        }

        private static string? GetDefaultRuntimeVersion(string assemblyPath)
        {
            var pathBase = assemblyPath.Substring(0, assemblyPath.Length - 4);

            var runtimeConfig = pathBase + ".runtimeconfig.json";
            if (File.Exists(runtimeConfig))
            {
                return runtimeConfig;
            }

            var depsJson = pathBase + ".deps.json";
            if (File.Exists(depsJson))
            {
                return RuntimeVersions.NetCoreAny;
            }

            return RuntimeVersions.NetFxAny;
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
