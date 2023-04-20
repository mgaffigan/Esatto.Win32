using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Esatto.Win32.CommonControls.Net
{
    class NativeMethods
    {
        const string
            KERNEL32 = "kernel32.dll",
            Mpr = "mpr.dll";
        private const int
            ERROR_MORE_DATA = 234,
            NO_ERROR = 0,
            RESOURCETYPE_DISK = 1,
            CONNECT_TEMPORARY = 0x00000004;

        [StructLayout(LayoutKind.Sequential)]
        struct NETRESOURCE
        {
            public int Scope;
            public int Type;
            public int DisplayType;
            public int Usage;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string LocalName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string RemoteName;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string Provider;
        }

        [DllImport(Mpr, CharSet = CharSet.Auto)]
        private static extern int WNetAddConnection2(
            ref NETRESOURCE netResource,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            [MarshalAs(UnmanagedType.LPWStr)] string username,
            int flags
        );

        public static void MapNetworkDrive(string uncPath, string localName, string username, string password)
        {
            var res = new NETRESOURCE();
            res.Type = RESOURCETYPE_DISK;
            res.RemoteName = uncPath;

            var result = WNetAddConnection2(ref res, password, username, CONNECT_TEMPORARY);
            if (result != NO_ERROR)
            {
                throw new Win32Exception(result);
            }
        }

        [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool GetComputerNameEx(
            [In] ComputerNameFormat nameType,
            [In, Out, MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpBuffer,
            [In, Out] ref int size);

        public static string GetComputerName(ComputerNameFormat nameType)
        {
            int length = 0;
            if (!GetComputerNameEx(nameType, null, ref length))
            {
                int error = Marshal.GetLastWin32Error();

                if (error != ERROR_MORE_DATA)
                {
                    throw new System.ComponentModel.Win32Exception(error);
                }
            }

            if (length < 0)
            {
                throw new InvalidOperationException("GetComputerName returned an invalid length: " + length);
            }

            StringBuilder stringBuilder = new StringBuilder(length);
            if (!GetComputerNameEx(nameType, stringBuilder, ref length))
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error);
            }

            return stringBuilder.ToString();
        }

        public enum ComputerNameFormat
        {
            NetBIOS,
            DnsHostName,
            Dns,
            DnsFullyQualified,
            PhysicalNetBIOS,
            PhysicalDnsHostName,
            PhysicalDnsDomain,
            PhysicalDnsFullyQualified
        }
    }
}
