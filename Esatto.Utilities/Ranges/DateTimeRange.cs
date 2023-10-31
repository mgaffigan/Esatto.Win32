using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    [TypeConverter(typeof(DateTimeRangeTypeConverter))]
    [DataContract(Namespace = "urn:esatto:ranges")]
    [KnownType(typeof(DateRange))]
    public struct DateTimeRange : IEquatable<DateTimeRange>, IComparable<DateTimeRange>, IComparable
    {
        private readonly static DateTime Epoch = new DateTime(1904, 1, 1);
        public static DateTimeRange Max { get; } = new DateTimeRange(Epoch, new DateTime(2104, 1, 1));

        [DataMember]
        public DateTime Start { get; private set; }

        public DateTimeKind Kind => Start.Kind;

        public TimeSpan Length => End - Start;

        [DataMember]
        public DateTime End { get; private set; }

        public DateTimeRange(DateTime start, DateTime end)
        {
            if (!(start.Kind == end.Kind))
            {
                throw new ArgumentException("Contract assertion not met: start.Kind == end.Kind", nameof(start));
            }
            if (!(end >= start))
            {
                throw new ArgumentException("Contract assertion not met: end >= start", nameof(end));
            }

            this.Start = start;
            this.End = end;
        }

        public static implicit operator DateTimeRange(DateRange range)
            => range == null ? throw new ArgumentNullException(nameof(range)) 
            : new DateTimeRange(range.StartDate, range.EndDate);

        public DateTimeRange BoundTo(DateTime? startDate = null, DateTime? endDate = null)
        {
            var boundedRange = new DateTimeRange(
                startDate ?? Epoch,
                endDate ?? Max.End);
            return boundedRange.Intersect(this);
        }

        public DateTimeRange With(DateTime? start = null, DateTime? end = null)
        {
            return new DateTimeRange(start ?? this.Start, end ?? this.End);
        }

        public bool IsEnclosedBy(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(this.Kind == other.Kind))
            {
                throw new ArgumentException("Contract assertion not met: this.Kind == other.Kind", nameof(other));
            }

            return (other.Start <= this.Start
                && other.End >= this.End);
        }

        public bool Contains(DateTime other)
        {
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
            }

            return this.Start <= other && this.End > other;
        }

        public bool IsIntersecting(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
            }

            return Contains(other.Start) || Contains(other.End.AddTicks(-1))
                || other.Contains(Start) || other.Contains(End.AddTicks(-1));
        }

        public bool IsTouching(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
            }

            return IsIntersecting(other)
                || other.End == this.Start
                || this.End == other.Start;
        }

        public DateTimeRange? Subtract(DateTimeRange other, out DateTimeRange? newRange)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
            }
            if (!(IsIntersecting(other) && !IsEnclosedBy(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other) && !IsEnclosedBy(other)", nameof(other));
            }

            return SubtractNonIntersecting(other, out newRange);
        }

        public DateTimeRange? SubtractNonIntersecting(DateTimeRange other, out DateTimeRange? newRange)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
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

            DateTimeRange? left = null;
            DateTimeRange? right = null;
            if (this.Contains(other.Start.AddTicks(-1)))
            {
                left = new DateTimeRange(this.Start, other.Start);
            }

            if (this.Contains(other.End))
            {
                right = new DateTimeRange(other.End, this.End);
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

        public IEnumerable<DateTimeRange> Subtract(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(other.Kind == this.Kind))
            {
                throw new ArgumentException("Contract assertion not met: other.Kind == this.Kind", nameof(other));
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

        public DateTimeRange Union(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!(IsTouching(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsTouching(other)", nameof(other));
            }

            var newLeft = Math.Min(other.Start.Ticks, this.Start.Ticks);
            var newRight = Math.Max(other.End.Ticks, this.End.Ticks);
            return new DateTimeRange(new DateTime(newLeft, Kind), new DateTime(newRight, Kind));
        }

        public DateTimeRange Intersect(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (!IsIntersecting(other))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other)", nameof(other));
            }

            var newLeft = Math.Max(other.Start.Ticks, this.Start.Ticks);
            var newRight = Math.Min(other.End.Ticks, this.End.Ticks);
            return new DateTimeRange(new DateTime(newLeft, Kind), new DateTime(newRight, Kind));
        }

        public DateTimeRange ExpandToInclude(DateTimeRange other)
        {
            if (other == default)
            {
                throw new ArgumentNullException(nameof(other));
            }

            var left = Math.Min(other.Start.Ticks, this.Start.Ticks);
            var right = Math.Max(other.End.Ticks, this.End.Ticks);
            return new DateTimeRange(new DateTime(left, Kind), new DateTime(right, Kind));
        }

        public DateTimeRange Shift(TimeSpan offset)
        {
            return new DateTimeRange(Start + offset, End + offset);
        }

        public DateTimeRange Inflate(TimeSpan padLeft = default(TimeSpan), TimeSpan padRight = default(TimeSpan))
        {
            if (!(padLeft + padRight + Length > TimeSpan.Zero))
            {
                throw new ArgumentException("Contract assertion not met: padLeft + padRight + Length > TimeSpan.Zero", nameof(padLeft));
            }

            return new DateTimeRange(Start - padLeft, End + padRight);
        }

        public override bool Equals(object? obj) => obj is DateTimeRange dr ? Equals(dr) : false;

        public bool Equals(DateTimeRange other)
        {
            return this.Start == other.Start
                && this.End == other.End;
        }

        public int CompareTo(object? obj) => obj is DateTimeRange dr ? CompareTo(dr) : 1;

        public int CompareTo(DateTimeRange other)
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

        public static bool operator ==(DateTimeRange a, DateTimeRange b) => a.Equals(b);
        public static bool operator !=(DateTimeRange a, DateTimeRange b) => !(a == b);

        public class DateTimeRangeTypeConverter : TypeConverter
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
                var sValues = sValue.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (sValues.Length > 2)
                {
                    throw new FormatException($"'{value}' constitutes an invalid range.  Must be in format '{{start index}}[, {{length}}]' ('1' or '1,3').");
                }
                try
                {
                    DateTime start = DateTime.Parse(sValues[0], culture);
                    DateTime end = start.AddDays(1);
                    if (sValues.Length == 1)
                    {
                        // no-op, use assumed length of 1
                    }
                    if (sValues.Length == 2)
                    {
                        end = DateTime.Parse(sValues[0], culture);
                    }
                    else throw new FormatException();

                    if (end < start)
                    {
                        throw new FormatException();
                    }

                    return new DateTimeRange(start, end);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Could not parse start offset or length of range '{value}'.  Start and length must be non-negative integers.  Length must be greater than 0.", ex);
                }
            }

            public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type? destinationType)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (destinationType == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var range = (DateTimeRange)value;

                if (destinationType == typeof(string))
                {
                    // for inheriting types, use ToString
                    if (value.GetType() != typeof(DateTimeRange))
                    {
                        return range.ToString();
                    }

                    return string.Format(culture, "{0}, {1}", range.Start, range.Length);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    var ci = typeof(DateTimeRange).GetConstructor(new[] { typeof(DateTime), typeof(DateTime) });
                    return new InstanceDescriptor(ci, new[] { range.Start, range.End });
                }
                else throw new NotSupportedException($"Cannot convert range to type '{destinationType}'");
            }
        }

        public IEnumerable<DateTime> AsEnumerable() 
            => AsEnumerable(TimeSpan.Zero);
        public IEnumerable<DateTime> AsEnumerable(TimeSpan timesOfDay) 
            => AsEnumerable(new[] { timesOfDay });
        public IEnumerable<DateTime> AsEnumerable(TimeSpan[] timesOfDay) 
            => AsEnumerable(timesOfDay, t => t, (_1, d) => d);

        public delegate TResult DateTimeRangeEnumerableTransformer<TSource, TResult>(TSource source, DateTime dateTime);

        public IEnumerable<TResult> AsEnumerable<TSource, TResult>(TSource[] timesOfDay, Func<TSource, TimeSpan> selector, 
            DateTimeRangeEnumerableTransformer<TSource,TResult> transform)
        {
            if (timesOfDay == null)
            {
                throw new ArgumentNullException(nameof(timesOfDay), "Contract assertion not met: timesOfDay != null");
            }
            if (!(timesOfDay.All(t => selector(t) < TimeSpan.FromHours(24))))
            {
                throw new ArgumentException("Contract assertion not met: timesOfDay.All(t => selector(t) < TimeSpan.FromHours(24))", nameof(timesOfDay));
            }

            var startDate = Start.Date;
            var endDate = End;

            for (var cDate = startDate; cDate < endDate; cDate = cDate.AddDays(1))
            {
                for (int i = 0; i < timesOfDay.Length; i++)
                {
                    var source = timesOfDay[0];
                    var newDate = cDate.Add(selector(source));
                    if (newDate >= endDate)
                    {
                        break;
                    }

                    yield return transform(source, newDate);
                }
            }
        }

        public static bool operator <(DateTimeRange left, DateTimeRange right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(DateTimeRange left, DateTimeRange right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(DateTimeRange left, DateTimeRange right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(DateTimeRange left, DateTimeRange right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}
