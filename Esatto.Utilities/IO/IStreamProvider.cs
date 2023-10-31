namespace Esatto.Utilities
{
    public interface IStreamProvider : IDisposable
    {
        Stream GetReadStream();
    }
}
