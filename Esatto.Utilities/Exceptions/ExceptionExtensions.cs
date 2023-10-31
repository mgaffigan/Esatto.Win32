namespace Esatto.Utilities
{
    public static class ExceptionExtensions
    {
        public static Exception GetRealException(this Exception ex)
        {
            if (ex is AggregateException ae)
            {
                var flattened = ae.Flatten();
                if (flattened.InnerExceptions.Count == 1)
                {
                    return flattened.InnerExceptions[0];
                }
            }

            return ex;
        }

        public static Exception GetException(this IReadOnlyList<Exception> exs) 
        {
            return exs.Count > 1 ? new AggregateException(exs) : exs.Single();
        }

        public static void ThrowIfNotEmpty(this IReadOnlyList<Exception> exs)
        {
            if (exs.Any())
            {
                throw exs.GetException();
            }
        }
    }
}
