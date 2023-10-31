using System.IO.Compression;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Esatto.Utilities
{
    public sealed class CompressedXmlIntSet : HashSet<int>, IXmlSerializable
    {
        #region ctor

        public CompressedXmlIntSet() : base() { }

        public CompressedXmlIntSet(IEnumerable<int> collection) : base(collection) { }

        #endregion

        #region Compression

        private static string EncodeRleIntList(IEnumerable<int> values)
        {
            values = values.Distinct().OrderBy(a => a).ToArray();

            using (var compressedStream = new MemoryStream())
            {
                using (var gzStream = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    int last = 0;
                    int rle_count = 0;
                    foreach (var id in values)
                    {
                        if (id == last + 1)
                        {
                            rle_count += 1;
                            last = id;
                        }
                        else
                        {
                            FlushPendingRle(gzStream, ref rle_count);

                            uint eff = (uint)id;
                            WriteByteToRleStream(gzStream, checked((byte)((eff >> 0) & 0xff)));
                            gzStream.WriteByte(checked((byte)((eff >> 8) & 0xff)));
                            gzStream.WriteByte(checked((byte)((eff >> 16) & 0xff)));
                            gzStream.WriteByte(checked((byte)((eff >> 24) & 0xff)));
                            last = id;
                        }
                    }
                    FlushPendingRle(gzStream, ref rle_count);
                }
                return Convert.ToBase64String(compressedStream.ToArray());
            }
        }

        private static void FlushPendingRle(Stream s, ref int rle_count)
        {
            while (rle_count > 0)
            {
                s.WriteByte(RLE_SENTINEL);

                const int rle_max = 255;
                int rle_in_this_symbol = rle_count;
                if (rle_in_this_symbol > rle_max)
                {
                    rle_in_this_symbol = rle_max;
                }

                s.WriteByte(checked((byte)(rle_in_this_symbol - 1)));
                rle_count -= rle_in_this_symbol;
            }
        }

        private static void WriteByteToRleStream(Stream s, byte b)
        {
            if (b == RLE_SENTINEL)
            {
                s.WriteByte(RLE_SENTINEL);
                s.WriteByte(RLE_SENTINEL);
            }
            else
            {
                s.WriteByte(b);
            }
        }

        const byte RLE_SENTINEL = 255;

        private static List<int> ParseRleIntList(string value)
        {
            var bId = new byte[4];
            var msData = new MemoryStream(Convert.FromBase64String(value));
            var productIds = new List<int>();
            using (var gzStream = new GZipStream(msData, CompressionMode.Decompress))
            {
                int last = 0;
                while (true)
                {
                    int read = gzStream.ReadByte();
                    if (read == -1)
                    {
                        break;
                    }

                    if (read == RLE_SENTINEL)
                    {
                        uint rle_count_minus_1 = ReadOrThrow(gzStream);

                        if (rle_count_minus_1 == RLE_SENTINEL)
                        {
                            // no-op, escape for literal 255
                        }
                        else
                        {
                            // note repeat of 0 == one above last
                            for (int repeat = 0; repeat <= rle_count_minus_1; repeat++)
                            {
                                last += 1;
                                productIds.Add(last);
                            }

                            // move to next byte
                            continue;
                        }
                    }

                    uint c = (uint)read;
                    c = c | (ReadOrThrow(gzStream) << 8);
                    c = c | (ReadOrThrow(gzStream) << 16);
                    c = c | (ReadOrThrow(gzStream) << 24);

                    last = unchecked((int)c);
                    productIds.Add(last);
                }
            }

            return productIds;
        }

        private static uint ReadOrThrow(Stream s)
        {
            int a = s.ReadByte();
            if (a == -1)
            {
                throw new EndOfStreamException("Unexpected end of stream");
            }
            return (uint)a;
        }

        #endregion

        #region XmlSerializable

        public XmlSchema GetSchema() => null!;

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent();

            var compressed = reader.ReadElementContentAsString();
            if (string.IsNullOrWhiteSpace(compressed))
            {
                return;
            }

            Clear();
            foreach (var a in ParseRleIntList(compressed))
            {
                Add(a);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            string compressed;
            if (this.Any())
            {
                compressed = EncodeRleIntList(this);
            }
            else
            {
                compressed = "";
            }

            writer.WriteString(compressed);
        }

        #endregion
    }
}
