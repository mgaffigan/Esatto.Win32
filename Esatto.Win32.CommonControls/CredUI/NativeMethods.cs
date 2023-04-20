using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.CompilerServices;

namespace Esatto.Win32.CommonControls.CredUI
{
    static class NativeMethods
    {
        public const int CRED_MAX_USERNAME_LENGTH = 513,
            CRED_MAX_CREDENTIAL_BLOB_SIZE = 512,
            CREDUI_MAX_PASSWORD_LENGTH = CRED_MAX_CREDENTIAL_BLOB_SIZE / 2;

        [DllImport("credui", CharSet=CharSet.Unicode)]
        public static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO creditUR,
          string targetName,
          IntPtr reserved1,
          int iError,
          StringBuilder userName,
          int maxUserName,
          StringBuilder password,
          int maxPassword,
          [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
          CREDUI_FLAGS flags);

        [Flags]
        public enum CREDUI_FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        public enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }
    }
}
