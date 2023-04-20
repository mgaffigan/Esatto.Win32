using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Users
{
    public static class Username
    {
        public static string SamCompatible => NativeMethods.GetSamCompatibleName();

        public static string UserPrincipalName => NativeMethods.GetUserPrincipalName();

        public static string DisplayName => NativeMethods.GetDisplayName();
    }
}
