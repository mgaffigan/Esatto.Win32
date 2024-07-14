#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using static Esatto.Win32.Windows.NativeMethods;
using Esatto.Win32.Com;
using System.Drawing;

namespace Esatto.Win32.Windows
{
    public class ShellPreviewControl : Control
    {
        private TypedPreviewerHandle? DisplayedPreviewer;

        public ShellPreviewControl()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Unload();
            }

            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            this.DisplayedPreviewer?.OnResize(ClientRectangle);
        }

        public Exception? LastLoadException { get; private set; }

        public event EventHandler? DisplayedPathChanged;
        private string? _DisplayedPath;
        public string? DisplayedPath
        {
            get { return _DisplayedPath; }
            set
            {
                AssertThreading();
                try
                {
                    if (value == null)
                    {
                        Unload();
                    }
                    else
                    {
                        Load(value);
                    }
                }
                catch (Exception ex)
                {
                    this.LastLoadException = ex;
                }
            }
        }

        public void Load(string path)
        {
            AssertThreading();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(path);
            }

            if (DisplayedPreviewer != null)
            {
                Unload();
            }

            var newType = GetPreviewerClsidForPath(path);
            var newPreviewer = new TypedPreviewerHandle(this, newType);
            try
            {
                newPreviewer.ShowFirstFile(path);
            }
            catch
            {
                newPreviewer.Dispose();
                throw;
            }

            this.DisplayedPreviewer = newPreviewer;
            this._DisplayedPath = path;
            this.LastLoadException = null;
            this.DisplayedPathChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Unload()
        {
            AssertThreading();
            DisplayedPreviewer?.Dispose();
            DisplayedPreviewer = null;
        }

        private void AssertThreading()
        {
            if (InvokeRequired)
            {
                throw new InvalidOperationException("Invalid attempt to access the previewer from a background thread");
            }
        }

        private static Guid GetPreviewerClsidForPath(string path)
        {
            var pBufSize = 1024;
            var pBuf = Marshal.AllocHGlobal(pBufSize);
            try
            {
                int cchPBuf = pBufSize / 2;
                AssocQueryString(
                      ASSOCF_INIT_DEFAULTTOSTAR | ASSOCF_NOTRUNCATE,
                      ASSOCSTR_SHELLEXTENSION, Path.GetExtension(path),
                      typeof(IPreviewHandler).GUID.ToString("B"),
                      pBuf, ref cchPBuf);

                return Guid.Parse(Marshal.PtrToStringUni(pBuf, cchPBuf - 1 /* cch includes null */));
            }
            finally
            {
                Marshal.FreeHGlobal(pBuf);
            }
        }
    }

    internal sealed class TypedPreviewerHandle : IDisposable
    {
        private readonly ShellPreviewControl Parent;
        public readonly Guid CLSID;

        private IPreviewHandler Handler;
        private Stream? OpenStream;

        public TypedPreviewerHandle(ShellPreviewControl target, Guid clsid)
        {
            this.Parent = target;
            this.CLSID = clsid;

            Handler = ComInterop.CreateLocalServer<IPreviewHandler>(clsid);
        }

        public void Dispose()
        {
            if (Handler == null)
            {
                return;
            }

            try
            {
                // excel is ill behaved (100% CPU) when told to unload, but 
                // FinalReleaseComObject does the trick fine.  This seems to
                // only apply when the hosting window disappears before calling Unload.
                //Handler.Unload();
            }
            finally
            {
                var temp = Handler;
                Handler = null!;
                Marshal.FinalReleaseComObject(temp);

                OpenStream?.Dispose();
                OpenStream = null;
            }
        }

        private void AssertAlive()
        {
            if (Handler == null)
            {
                throw new ObjectDisposedException(nameof(ShellPreviewControl));
            }
        }

        public void ShowFirstFile(string path)
        {
            AssertAlive();
            if (OpenStream != null)
            {
                throw new InvalidOperationException("Duplicate call to ShowFirstFile");
            }

            Handler.Initialize(path, out OpenStream);
            var r = Parent.ClientRectangle;
            Handler.SetWindow(Parent.Handle, r);
            Handler.DoPreview();
        }

        public void OnResize(Rectangle clientRectangle)
        {
            AssertAlive();

            var r = Parent.ClientRectangle;
            Handler.SetRect(ref r);
        }
    }
}
