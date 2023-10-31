using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Esatto.Utilities
{
    public class TempFile : IDisposable
    {
        private string _Path;
        private readonly ILogger Logger;

        public string Path
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException("TempFile");
                }

                return _Path;
            }
#if NET
            [MemberNotNull(nameof(_Path), nameof(Info))]
#endif
            private set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                this._Path = value;
                this.Info = new FileInfo(value);
            }
        }

        public FileInfo Info { get; private set; }

        private bool IsDisposed;

        public TempFile(ILogger logger, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.Logger = logger;
            this.Path = path;
        }

        ~TempFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            try
            {
                File.Delete(_Path);
            }
            catch (Exception ex)
            {
                // only log if we are disposing, if we are running in the finalizer
                // there is really no guarantee made about the state of the logger
                if (isDisposing)
                {
                    Logger.LogWarning(ex, "Unable to delete temporary file: {Path}", _Path);
                }
            }
        }

        private sealed class TempFileStreamProvider : IStreamProvider
        {
            private TempFile Parent;
            private readonly bool RefCount;

            public TempFileStreamProvider(TempFile tf, bool refCount)
            {
                this.Parent = tf;
                this.RefCount = refCount;
            }

            public void Dispose()
            {
                if (RefCount)
                {
                    Parent?.DisposeTempFileStream();
                }
            }

            public Stream GetReadStream()
            {
                var parent = Parent;
                if (parent == null)
                {
                    throw new ObjectDisposedException(nameof(TempFileStreamProvider));
                }
                return parent.GetReadStream();
            }
        }

        private int TempFileStreamProviderCount;

        private void DisposeTempFileStream()
        {
            var after = Interlocked.Decrement(ref TempFileStreamProviderCount);
            if (after == 0)
            {
                Dispose();
            }
        }

        public IStreamProvider GetStreamProvider(bool disposeTempFile)
        {
            if (disposeTempFile)
            {
                Interlocked.Increment(ref TempFileStreamProviderCount);
            }
            return new TempFileStreamProvider(this, disposeTempFile);
        }

        public FileStream GetWriteStream()
        {
            return new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public FileStream GetReadStream()
        {
            return new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}
