using System.Web;

namespace Esatto.Utilities
{
    public static class UriExtensions
    {
#if NET
        public static Uri WithQueryArguments(this Uri uri, Dictionary<string, string> args)
        {
            var urib = new UriBuilder(uri);
            var collection = HttpUtility.ParseQueryString(string.Empty);
            foreach (var a in args)
            {
                collection[a.Key] = a.Value;
            }
            urib.Query = collection.ToString();
            return urib.Uri;
        }
#endif
    }
}