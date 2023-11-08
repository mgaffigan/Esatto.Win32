namespace Esatto.Utilities
{
    public static class StreamExtensions
    {
        public static byte[] GetByteArray(this Stream source)
        {
            var msSource = source as MemoryStream;
            if (msSource == null)
            {
                // network stream does not support seek
                // therefore cannot init with capacity
                msSource = source is FileStream fs ? new MemoryStream(checked((int)fs.Length)) : new MemoryStream();
                source.CopyTo(msSource);
            }

            var data = msSource.GetBuffer();
            if (data.Length != msSource.Length)
            {
                Array.Resize(ref data, checked((int)msSource.Length));
            }
            return data;
        }
    }
}
