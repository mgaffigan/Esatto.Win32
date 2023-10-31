using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Esatto.Utilities
{
    public static class RelativeTimespanConverter
    {
        static Regex rxparse = new Regex(regex, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        const string regex =
            @"
                ^
                \s*
	                (?<command>T|@)?
                \s*
	                (?<dir>\+|-)?
                \s*
                (?:
	                (?<day>\d*)\s*d(?:ays?)?
	                |
	                (?<hour>\d*)\s*h(?:ours?)?
	                |
	                (?<min>\d*)\s*m(?:in(?:utes?)?)?
	                |
	                (?<zero>0)
	                |
	                (?<hour>\d*):(?<min>\d{2})(?:\s*(?<ampm>A|P)M?)?
	                |
	                \s*
                )*
                \s*
                $
            ";

        public static string TimespanToString(TimeSpan tsvalue)
        {
            var totalHours = tsvalue.Days * 24 + tsvalue.Hours;
            var minutes = tsvalue.Minutes;
            var sb = new StringBuilder();
            if (tsvalue < TimeSpan.Zero)
            {
                sb.Append("T-");
                totalHours *= -1;
                minutes *= -1;
            }
            else
            {
                sb.Append("T+");
            }
            if (totalHours != 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{totalHours}h");
            }
            if (minutes != 0)
            {
                sb.Append(CultureInfo.InvariantCulture, $"{minutes}m");
            }
            if (totalHours == 0 && minutes == 0)
            {
                sb.Append('0');
            }

            return sb.ToString();
        }

        public static TimeSpan? StringToTimespan(string sval, DateTime? relativeToNull)
        {
            if (string.IsNullOrWhiteSpace(sval))
            {
                return null;
            }

            var matches = rxparse.Match(sval);
            if (!matches.Success || !matches.Groups.Cast<Group>().Any(c => c.Success))
            {
                throw new FormatException("Format was not recognized");
            }

            var commandMatch = matches.Groups["command"];
            var zeroMatch = matches.Groups["zero"];
            var dayMatch = matches.Groups["day"];
            var hoursMatch = matches.Groups["hour"];
            var minMatch = matches.Groups["min"];
            var dirMatch = matches.Groups["dir"];
            var ampmMatch = matches.Groups["ampm"];

            if (zeroMatch.Success && (dayMatch.Success || hoursMatch.Success || minMatch.Success))
            {
                throw new FormatException("Format was not recognized");
            }

            bool isRelative = true;
            if (commandMatch.Success)
            {
                if (commandMatch.Value.Equals("T", StringComparison.OrdinalIgnoreCase))
                {
                    // nop
                }
                else if (commandMatch.Value.Equals("@", StringComparison.Ordinal))
                {
                    if (relativeToNull == null)
                    {
                        // cannot have relative time without relative date
                        throw new FormatException("Only relative dates are permitted");
                    }

                    isRelative = false;
                }
                else throw new NotSupportedException("Unexpected commandMatch");
            }

            bool isNegative = false;
            if (dirMatch.Success)
            {
                if (dirMatch.Value == "+")
                {
                    // nop
                }
                else if (dirMatch.Value == "-")
                {
                    isNegative = true;
                }
                else throw new NotSupportedException("Unexpected dirmatch");
            }

            bool? isPm = null;
            if (ampmMatch.Success)
            {
                isRelative = false;

                if (ampmMatch.Value.Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    isPm = false;
                }
                else if (ampmMatch.Value.Equals("P", StringComparison.OrdinalIgnoreCase))
                {
                    isPm = true;
                }
                else throw new NotSupportedException("Unexpected ampmMatch");
            }

            if (zeroMatch.Success)
            {
                return TimeSpan.Zero;
            }

            int days = 0, hours = 0, min = 0;
            if (dayMatch.Success)
            {
                days = int.Parse(dayMatch.Value, CultureInfo.InvariantCulture);
            }
            if (hoursMatch.Success)
            {
                hours = int.Parse(hoursMatch.Value, CultureInfo.InvariantCulture);
                if (isPm == true)
                {
                    hours += 12;
                }
                else if (isPm == false && hours == 12)
                {
                    hours = 0;
                }
            }
            if (minMatch.Success)
            {
                min = int.Parse(minMatch.Value, CultureInfo.InvariantCulture);
            }

            if (isRelative)
            {
                var result = new TimeSpan(hours + (days * 24), min, 0);
                if (isNegative)
                {
                    result = new TimeSpan(-1L * result.Ticks);
                }
                return result;
            }
            else
            {
                var relativeTo = relativeToNull ?? throw new FormatException("Relative time specified by no base time provided");
                var date = relativeTo.Date
                    .AddDays(days * (isNegative ? -1 : 1))
                    .AddHours(hours).AddMinutes(min);
                return date.Subtract(relativeTo);
            }
        }
    }
}
