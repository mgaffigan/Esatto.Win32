using System.Runtime.InteropServices;
using System.Security;

namespace Esatto.Utilities
{
    public static class SecureStringUtilities
    {
        public static string GetUnsecureString(this SecureString securePassword)
        {
            if (securePassword == null)
            {
                throw new ArgumentNullException(nameof(securePassword));
            }

            var unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
            try
            {
                return Marshal.PtrToStringUni(unmanagedString)!;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
