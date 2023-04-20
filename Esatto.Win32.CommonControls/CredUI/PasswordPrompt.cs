using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Esatto.Win32.CommonControls.CredUI;

namespace Esatto.Win32.CommonControls
{
    public static class PasswordPrompt
    {
        public static bool ShowDialog(
            IntPtr ParentWindowHandle,
            string title,
            string message,
            string target,
            ref string username,
            ref string password)
        {
            //allocate memory for returns
            StringBuilder sbUsername = new StringBuilder(NativeMethods.CRED_MAX_USERNAME_LENGTH);
            if (username != null)
            {
                sbUsername.Append(username);
            }
            //bad, bad, bad... cleartext passwords (WCF doesn't support securestring)
            StringBuilder sbPassword = new StringBuilder(NativeMethods.CREDUI_MAX_PASSWORD_LENGTH);
            if (password != null)
            {
                sbPassword.Append(password);
            }

            //setup args
            var credInfo = new NativeMethods.CREDUI_INFO()
            {
                hwndParent = ParentWindowHandle,
                hbmBanner = IntPtr.Zero,
                pszCaptionText = title,
                pszMessageText = message
            };
            credInfo.cbSize = Marshal.SizeOf(credInfo);
            bool save = false;

            var ret = NativeMethods.CredUIPromptForCredentials(
                ref credInfo, target, IntPtr.Zero, 0,
                sbUsername, NativeMethods.CRED_MAX_USERNAME_LENGTH,
                sbPassword, NativeMethods.CREDUI_MAX_PASSWORD_LENGTH,
                ref save, NativeMethods.CREDUI_FLAGS.GENERIC_CREDENTIALS
                | NativeMethods.CREDUI_FLAGS.ALWAYS_SHOW_UI 
                | NativeMethods.CREDUI_FLAGS.DO_NOT_PERSIST
                | NativeMethods.CREDUI_FLAGS.EXCLUDE_CERTIFICATES);

            username = sbUsername.ToString();
            password = sbPassword.ToString();

            if (ret == NativeMethods.CredUIReturnCodes.NO_ERROR)
                return true;
            else if (ret == NativeMethods.CredUIReturnCodes.ERROR_CANCELLED)
                return false;
            else
                throw new Exception(ret.ToString());
        }
    }
}
