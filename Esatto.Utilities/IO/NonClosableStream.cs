namespace Esatto.Utilities
{
    public sealed class NonClosableStream : Stream
    {
        private readonly Stream BaseStream;

        public NonClosableStream(Stream baseStream)
        {
            if (baseStream == null)
            {
                throw new ArgumentNullException(nameof(baseStream), "Contract assertion not met: baseStream != null");
            }

            this.BaseStream = baseStream;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // we DO NOT dispose BaseStream, as that is the whole point.
        }

        public override bool CanRead => BaseStream.CanRead;

        public override bool CanSeek => BaseStream.CanSeek;

        public override bool CanWrite => BaseStream.CanWrite;

        public override long Length => BaseStream.Length;

        public override long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public override void Flush() => BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) => BaseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);

        public override void SetLength(long value) => BaseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => BaseStream.Write(buffer, offset, count);
    }
}
