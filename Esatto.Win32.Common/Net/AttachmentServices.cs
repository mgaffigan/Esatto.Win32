using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Net
{
    public static class AttachmentServices
    {
        public static void SetMarkOfTheWeb(string path, Guid client, Uri source)
        {
            var service = (IAttachmentExecute)new CAttachmentServices();
            service.SetClientGuid(client);
            service.SetLocalPath(path);
            service.SetSource(source.ToString());
            service.Save();
        }
    }

    [ComImport, Guid("4125dd96-e03a-4103-8f70-e0597d803b9c")]
    internal class CAttachmentServices
    {
    }

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("73db1241-1e85-4581-8e4f-a81e1d0f8c57")]
    internal interface IAttachmentExecute
    {
        // we need Guid, LocalPath, Source, Save

        void _VtblGap1_1();
        // HRESULT SetClientTitle(_In_  LPCWSTR pszTitle);

        void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);

        void SetLocalPath([MarshalAs(UnmanagedType.LPWStr)] string pszLocalPath);

        void _VtblGap2_1();
        // HRESULT SetFileName(_In_  LPCWSTR pszFileName);

        void SetSource([MarshalAs(UnmanagedType.LPWStr)] string pszSource);

        void _VtblGap3_3();
        // HRESULT SetReferrer(_In_  LPCWSTR pszReferrer);
        // HRESULT CheckPolicy();
        // HRESULT Prompt(
        //    _In_  HWND hwnd,
        //    _In_  ATTACHMENT_PROMPT prompt,
        //    _Out_  ATTACHMENT_ACTION* paction);

        void Save(); 

        void _VtblGap4_3();
        // HRESULT Execute(
        //    _In_  HWND hwnd,
        //    _In_opt_  LPCWSTR pszVerb,
        //    _Out_opt_  HANDLE* phProcess);
        // HRESULT SaveWithUI(_In_  HWND hwnd);
        // HRESULT ClearClientState();
    }
}
