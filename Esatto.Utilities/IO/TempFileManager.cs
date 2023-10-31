using Microsoft.Extensions.Logging;

namespace Esatto.Utilities
{
    public class TempFileManager
    {
        private readonly ILogger Logger;

        public string BasePath { get; private set; }
        private const int MAX_PATH = 260;

        public TempFileManager(ILogger logger)
            : this(logger, Path.GetTempPath())
        {
        }

        public TempFileManager(ILogger logger, string basePath)
        {
            if (String.IsNullOrEmpty(basePath))
            {
                throw new ArgumentException("Contract assertion not met: !String.IsNullOrEmpty(basePath)", nameof(basePath));
            }
            if (!Directory.Exists(basePath))
            {
                throw new FileNotFoundException("Base path not found", basePath);
            }

            int fnLength = Guid.NewGuid().ToString("n").Length + 1 /* . */ + 8 /* extension */;
            if (basePath.Length > MAX_PATH - fnLength)
                throw new PathTooLongException("%TEMP% is too long");

            this.Logger = logger;
            this.BasePath = basePath;
        }

        public TempFile GetTempFile()
        {
            return new TempFile(Logger, GetTempFileName("bin"));
        }

        public TempFile GetTempFile(string extension)
        {
            return new TempFile(Logger, GetTempFileName(extension));
        }

        private string GetTempFileName(string extension)
        {
            string rand = Guid.NewGuid().ToString("n");

            string newT = Path.Combine(
                // %APPDATA%\Local\Temp\
                BasePath,
                // {5115FB90-0444-4251-864A-447A8CB3778B}.extension
                rand + "." + extension);

            if (newT.Length > MAX_PATH)
                throw new InvalidOperationException("Extension is too long, total temp path is longer than MAX_PATH");
            return newT;
        }

        public Stream GetTempStream()
        {
            return new FileStream(GetTempFileName(".bin"), FileMode.CreateNew,
                FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
        }

        public async Task<Stream> GetSeekableStream(Stream baseStream)
        {
            using var temp = GetTempStream().MakeUnique();

            // Copy
            await baseStream.CopyToAsync(temp.Value).ConfigureAwait(false);
            temp.Value.Position = 0;

            return temp.Take();
        }

        private sealed class AutoClosingTempFileStream : Stream
        {
            private readonly TempFile File;
            private Stream BaseStream;

            public override bool CanRead => BaseStream.CanRead;

            public override bool CanSeek => BaseStream.CanSeek;

            public override bool CanWrite => BaseStream.CanWrite;

            public override long Length => BaseStream.Length;

            public override long Position
            {
                get { return BaseStream.Position; }
                set { BaseStream.Position = value; }
            }

            public AutoClosingTempFileStream(TempFile file, Stream stream)
            {
                if (file == null)
                {
                    throw new ArgumentNullException(nameof(file), "Contract assertion not met: file != null");
                }

                this.File = file;
                this.BaseStream = stream;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                this.BaseStream.Dispose();
                this.File.Dispose();
            }

            public override void Flush() => BaseStream.Flush();

            public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

            public override void SetLength(long value) => BaseStream.SetLength(value);

            public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);

            public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);
        }
    }
}