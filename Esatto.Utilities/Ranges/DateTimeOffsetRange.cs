using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    [TypeConverter(typeof(DateTimeOffsetRangeTypeConverter))]
    [DataContract(Namespace = "urn:esatto:ranges")]
    public struct DateTimeOffsetRange : IEquatable<DateTimeOffsetRange>, IComparable<DateTimeOffsetRange>, IComparable
    {
        private readonly static DateTimeOffset Epoch = new DateTimeOffset(1904, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public static DateTimeOffsetRange Max { get; } = new DateTimeOffsetRange(Epoch, new DateTimeOffset(2104, 1, 1, 0, 0, 0, TimeSpan.Zero));

        [DataMember]
        public DateTimeOffset Start { get; private set; }

        public TimeSpan Length => End - Start;

        [DataMember]
        public DateTimeOffset End { get; private set; }

        public DateTimeOffsetRange(DateTimeOffset start, DateTimeOffset end)
        {
            if (!(end >= start))
            {
                throw new ArgumentException("Contract assertion not met: end >= start", nameof(end));
            }

            this.Start = start;
            this.End = end;
        }

        public static implicit operator DateTimeOffsetRange(DateTimeRange range)
            => new DateTimeOffsetRange(range.Start, range.End);

        public DateTimeOffsetRange BoundTo(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
        {
            var boundedRange = new DateTimeOffsetRange(
                startDate ?? Epoch,
                endDate ?? Max.End);
            return boundedRange.Intersect(this);
        }

        public DateTimeOffsetRange With(DateTimeOffset? start = null, DateTimeOffset? end = null)
        {
            return new DateTimeOffsetRange(start ?? this.Start, end ?? this.End);
        }

        public bool IsEnclosedBy(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return (other.Start <= this.Start
                && other.End >= this.End);
        }

        public bool Contains(DateTimeOffset other)
        {
            return this.Start <= other && this.End > other;
        }

        public bool IsIntersecting(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return Contains(other.Start) || Contains(other.End.AddTicks(-1))
                || other.Contains(Start) || other.Contains(End.AddTicks(-1));
        }

        public bool IsTouching(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return IsIntersecting(other)
                || other.End == this.Start
                || this.End == other.Start;
        }

        public DateTimeOffsetRange? Subtract(DateTimeOffsetRange other, out DateTimeOffsetRange? newRange)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(IsIntersecting(other) && !IsEnclosedBy(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other) && !IsEnclosedBy(other)", nameof(other));
            }

            return SubtractNonIntersecting(other, out newRange);
        }

        public DateTimeOffsetRange? SubtractNonIntersecting(DateTimeOffsetRange other, out DateTimeOffsetRange? newRange)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (!IsIntersecting(other))
            {
                newRange = null;
                return this;
            }
            if (IsEnclosedBy(other))
            {
                newRange = null;
                return null;
            }

            DateTimeOffsetRange? left = null;
            DateTimeOffsetRange? right = null;
            if (this.Contains(other.Start.AddTicks(-1)))
            {
                left = new DateTimeOffsetRange(this.Start, other.Start);
            }

            if (this.Contains(other.End))
            {
                right = new DateTimeOffsetRange(other.End, this.End);
            }

            if (left != null && right != null)
            {
                newRange = right.Value;
                return left.Value;
            }
            else
            {
                newRange = null;
                return left ?? right;
            }
        }

        public IEnumerable<DateTimeOffsetRange> Subtract(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var remainder = Subtract(other, out var newRange);
            if (remainder != null)
            {
                yield return remainder.Value;
            }
            if (newRange != null)
            {
                yield return newRange.Value;
            }
        }

        public DateTimeOffsetRange Union(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(IsTouching(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsTouching(other)", nameof(other));
            }

            return new DateTimeOffsetRange(Earlier(other.Start, this.Start), Later(other.End, this.End));
        }

        public DateTimeOffsetRange Intersect(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!IsIntersecting(other))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other)", nameof(other));
            }

            return new DateTimeOffsetRange(Later(other.Start, this.Start), Earlier(other.End, this.End));
        }

        public DateTimeOffsetRange ExpandToInclude(DateTimeOffsetRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new DateTimeOffsetRange(Earlier(other.Start, this.Start), Later(other.End, this.End));
        }

        private static DateTimeOffset Earlier(DateTimeOffset a, DateTimeOffset b) => a <= b ? a : b;
        private static DateTimeOffset Later(DateTimeOffset a, DateTimeOffset b) => a >= b ? a : b;

        public DateTimeOffsetRange Shift(TimeSpan offset)
        {
            return new DateTimeOffsetRange(Start + offset, End + offset);
        }

        public DateTimeOffsetRange Inflate(TimeSpan padLeft = default(TimeSpan), TimeSpan padRight = default(TimeSpan))
        {
            if (!(padLeft + padRight + Length >= TimeSpan.Zero))
            {
                throw new ArgumentException("Contract assertion not met: padLeft + padRight + Length >= TimeSpan.Zero", nameof(padLeft));
            }

            return new DateTimeOffsetRange(Start - padLeft, End + padRight);
        }

        public override bool Equals(object? obj) => obj is DateTimeOffsetRange dr ? Equals(dr) : false;

        // Instant equality, consistent with DateTimeOffset.Equals
        public bool Equals(DateTimeOffsetRange other)
        {
            return this.Start == other.Start
                && this.End == other.End;
        }

        public int CompareTo(object? obj) => obj is DateTimeOffsetRange dr ? CompareTo(dr) : 1;

        public int CompareTo(DateTimeOffsetRange other)
        {
            // sort by StartDate, then by length
            var startDateComparison = Start.CompareTo(other.Start);
            if (startDateComparison == 0)
            {
                return Length.CompareTo(other.Length);
            }
            return startDateComparison;
        }

        public override string ToString() => $"[{Start}, {End})";

        public override int GetHashCode() => unchecked(Start.GetHashCode() ^ (End.GetHashCode() << 16));

        public static bool operator ==(DateTimeOffsetRange a, DateTimeOffsetRange b) => a.Equals(b);
        public static bool operator !=(DateTimeOffsetRange a, DateTimeOffsetRange b) => !(a == b);

        public class DateTimeOffsetRangeTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
            {
                return sourceType == typeof(string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            {
                return destinationType == typeof(string)
                    || destinationType == typeof(InstanceDescriptor);
            }

            public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
            {
                var sValue = (string)(value ?? throw new ArgumentNullException(nameof(value)));
                var sValues = DateTimeRange.DateTimeRangeTypeConverter.SplitRange(sValue);

                if (sValues is null)
                {
                    throw new FormatException($"'{value}' constitutes an invalid range.  Must be in format '{{start}}[..{{end}}]' ('2024-01-01T00:00-05:00' or '2024-01-01T00:00-05:00..2024-01-05T00:00-05:00').");
                }

                DateTimeOffset start, end;
                try
                {
                    // values without an offset are assumed to be local, matching DateTimeOffset.Parse
                    start = DateTimeOffset.Parse(sValues[0].Trim(), culture);
                    // single value is assumed to be one day
                    end = sValues.Length == 2
                        ? DateTimeOffset.Parse(sValues[1].Trim(), culture)
                        : start.AddDays(1);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Could not parse start or end of range '{value}'.", ex);
                }

                if (end < start)
                {
                    throw new FormatException($"End of range '{value}' must not be before start.");
                }

                return new DateTimeOffsetRange(start, end);
            }

            public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type? destinationType)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (destinationType == null)
                {
                    throw new ArgumentNullException(nameof(destinationType));
                }

                var range = (DateTimeOffsetRange)value;

                if (destinationType == typeof(string))
                {
                    // ISO 8601 round-trip format is culture-invariant and preserves offset
                    return string.Format(CultureInfo.InvariantCulture, "{0:o}..{1:o}", range.Start, range.End);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    var ci = typeof(DateTimeOffsetRange).GetConstructor(new[] { typeof(DateTimeOffset), typeof(DateTimeOffset) });
                    return new InstanceDescriptor(ci, new object[] { range.Start, range.End });
                }
                else throw new NotSupportedException($"Cannot convert range to type '{destinationType}'");
            }
        }

        public static bool operator <(DateTimeOffsetRange left, DateTimeOffsetRange right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(DateTimeOffsetRange left, DateTimeOffsetRange right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(DateTimeOffsetRange left, DateTimeOffsetRange right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(DateTimeOffsetRange left, DateTimeOffsetRange right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
