using Esatto;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Esatto.Utilities
{
    [ImmutableObject(true)]
    public class TempDirectory : IDisposable
    {
        private readonly ILogger Logger;

        private string _Path;

        public string Path
        {
            get
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException("TempDirectory");
                }

                return _Path;
            }
        }

        private bool IsDisposed;

        public TempDirectory(ILogger logger)
            : this(logger, System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()))
        {
        }

        public TempDirectory(ILogger logger, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (Directory.Exists(path))
            {
                throw new ArgumentException("Directory already exists", nameof(path));
            }

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Directory.CreateDirectory(path);
            _Path = path;
        }

        ~TempDirectory()
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
                Directory.Delete(_Path, true);
            }
            catch (Exception ex)
            {
                // only log if we are disposing, if we are running in the finalizer
                // there is really no guarantee made about the state of the logger
                if (isDisposing)
                {
                    Logger.LogWarning(ex, "Unable to delete temporary directory: {Path}", _Path);
                }
            }
        }
    }
}