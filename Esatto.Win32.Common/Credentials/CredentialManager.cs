#nullable enable

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Credentials
{
    public static class CredentialManager
    {
        public static NetworkCredential Read(string targetName)
        {
            if (!CredReadW(targetName, 1 /* CRED_TYPE_GENERIC */, 0, out var cred))
            {
                throw TranslateException(targetName);
            }

            using (cred)
            {
                var credential = Marshal.PtrToStructure<CREDENTIAL>(cred.DangerousGetHandle());
                var cbPassword = credential.CredentialBlobSize / 2 /* sizeof(wchar_t) */;
                var password = Marshal.PtrToStringUni(credential.CredentialBlob, cbPassword);
                return new NetworkCredential(credential.UserName, password);
            }
        }

        public static unsafe void Write(string targetName, string username, string password)
        {
            fixed (char* passwordPtr = password)
            {
                var cred = new CREDENTIAL
                {
                    TargetName = targetName,
                    Type = 1 /* CRED_TYPE_GENERIC */,
                    Persist = 3 /* CRED_PERSIST_ENTERPRISE */,
                    UserName = username,
                    CredentialBlobSize = password.Length * 2 /* sizeof(wchar_t) */,
                    CredentialBlob = (nint)passwordPtr,
                };
                if (!CredWriteW(cred, 0))
                {
                    throw new Win32Exception();
                }
            }
        }

        public static void Delete(string targetName)
        {
            if (!CredDeleteW(targetName, 1 /* CRED_TYPE_GENERIC */, 0))
            {
                throw TranslateException(targetName);
            }
        }

        private static Exception TranslateException(string targetName)
        {
            return Marshal.GetLastWin32Error() switch
            {
                1168 /* ERROR_NOT_FOUND */ => new FileNotFoundException("Could not locate generic credential", targetName),
                _ => new Win32Exception(),
            };
        }

        [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredReadW(
            [MarshalAs(UnmanagedType.LPWStr)] string targetName,
            int credType, int flags, out CredFreeSafeHandle credential);

        [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWriteW(in CREDENTIAL credential, int flags);

        [DllImport("advapi32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDeleteW([MarshalAs(UnmanagedType.LPWStr)] string targetName, int credType, int flags);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            [MarshalAs(UnmanagedType.LPWStr)] public string? TargetName;
            [MarshalAs(UnmanagedType.LPWStr)] public string? Comment;
            public FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)] public string? TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)] public string? UserName;
        }

        private class CredFreeSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public CredFreeSafeHandle()
                : base(true)
            {
            }

            public CREDENTIAL GetCredential() => Marshal.PtrToStructure<CREDENTIAL>(handle);

            protected override bool ReleaseHandle()
            {
                CredFree(handle);
                return true;
            }
        }
    }
}
