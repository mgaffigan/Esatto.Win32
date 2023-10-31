using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Esatto.Utilities
{
    // From http://codereview.stackexchange.com/questions/5391/range-of-integers-from-a-string
    public class IntRange
    {
        private static IEnumerable<int> GetRange(int start, int end)
        {
            int step = (start > end) ? -1 : 1;
            for (int i = start; true; i += step)
            {
                yield return i;
                if (i == end) break;
            }
        }

        public static IEnumerable<int> GetRange(string numbers)
        {
            var items = numbers.Split(',');
            var rangeRegex = new Regex(@"(-?\d+)-(-?\d+)");
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    int result;
                    if (int.TryParse(item, out result))
                    {
                        yield return result;
                        continue;
                    }
                    else if (rangeRegex.IsMatch(item))
                    {
                        var m = rangeRegex.Match(item);
                        foreach (var n in GetRange(
                            int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), 
                            int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
                        {
                            yield return n;
                        }
                        continue;
                    }
                    throw new InvalidCastException($"Unable to cast \"{item}\" to an int");
                }
            }
        }

        public static string GetRange(IEnumerable<int> numbers)
        {
            var sorted = numbers.ToArray();
            Array.Sort<int>(sorted);

            int? currentRun = null;
            int? lastValue = null;
            StringBuilder sb = new StringBuilder();

            foreach (var n in sorted)
            {
                if (currentRun == null)
                {
                    // first run, start new
                    lastValue = currentRun = n;
                }
                else if (n == lastValue + 1)
                {
                    // same as current run, add to the end
                    lastValue = n;
                }
                else
                {
                    // close the old
                    closeRun(sb, currentRun, lastValue);

                    // start new run
                    lastValue = currentRun = n;
                }
            }

            if (currentRun != null)
            {
                closeRun(sb, currentRun, lastValue);
            }

            return sb.ToString();
        }

        private static void closeRun(StringBuilder sb, int? runStart, int? runEnd)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append(runStart);

            if (runEnd != null && runEnd != runStart)
            {
                sb.Append(" - ");
                sb.Append(runEnd);
            }
        }

        //"1,,3, 7,-1,5 ,10-5, 1-3,7-7,-3--5" is valid
    }
}
