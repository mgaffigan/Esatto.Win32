namespace Esatto.Utilities
{
    public sealed class ThreadAssert
    {
        private readonly Thread OriginalThread;

        public ThreadAssert()
        {
            this.OriginalThread = Thread.CurrentThread;
        }

        public void Assert()
        {
            if (this.OriginalThread != Thread.CurrentThread)
            {
                throw new InvalidOperationException("Cross thread access to object");
            }
        }
    }
}
