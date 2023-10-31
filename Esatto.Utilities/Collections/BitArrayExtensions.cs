using System.Collections;

namespace BitArrayExtensions
{
    public static class BitArrayExtensionsClass
    {
        public static byte[] GetBytes(this BitArray ba)
        {
            byte[] bytes = new byte[(ba.Length + 7) / 8];
            ba.CopyTo(bytes, 0);
            return bytes;
        }
    }
}
