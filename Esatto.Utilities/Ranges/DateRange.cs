using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    [DataContract(Namespace = "urn:esatto:ranges")]
    public sealed class DateRange : Range, IComparable<DateRange>
    {
        private readonly static DateTime Epoch = new DateTime(1904, 1, 1);
        public static DateRange Max { get; } = new DateRange(Epoch, new DateTime(2104, 1, 1));

        [DataMember]
        public DateTime StartDate { get; private set; }

        public DateTime LastDayOfRange => EndDate.AddDays(-1);

        [DataMember]
        public DateTime EndDate { get; private set; }

        public DateRange(DateTime startDate, int days)
            : this(startDate, startDate.AddDays(days))
        {
            if (!(days >= 1))
            {
                throw new ArgumentOutOfRangeException(nameof(days), "Contract assertion not met: days >= 1");
            }
        }

        public DateRange(DateTime startDate, DateTime endDate)
            : base((int)(startDate - Epoch).TotalDays, (int)(endDate - startDate).TotalDays)
        {
            if (!(startDate.Date == startDate))
            {
                throw new ArgumentException("Contract assertion not met: startDate.Date == startDate", nameof(startDate));
            }
            if (!(endDate.Date == endDate))
            {
                throw new ArgumentException("Contract assertion not met: endDate.Date == endDate", nameof(endDate));
            }

            this.StartDate = startDate;
            this.EndDate = endDate;
        }

        protected override Range CreateRange(int newStart, int newLength)
        {
            var start = Epoch.AddDays(newStart);
            return new DateRange(start, newLength);
        }

        public override string ToString()
        {
            return $"{StartDate:d} +{Length}";
        }

        #region overrides

        public DateRange BoundTo(DateTime? startDate = null, DateTime? endDate = null)
        {
            var boundedRange = new DateRange(
                startDate ?? Epoch,
                endDate ?? Max.EndDate);
            return boundedRange.Intersect(this);
        }

        public new DateRange With(int? startDelta = null, int? newLength = null) => (DateRange)base.With(startDelta, newLength);
        public bool Contains(DateTime date) => base.Contains(new DateRange(date, 1).Start);

        public DateRange? Subtract(DateRange other, out DateRange? newRange)
        {
            var result = base.Subtract(other, out var newRRange);
            newRange = (DateRange?)newRRange;
            return (DateRange?)result;
        }

        public IEnumerable<DateRange?> Subtract(DateRange other) => base.Subtract(other).Cast<DateRange?>();

        public DateRange? SubtractNonIntersecting(DateRange other, out DateRange? newRange)
        {
            var result = base.SubtractNonIntersecting(other, out var newRRange);
            newRange = (DateRange?)newRRange;
            return (DateRange?)result;
        }

        public IEnumerable<DateRange> SubtractNonIntersecting(DateRange other)
        {
            var a = SubtractNonIntersecting(other, out var b);
            if (a != null)
            {
                yield return a;
            }
            if (b != null)
            {
                yield return b;
            }
        }

        public DateRange Union(DateRange other) => (DateRange)base.Union(other);

        public DateRange Intersect(DateRange other) => (DateRange)base.Intersect(other);

        public DateRange ExpandToInclude(DateRange other) => (DateRange)base.ExpandToInclude(other);

        public new DateRange Shift(int offset) => (DateRange)base.Shift(offset);

        public new DateRange Inflate(int padLeft = 0, int padRight = 0) => (DateRange)base.Inflate(padLeft, padRight);

        public new IEnumerable<DateTime> AsEnumerable()
        {
            for (DateTime cd = StartDate; cd < EndDate; cd = cd.AddDays(1))
            {
                yield return cd;
            }
        }

        public int CompareTo(DateRange? other) => CompareTo((Range?)other);

        #endregion
    }
}
