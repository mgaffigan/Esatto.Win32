using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

using static Esatto.Win32.Com.NativeMethods;

namespace Esatto.Win32.Com;

internal static partial class NativeMethods
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ACTCTX
    {
        public int cbSize;
        public int dwFlags;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpSource;
        public ushort wProcessorArchitecture;
        public ushort wLangId;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpAssemblyDirectory;
        public IntPtr lpResourceName;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpApplicationName;
        public IntPtr hModule;
    }

    [DllImport("KERNEL32.dll", ExactSpelling = true, EntryPoint = "CreateActCtxW", SetLastError = true)]
    internal static extern unsafe ActCtxSafeHandle CreateActCtx(in ACTCTX pActCtx);

    [DllImport("KERNEL32.dll", ExactSpelling = true)]
    internal static extern void ReleaseActCtx(IntPtr hActCtx);

    [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern unsafe bool ActivateActCtx(ActCtxSafeHandle hActCtx, out IntPtr lpCookie);

    [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeactivateActCtx(int dwFlags, IntPtr ulCookie);

    internal class ActCtxSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal ActCtxSafeHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            ReleaseActCtx(this.handle);
            return true;
        }
    }
}

public sealed class ActivationContext : IDisposable
{
    private readonly ActCtxSafeHandle Handle;

    internal ActivationContext(ActCtxSafeHandle handle)
    {
        this.Handle = handle;
    }

    // Resource ID 2 is default for .Net assemblies
    // https://learn.microsoft.com/en-us/windows/win32/sbscs/using-side-by-side-assemblies-as-a-resource?redirectedfrom=MSDN
    public static ActivationContext CreateFromAssembly(Assembly asm, int resourceId = 2)
        => CreateFromPath(asm.Location, resourceId);

    public static ActivationContext CreateFromPath(string path, int resourceId = 0)
    {
        var req = new ACTCTX
        {
            cbSize = Marshal.SizeOf<ACTCTX>(),
            dwFlags = resourceId == 0 ? 0 : 0x00000008 /* ACTCTX_FLAG_RESOURCE_NAME_VALID */,
            lpSource = path,
            lpResourceName = (IntPtr)resourceId,
        };
        var handle = CreateActCtx(in req);
        if (handle.IsInvalid)
        {
            throw new Win32Exception();
        }
        return new ActivationContext(handle);
    }

    /// <summary>
    /// Activates an activation context by pushing it onto the stack and returning
    /// a disposable to pop it.
    /// </summary>
    /// <returns>Disposable which will call DeactivateActCtx</returns>
    /// <exception cref="Win32Exception">If the call to ActivateActCtx fails</exception>
    public IDisposable Enter()
    {
        if (!ActivateActCtx(this.Handle, out var cookie))
        {
            throw new Win32Exception();
        }
        return new ContextCookie(cookie);
    }

    class ContextCookie : IDisposable
    {
        private readonly IntPtr Cookie;

        public ContextCookie(IntPtr cookie)
        {
            this.Cookie = cookie;
        }

        public void Dispose()
        {
            if (!DeactivateActCtx(0, this.Cookie))
            {
                throw new Win32Exception();
            }
        }
    }

    public void Dispose()
    {
        this.Handle.Dispose();
    }
}

public static partial class AssemblyExtensions
{
    public static ActivationContext CreateActivationContext(this Assembly asm, int resourceId = 2)
        => ActivationContext.CreateFromAssembly(asm, resourceId);
}