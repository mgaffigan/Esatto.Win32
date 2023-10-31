using System.Text;

namespace Esatto.Utilities
{
    public static class ByteArrayExtensions
    {
        public static string ToHexString(this byte[] array)
        {
#if NET
            return Convert.ToHexString(array);
#else
            var sb = new StringBuilder(array.Length * 2);

            foreach (byte b in array)
            {
                // can be "x2" if you want lowercase
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
#endif
        }
    }
}
