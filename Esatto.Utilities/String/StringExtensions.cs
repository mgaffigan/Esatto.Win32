using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Esatto.Utilities
{
    public static class StringExtensionsClass
    {
        public static bool In(this string? s, params string?[] options)
        {
            if (options == null || s == null)
            {
                return false;
            }

            return options.Contains(s, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Transform a string to "Title Case"
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? ToTitleCase(this string? s)
        {
            if (s == null)
            {
                return null;
            }

            var sbResult = new StringBuilder(s.Length);
            bool isAfterWhitespace = true;
            // since some personal names are outside the BMP, enumerate codepoints
            for (var enumerator = StringInfo.GetTextElementEnumerator(s); enumerator.MoveNext();)
            {
                var category = char.GetUnicodeCategory(s, enumerator.ElementIndex);
                if (category == UnicodeCategory.UppercaseLetter && !isAfterWhitespace)
                {
                    sbResult.Append(enumerator.GetTextElement().ToLower(CultureInfo.CurrentCulture));
                }
                else if (category == UnicodeCategory.LowercaseLetter && isAfterWhitespace)
                {
                    sbResult.Append(enumerator.GetTextElement().ToUpper(CultureInfo.CurrentCulture));
                }
                else
                {
                    // anything else passes through unmolested
                    sbResult.Append(enumerator.GetTextElement());
                }

                // once we hit whitespace, we go back to caps
                if (category == UnicodeCategory.SpaceSeparator)
                {
                    isAfterWhitespace = true;
                }
                // as soon as we hit latin text again, flip back to lowercase
                else if (category == UnicodeCategory.UppercaseLetter
                    || category == UnicodeCategory.LowercaseLetter)
                {
                    isAfterWhitespace = false;
                }
            }

            return sbResult.ToString();
        }

        // https://stackoverflow.com/a/63760729/138200
        public static string GetInitials(this string value)
           => string.Concat(value
              .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
              .Where(x => x.Length >= 1 && char.IsLetter(x[0]))
              .Select(x => char.ToUpper(x[0], CultureInfo.CurrentCulture)));

#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? ToTitleCaseIfUpper(this string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            if (IsMixedCase(s))
            {
                return s;
            }
            else
            {
                return ToTitleCase(s);
            }
        }

        /// <summary>
        /// Check if the specified string appears to have mixed case
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static bool IsMixedCase(this string s)
        {
            bool lowerFound = false;
            bool upperFound = false;
            var si = new StringInfo(s);

            for (int i = 0; i < si.LengthInTextElements; i++)
            {
                var schar = si.SubstringByTextElements(i, 1);

                //look for a letter character and store to isDetermined
                if (char.IsUpper(schar, 0))
                    upperFound = true;
                else if (char.IsLower(schar, 0))
                    lowerFound = true;

                //immediately return if we have found both cases
                if (upperFound && lowerFound)
                    return true;
            }

            //we only found one or zero cases.
            return false;
        }

        /// <summary>
        /// Transform a string to "Upper case first"
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? ToUpperCaseFirst(this string? s)
        {
            if (s == null || s.Length == 0)
                return s;

            var si = new StringInfo(s);

            StringBuilder sbout = new StringBuilder(s.Length);

            //copy the first character as uppercase
            sbout.Append(si.SubstringByTextElements(0, 1).ToUpper(CultureInfo.CurrentCulture));

            if (s.Length > 1)
            {
                //make a single copy of the string as lowercase and use append to substring
                sbout.Append(si.SubstringByTextElements(1).ToLower(CultureInfo.CurrentCulture));
            }

            return sbout.ToString();
        }

        /// <summary>
        /// Transform a string to "Sentance case. This capitalizes after punctuation."
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? ToSentenceCase(this string? s)
        {
            if (s == null || s.Length == 0)
                return s;
            var si = new StringInfo(s);
            var sb = new StringBuilder(s.Length);

            //assume the first letter should be capitalized
            bool sentanceEnderSentinel = true;
            for (int ci = 0; ci < si.LengthInTextElements; ci++)
            {
                var schar = si.SubstringByTextElements(ci, 1);

                //If we should capitalize, find the first letter (nonwhitespace
                //or punctuation character) and capitalize.
                if (sentanceEnderSentinel
                    && Char.IsLetter(schar, 0))
                {
                    sb.Append(schar.ToUpper(CultureInfo.CurrentCulture));
                    sentanceEnderSentinel = false;
                }
                else
                {
                    //look for a end of sentance punctuation mark and assume the
                    //next letter should be capitalized
                    if (schar.Equals(".", StringComparison.Ordinal)
                        || schar.Equals("!", StringComparison.Ordinal)
                        || schar.Equals("?", StringComparison.Ordinal))
                        sentanceEnderSentinel = true;

                    //convert the current letter to lowercase.
                    sb.Append(schar.ToLower(CultureInfo.CurrentCulture));
                }
            }

            return sb.ToString();
        }

#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? Left(this string? s, int length)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Substring(0, Math.Min(s.Length, length));
        }

#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? LeftByTextElements(this string? s, int length)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            var si = new StringInfo(s);
            return si.SubstringByTextElements(0, Math.Min(si.LengthInTextElements, length));
        }

#if NET
        [return: NotNullIfNotNull(nameof(text))]
#endif
        public static IEnumerable<string> WordWrap(this string? text, int maxLineLength)
        {
            if (maxLineLength < 2) throw new ArgumentOutOfRangeException(nameof(maxLineLength));

            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            int currentIndex;
            var lastWrap = 0;
            var whitespace = new[] { ' ', '\r', '\n', '\t' };
            var wrapChars = whitespace;
            // no separate wrap-chars, as they are typically followed by whitespace
            // new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' };
            // counterexample:
            // |                |
            // ACETAMINOPHEN 0.125mg would produce
            // ACETAMINOPHEN 0.
            // 125mg

            var si = new StringInfo(text);
            do
            {
                currentIndex =
                    // if the total length of the string is less than the max line length, we don't need to wrap
                    lastWrap + maxLineLength > si.LengthInTextElements ? si.LengthInTextElements
                    // search for a wrap character between the last wrap point and the end of the string
                    : (text.LastIndexOfAny(wrapChars, Math.Min(si.LengthInTextElements - 1, lastWrap + maxLineLength)) + 1);

                // if we did not find anthing, cut it at max line length or the text length
                //     |      |
                // ie: AAAAAAAAAAAA wraps at
                //            ^ regardless of the abscense of a wrap character
                if (currentIndex <= lastWrap)
                    currentIndex = Math.Min(lastWrap + maxLineLength, si.LengthInTextElements);

                // return the trimmed line
                yield return si.SubstringByTextElements(lastWrap, currentIndex - lastWrap).Trim(whitespace);

                // advance our search to after the current piece
                lastWrap = currentIndex;
            } while (currentIndex < si.LengthInTextElements);
        }

#if NET
        [return: NotNullIfNotNull(nameof(text))]
#endif
        public static string? Ellipsis(this string? text, int maxTextLength)
        {
            if (!(maxTextLength > 4))
            {
                throw new ArgumentOutOfRangeException(nameof(maxTextLength), "Contract assertion not met: maxTextLength > 4");
            }

            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var lines = WordWrap(text, maxTextLength).ToArray();
            if (lines.Length > 1 && lines[0].Length > (maxTextLength - 3))
            {
                // shorten then add ...
                return $"{text.WordWrap(maxTextLength - 3).First()}...";
            }
            else if (lines.Length == 1)
            {
                return text;
            }
            else
            {
                return $"{lines[0]}...";
            }
        }

        public static string GetSha1Hash(this string @this)
        {
#pragma warning disable CA5350 // SHA1 is weak
            using var sha1 = SHA1.Create();
#pragma warning restore CA5350 // SHA1 is weak
            sha1.ComputeHash(Encoding.UTF8.GetBytes(@this));

            var sb = new StringBuilder(sha1.Hash!.Length * 2);
            foreach (byte b in sha1.Hash)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

#if NET
        [return: NotNullIfNotNull(nameof(s))]
#endif
        public static string? NormalizeCrLf(this string? s)
        {
            if (s == null)
            {
                return null;
            }

            if (!NeedsCrlfNormalization(s))
            {
                return s;
            }

            var sbResult = new StringBuilder(s.Length + 10 + s.Length / 40);
            for (int i = 0; i < s.Length; i++)
            {
                // If we have a CR, it might be part of a CRLF, or it could be independent
                if (s[i] == '\r')
                {
                    // if it is part of a CRLF, skip the LF
                    if (i + 1 < s.Length && s[i + 1] == '\n')
                    {
                        i += 1;
                    }

                    sbResult.Append('\r');
                    sbResult.Append('\n');
                }
                // Since LF as part of CRLF was handled earlier, this is only a loose LF
                else if (s[i] == '\n')
                {
                    sbResult.Append('\r');
                    sbResult.Append('\n');
                }
                else
                {
                    sbResult.Append(s[i]);
                }
            }
            return sbResult.ToString();
        }

        private static bool NeedsCrlfNormalization(string s)
        {
            // true if \n or \r by itself
            // false if \r is always followed by \n and \n is always preceeded by \r
            bool lastWasCR = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (lastWasCR)
                {
                    if (s[i] != '\n')
                    {
                        return true;
                    }
                    else
                    {
                        lastWasCR = false;
                    }
                }
                else if (s[i] == '\r')
                {
                    lastWasCR = true;
                }
                else if (s[i] == '\n')
                {
                    return true;
                }
            }

            if (lastWasCR)
            {
                return true;
            }

            return false;
        }

        public static bool IsIllegalXmlString(this string s) => s.Any(IsIllegalXmlChar);

        public static void AssertValidXmlString(this string s, string paramName = "s")
        {
            if (s.IsIllegalXmlString())
            {
                throw new ArgumentException("Illegal character in string", paramName);
            }
        }

        public static bool IsIllegalXmlChar(this char c) => c switch
        {
            // https://www.w3.org/TR/REC-xml/#charsets
            '\t' => false,
            '\n' => false,
            '\r' => false,
            (>= '\x20') and (<= '\xd7ff') => false,
            (>= '\xe000') and (<= '\xfffd') => false,
            // No need to exclude 0x10_0000 to 0x10_ffff since they are
            // represented as surrogate pairs in UTF-16 in the range
            // 0xd800-0xdfff, which is already excluded
            _ => true,
        };
    }
}

#if NETFRAMEWORK
namespace System
{
    public static class Net47StringExtensions
    {
        public static string Replace(this string s, string oldValue, string newValue, StringComparison comparison)
            => s.Replace(oldValue, newValue);
        public static bool Contains(this string s, string value, StringComparison comparison)
            => s.IndexOf(value, comparison) >= 0;
        public static bool Contains(this string s, char value, StringComparison comparison)
            => s.IndexOf(value, comparison) >= 0;
        public static int IndexOf(this string s, char c, StringComparison comparison)
            => s.IndexOf(c);
        public static void Append(this StringBuilder sb, IFormatProvider? provider, FormattableString message)
            => sb.Append(message.ToString(provider));
    }
}
#endif