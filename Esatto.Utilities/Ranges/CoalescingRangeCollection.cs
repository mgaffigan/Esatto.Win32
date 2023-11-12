using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Esatto.Utilities
{
    public class CoalescingRangeCollection : IObservableCollection<Range>, IXmlSerializable, INotifyPropertyChanged
    {
        private readonly ObservableCollectionImpl<Range> _Allocations;

        public int Count => ((IReadOnlyCollection<Range>)_Allocations).Count;

        public Range this[int index] => ((IReadOnlyList<Range>)_Allocations)[index];

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public CoalescingRangeCollection()
        {
            this._Allocations = new ObservableCollectionImpl<Range>();
            this._Allocations.CollectionChanged += (_1, args) => CollectionChanged?.Invoke(this, args);
            ((INotifyPropertyChanged)this._Allocations).PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        XmlSchema? IXmlSerializable.GetSchema() => null;
        void IXmlSerializable.ReadXml(XmlReader reader) => ParseInternal(reader.ReadElementContentAsString(), this);
        void IXmlSerializable.WriteXml(XmlWriter writer) => writer.WriteString(ToString());

        public static CoalescingRangeCollection Parse(string s)
        {
            var result = new CoalescingRangeCollection();
            ParseInternal(s, result);
            return result;
        }

        private static void ParseInternal(string s, CoalescingRangeCollection result)
        {
            // RangeList = comma separated elements
            // Element = ACTION ELEMENT | ACTION START - END
            var parts = s.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var rest = part.Trim();

                bool invert = rest.StartsWith("^", StringComparison.Ordinal);
                if (invert) rest = rest.Substring(1).TrimStart();

                var iDash = part.IndexOf("-", StringComparison.Ordinal);
                Range elem;
                if (iDash == 0)
                {
                    throw new FormatException("Invalid range format.  Cannot start range element with dash");
                }
                else if (iDash < 0)
                {
                    elem = new Range(int.Parse(part, CultureInfo.InvariantCulture), 1);
                }
                else
                {
#pragma warning disable CA1846 // Prefer 'AsSpan' over 'Substring' - not supported in net48
                    var left = int.Parse(part.Substring(0, iDash), CultureInfo.InvariantCulture);
                    var right = int.Parse(part.Substring(iDash + 1), CultureInfo.InvariantCulture);
#pragma warning restore CA1846 // Prefer 'AsSpan' over 'Substring'
                    elem = new Range(left, right - left + 1);
                }

                if (invert)
                {
                    result.ClearRange(elem);
                }
                else
                {
                    result.AddRange(elem);
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var alloc in _Allocations)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                sb.Append(alloc.Start);
                if (alloc.Length != 1)
                {
                    sb.Append('-');
                    sb.Append(alloc.End - 1);
                }
            }
            return sb.ToString();
        }

        public void AddRange(Range newRange)
        {
            if (newRange == null)
            {
                throw new ArgumentNullException(nameof(newRange));
            }

            // if the desired range is enclosed by a compatible range, no-op
            if (_Allocations.Any(a => newRange.IsEnclosedBy(a)))
            {
                return;
            }

            ClearRange(newRange);

            // add our range
            var iLeftAllocation = _Allocations.IndexOfOrDefault(a => a.Contains(newRange.Start - 1));
            var iRightAllocation = _Allocations.IndexOfOrDefault(a => a.Contains(newRange.End));
            var leftAllocation = iLeftAllocation == null ? null : _Allocations[iLeftAllocation.Value];
            var rightAllocation = iRightAllocation == null ? null : _Allocations[iRightAllocation.Value];
            if (leftAllocation != null && rightAllocation != null)
            {
                // all one big happy family on the left and the right, coalesce
                _Allocations[iLeftAllocation!.Value] = leftAllocation.Union(newRange).Union(rightAllocation);
                _Allocations.RemoveAt(iRightAllocation!.Value);
            }
            else if (leftAllocation != null)
            {
                _Allocations[iLeftAllocation!.Value] = leftAllocation.Union(newRange);
            }
            else if (rightAllocation != null)
            {
                _Allocations[iRightAllocation!.Value] = rightAllocation.Union(newRange);
            }
            else
            {
                _Allocations.Add(newRange);
            }
        }

        public void ClearRange(Range clearedRange)
        {
            if (clearedRange == null)
            {
                throw new ArgumentNullException(nameof(clearedRange), "Contract assertion not met: clearedRange != null");
            }

            // adjust intersecting ranges
            for (int i = 0; i < _Allocations.Count;)
            {
                var cRange = _Allocations[i];

                // delete encompassed ranges
                if (cRange.IsEnclosedBy(clearedRange))
                {
                    _Allocations.RemoveAt(i);
                    // we do not increment i, since we changed what is at the current index
                    continue;
                }

                // adjust intersecting ranges
                if (cRange.IsIntersecting(clearedRange))
                {
                    _Allocations[i] = cRange.Subtract(clearedRange, out var newRange)
                        ?? throw new InvalidOperationException("Subtraction of enclosed range should have been removed");
                    if (newRange != null)
                    {
                        _Allocations.Insert(i + 1, newRange);
                        // we do not want to examine our newRange
                        i += 1;
                    }
                }

                i += 1;
            }
        }

        public bool Contains(Range range) => _Allocations.Any(a => range.IsEnclosedBy(a));
        public bool Contains(int index) => _Allocations.Any(a => a.Contains(index));

        public void Clear()
        {
            _Allocations.Clear();
        }

        public IEnumerator<Range> GetEnumerator()
        {
            return _Allocations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Allocations.GetEnumerator();
        }
    }
}
