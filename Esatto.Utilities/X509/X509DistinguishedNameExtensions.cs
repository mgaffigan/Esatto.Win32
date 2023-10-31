using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
#if NET
using System.Formats.Asn1;
using System.Security.Cryptography;
#endif

namespace Esatto.Utilities
{
    public static class X509DistinguishedNameExtensions
    {
        public static IEnumerable<KeyValuePair<string, string>> GetRelativeNames(this X500DistinguishedName dn)
#if NET
        {
            var reader = new AsnReader(dn.RawData, AsnEncodingRules.BER);
            var snSeq = reader.ReadSequence();
            if (!snSeq.HasData)
            {
                throw new InvalidOperationException();
            }

            // Many types are allowable.  We're only going to support the string-like ones
            // (This excludes IPAddress, X400 address, and other wierd stuff)
            // https://www.rfc-editor.org/rfc/rfc5280#page-37
            // https://www.rfc-editor.org/rfc/rfc5280#page-112
            var allowedRdnTags = new[]
            {
                UniversalTagNumber.TeletexString, UniversalTagNumber.PrintableString,
                UniversalTagNumber.UniversalString, UniversalTagNumber.UTF8String,
                UniversalTagNumber.BMPString, UniversalTagNumber.IA5String,
                UniversalTagNumber.NumericString, UniversalTagNumber.VisibleString,
                UniversalTagNumber.T61String
            };
            while (snSeq.HasData)
            {
                var rdnSeq = snSeq.ReadSetOf().ReadSequence();
                var attrOid = rdnSeq.ReadObjectIdentifier();
                var attrValueTagNo = (UniversalTagNumber)rdnSeq.PeekTag().TagValue;
                if (!allowedRdnTags.Contains(attrValueTagNo))
                {
                    throw new NotSupportedException($"Unknown tag type {attrValueTagNo} for attr {attrOid}");
                }
                var attrValue = rdnSeq.ReadCharacterString(attrValueTagNo);
                var friendlyName = new Oid(attrOid).FriendlyName;
                yield return new KeyValuePair<string, string>(friendlyName ?? attrOid, attrValue);
            }
        }
#else
        {
            foreach (Match match in matchKvps.Matches(dn.Name)) 
            {
                var value = match.Groups["value"].Value;
                if (value.StartsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                    value = value.Replace("\"\"", "\"");
                }
                yield return new KeyValuePair<string, string>(match.Groups["attr"].Value, value);
            }
        }

        private static readonly Regex matchKvps = new Regex(@"(?<=^|,\s*)(?<attr>[A-Z]+)=(?<value>(?:""(?:[^""]|"""")*"")|[^,""]*)(?=$|\s*,)");
#endif
    }
}
