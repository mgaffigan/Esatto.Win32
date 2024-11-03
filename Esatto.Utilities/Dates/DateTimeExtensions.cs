namespace Esatto.Utilities
{
    public static class DateTimeExtensions
    {
        public static DateTime AsUtcDateOnly(this DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
        }

        public static DateTime EarlierOf(this DateTime a, DateTime b)
        {
            if (!(a.Kind == b.Kind))
            {
                throw new ArgumentException("Contract assertion not met: a.Kind == b.Kind", nameof(a));
            }

            return new DateTime(Math.Min(a.Ticks, b.Ticks), a.Kind);
        }

        public static DateTime LaterOf(this DateTime a, DateTime b)
        {
            if (!(a.Kind == b.Kind))
            {
                throw new ArgumentException("Contract assertion not met: a.Kind == b.Kind", nameof(a));
            }

            return new DateTime(Math.Max(a.Ticks, b.Ticks), a.Kind);
        }

        public static DateTime? EarlierOf(this DateTime? a, DateTime? b)
        {
            if (!(a == null || b == null || a?.Kind == b?.Kind))
            {
                throw new ArgumentException("Contract assertion not met: a == null || b == null || a?.Kind == b?.Kind", nameof(a));
            }

            if (a == null)
            {
                return b;
            }
            if (b == null)
            {
                return a;
            }

            return a.Value.EarlierOf(b.Value);
        }

        public static DateTime? LaterOf(this DateTime? a, DateTime? b)
        {
            if (!(a == null || b == null || a?.Kind == b?.Kind))
            {
                throw new ArgumentException("Contract assertion not met: a == null || b == null || a?.Kind == b?.Kind", nameof(a));
            }

            if (a == null)
            {
                return b;
            }
            if (b == null)
            {
                return a;
            }

            return a.Value.LaterOf(b.Value);
        }

        /// <summary>
        /// Get the date that is the highest to the specified date
        /// </summary>
        /// <example>2011-06-01T14:20 -> 2011-06-02; 2011-06-01T00:00 -> 2011-06-01</example>
        /// <param name="datetime"></param>
        /// <returns></returns>
        public static DateTime Ceiling(this DateTime datetime)
        {
            DateTime date = datetime.Date;

            if (datetime > date)
                return date.AddDays(1);
            else
                return date;
        }

        /// <summary>
        /// Get all days between this date and the end date.  Start date
        /// </summary>
        /// <param name="startDate">first date (inclusive)</param>
        /// <param name="endDate">last date (exclusive)</param>
        /// <returns></returns>
        public static IEnumerable<DateTime> To(this DateTime startDate, DateTime endDate)
        {
                 //find the next whole day
            for (DateTime dt = startDate.Ceiling(); 
                dt < endDate; 
                dt = dt.AddDays(1))
                yield return dt;
        }

        public static bool IsLaterThan(this DateTime me, DateTime comparison)
        {
            return me.CompareTo(comparison) > 0;
        }

        public static bool IsEarlierThan(this DateTime me, DateTime comparison)
        {
            return me.CompareTo(comparison) < 0;
        }

        public static bool IsEqualTo(this DateTime me, DateTime comparison)
        {
            return me.CompareTo(comparison) == 0;
        }
    }
}
