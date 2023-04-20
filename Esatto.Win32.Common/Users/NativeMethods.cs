using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Esatto.Win32.Users
{
    internal class NativeMethods
    {
        #region Interop Definitions
        private enum EXTENDED_NAME_FORMAT
        {
            NameUnknown = 0,
            NameFullyQualifiedDN = 1,
            NameSamCompatible = 2,
            NameDisplay = 3,
            NameUniqueId = 6,
            NameCanonical = 7,
            NameUserPrincipal = 8,
            NameCanonicalEx = 9,
            NameServicePrincipal = 10,
            NameDnsDomain = 12
        }

        [DllImport("secur32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern int GetUserNameEx(EXTENDED_NAME_FORMAT nameFormat, StringBuilder userName, ref int userNameSize);

        #endregion

        private static string GetUserName(EXTENDED_NAME_FORMAT nameFormat)
        {
            StringBuilder userName = new StringBuilder(1024);
            int userNameSize = userName.Capacity;

            if (GetUserNameEx(nameFormat, userName, ref userNameSize) == 0)
                throw new Win32Exception();
            if (string.IsNullOrWhiteSpace(userName.ToString()))
            {
                throw new InvalidOperationException("No name available");
            }

            return userName.ToString();
        }

        public static string GetUserFullName() => GetUserName(EXTENDED_NAME_FORMAT.NameDisplay);

        public static string GetSamCompatibleName() => GetUserName(EXTENDED_NAME_FORMAT.NameSamCompatible);

        public static string GetUserPrincipalName() => GetUserName(EXTENDED_NAME_FORMAT.NameUserPrincipal);

        public static string GetDisplayName() => GetUserName(EXTENDED_NAME_FORMAT.NameDisplay);
    }
}