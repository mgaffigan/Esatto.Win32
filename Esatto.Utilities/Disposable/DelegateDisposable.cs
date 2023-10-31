namespace Esatto.Utilities
{
    public sealed class DelegateDisposable : IDisposable
    {
        private Action? _Dispose;

        public DelegateDisposable(Action dispose)
        {
            this._Dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        }

        public void Dispose()
        {
            _Dispose?.Invoke();
            _Dispose = null;
        }
    }
}
