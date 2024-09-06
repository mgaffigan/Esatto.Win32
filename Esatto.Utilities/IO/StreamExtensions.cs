namespace Esatto.Utilities;

public static class StreamExtensions
{
    public static byte[] GetByteArray(this Stream source)
    {
        if (source is MemoryStream ms) return ms.ToArray();

        // network stream does not support seek
        // therefore cannot init with capacity
        using var msSource = source is FileStream fs ? new MemoryStream(checked((int)fs.Length)) : new MemoryStream();
        source.CopyTo(msSource);
        return msSource.ToArray();
    }

    public static async Task<byte[]> GetByteArrayAsync(this Stream source)
    {
        if (source is MemoryStream ms) return ms.ToArray();

        // network stream does not support seek
        // therefore cannot init with capacity
        using var msSource = source is FileStream fs ? new MemoryStream(checked((int)fs.Length)) : new MemoryStream();
        await source.CopyToAsync(msSource);
        return msSource.ToArray();
    }
}
