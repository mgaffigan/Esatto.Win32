namespace Esatto.Utilities
{
    public static class StreamExtensions
    {
        public static byte[] GetByteArray(this Stream source)
        {
            var msSource = source as MemoryStream;
            if (msSource == null)
            {
                // network stream does not suppory seek
                // therefore cannot init with capacity
                msSource = new MemoryStream();
                source.CopyTo(msSource);
            }

            var data = msSource.ToArray();
            return data;
        }
    }
}
