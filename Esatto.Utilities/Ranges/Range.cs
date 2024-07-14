using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities
{
    [TypeConverter(typeof(RangeTypeConverter))]
    [DataContract(Namespace = "urn:esatto:ranges")]
    [KnownType(typeof(DateRange))]
    public class Range : IEquatable<Range>, IComparable<Range>, IComparable
    {
        [DataMember]
        public int Start { get; private set; }

        [DataMember]
        public int Length { get; private set; }

        public int End => Start + Length;

        public Range(int start, int length)
        {
            if (!(start >= 0))
            {
                throw new ArgumentOutOfRangeException(nameof(start), "Contract assertion not met: start >= 0");
            }
            if (!(length > 0))
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Contract assertion not met: length > 0");
            }

            this.Start = start;
            this.Length = length;
        }

        protected virtual Range CreateRange(int newStart, int newLength)
        {
            return new Range(newStart, newLength);
        }

        public Range With(int? start = null, int? length = null)
        {
            return CreateRange(start ?? Start, length ?? Length);
        }
        public bool IsEnclosedBy(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            return (other.Start <= this.Start
                && other.End >= this.End);
        }
        public bool Contains(int other)
        {
            return this.Start <= other && this.End > other;
        }
        public bool IsIntersecting(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            return Contains(other.Start) || Contains(other.End - 1)
                || other.Contains(Start) || other.Contains(End - 1);
        }
        public bool IsTouching(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            return IsIntersecting(other)
                || other.End == this.Start
                || this.End == other.Start;
        }

        public Range? Subtract(Range other, out Range? newRange)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }
            if (!(IsIntersecting(other) && !IsEnclosedBy(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other) && !IsEnclosedBy(other)", nameof(other));
            }

            return SubtractNonIntersecting(other, out newRange);
        }

        public Range? SubtractNonIntersecting(Range other, out Range? newRange)
        {
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

            Range? left = null;
            Range? right = null;
            if (this.Contains(other.Start - 1))
            {
                left = CreateRange(this.Start, other.Start - Start);
            }

            if (this.Contains(other.End))
            {
                right = CreateRange(other.End, this.End - other.End);
            }

            if (left != null && right != null)
            {
                newRange = right;
                return left;
            }
            else
            {
                newRange = null;
                return left ?? right;
            }
        }

        public IEnumerable<Range> Subtract(Range other)
        {
            var remainder = Subtract(other, out var newRange);
            if (remainder != null)
            {
                yield return remainder;
            }
            if (newRange != null)
            {
                yield return newRange;
            }
        }

        public Range Union(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }
            if (!IsTouching(other))
            {
                throw new ArgumentException("Contract assertion not met: IsTouching(other)", nameof(other));
            }

            var newLeft = Math.Min(other.Start, this.Start);
            var newRight = Math.Max(other.End, this.End);
            return CreateRange(newLeft, newRight - newLeft);
        }

        public Range Intersect(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }
            if (!(IsIntersecting(other)))
            {
                throw new ArgumentException("Contract assertion not met: IsIntersecting(other)", nameof(other));
            }

            var newLeft = Math.Max(other.Start, this.Start);
            var newRight = Math.Min(other.End, this.End);
            return CreateRange(newLeft, newRight - newLeft);
        }

        public Range ExpandToInclude(Range other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other), "Contract assertion not met: other != null");
            }

            var left = Math.Min(other.Start, this.Start);
            var right = Math.Max(other.End, this.End);

            return CreateRange(left, right - left);
        }

        public Range Shift(int offset)
        {
            return CreateRange(Start + offset, Length);
        }

        public Range Inflate(int padLeft = 0, int padRight = 0)
        {
            if (!(padLeft + padRight + Length > 0))
            {
                throw new ArgumentException("Contract assertion not met: padLeft + padRight + Length > 0", nameof(padLeft));
            }

            return CreateRange(Start - padLeft, Length + padLeft + padRight);
        }

        public override bool Equals(object? obj) => Equals(obj as Range);

        public bool Equals(Range? other)
        {
            if (other is null)
            {
                return false;
            }

            return this.Start == other.Start
                && this.Length == other.Length;
        }

        public int CompareTo(object? obj) => CompareTo(obj as Range);

        public virtual int CompareTo(Range? other)
        {
            if (other == null)
            {
                return 1;
            }

            // sort by StartDate, then by length
            var startDateComparison = Start.CompareTo(other.Start);
            if (startDateComparison == 0)
            {
                return Length.CompareTo(other.Length);
            }
            return startDateComparison;
        }

        public override string ToString() => $"[{Start}, {End})";

        public override int GetHashCode() => unchecked(Start ^ (Length << 16));

        public static bool operator ==(Range? a, Range? b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (object.ReferenceEquals(a, null))
            {
                return false;
            }

            return a.Equals(b);
        }

        public static bool operator !=(Range? a, Range? b) => !(a == b);

        public class RangeTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
            {
                return destinationType == typeof(string)
                    || destinationType == typeof(InstanceDescriptor);
            }

            private static readonly char[] Delimiters = [' ', ','];

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
            {
                var sValue = value as string ?? throw new ArgumentOutOfRangeException(nameof(value));
                var sValues = sValue.Split(Delimiters, StringSplitOptions.RemoveEmptyEntries);

                if (sValues.Length > 2)
                {
                    throw new FormatException($"'{value}' constitutes an invalid range.  Must be in format '{{start index}}[, {{length}}]' ('1' or '1,3').");
                }
                try
                {
                    int start = int.Parse(sValues[0], culture);
                    int length = 1;
                    if (sValues.Length == 1)
                    {
                        // no-op, use assumed length of 1
                    }
                    if (sValues.Length == 2)
                    {
                        length = int.Parse(sValues[1], culture);
                    }
                    else throw new FormatException();

                    if (start < 0 || length < 1)
                    {
                        throw new FormatException();
                    }

                    return new Range(start, length);
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"Could not parse start offset or length of range '{value}'.  Start and length must be non-negative integers.  Length must be greater than 0.", ex);
                }
            }

            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (destinationType == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var range = (Range)value;

                if (destinationType == typeof(string))
                {
                    // for inheriting types, use ToString
                    if (value.GetType() != typeof(Range))
                    {
                        return range.ToString();
                    }

                    return string.Format(culture, "{0}, {1}", range.Start, range.Length);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    var ci = typeof(Range).GetConstructor(new[] { typeof(int), typeof(int) });
                    return new InstanceDescriptor(ci, new[] { range.Start, range.Length });
                }
                else throw new NotSupportedException($"Cannot convert range to type '{destinationType}'");
            }
        }

        public IEnumerable<int> AsEnumerable() => Enumerable.Range(Start, Length);

        public static bool operator <(Range left, Range right)
        {
            return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
        }

        public static bool operator <=(Range left, Range right)
        {
            return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
        }

        public static bool operator >(Range left, Range right)
        {
            return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
        }

        public static bool operator >=(Range left, Range right)
        {
            return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
        }
    }
}
