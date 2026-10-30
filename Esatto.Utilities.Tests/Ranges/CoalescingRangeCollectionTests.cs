using System.Collections.Specialized;
using System.ComponentModel;
using System.Xml.Serialization;
using Range = Esatto.Utilities.Range;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public class CoalescingRangeCollectionTests
{
    private static Range R(int start, int length) => new(start, length);

    private static IReadOnlyList<Range> Ranges(CoalescingRangeCollection c) => c;

    // Order of the ranges is unspecified (coalescing may replace a range at any index), so compare as a set
    private static void AssertRanges(CoalescingRangeCollection c, params (int start, int length)[] expected)
    {
        var actual = Ranges(c).OrderBy(r => r.Start).Select(r => $"[{r.Start},{r.End})").ToList();
        var exp = expected.OrderBy(e => e.start).Select(e => $"[{e.start},{e.start + e.length})").ToList();
        CollectionAssert.AreEqual(exp, actual, $"Expected {string.Join(" ", exp)}; actual {string.Join(" ", actual)}");
        Assert.AreEqual(expected.Length, c.Count, "Count");
        var enumerated = Ranges(c).ToList();
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(enumerated[i], c[i], $"Indexer [{i}] disagrees with enumeration");
        }
    }

    private static CoalescingRangeCollection With(params int[] items)
    {
        var c = new CoalescingRangeCollection();
        foreach (var i in items)
        {
            c.Add(i);
        }
        return c;
    }

    private static CoalescingRangeCollection WithRanges(params (int start, int length)[] ranges)
    {
        var c = new CoalescingRangeCollection();
        foreach (var (s, l) in ranges)
        {
            c.AddRange(R(s, l));
        }
        return c;
    }

    #region Add

    [TestMethod]
    public void New_IsEmpty()
    {
        var c = new CoalescingRangeCollection();

        Assert.AreEqual(0, c.Count);
        Assert.HasCount(0, Ranges(c).ToList());
        Assert.IsFalse(c.Contains(0));
    }

    [TestMethod]
    public void Add_Single()
    {
        AssertRanges(With(5), (5, 1));
    }

    [TestMethod]
    public void Add_Consecutive_Coalesces()
    {
        AssertRanges(With(1, 2, 3), (1, 3));
    }

    [TestMethod]
    public void Add_ConsecutiveDescending_Coalesces()
    {
        AssertRanges(With(3, 2, 1), (1, 3));
    }

    [TestMethod]
    public void Add_Gap_KeepsSeparate()
    {
        AssertRanges(With(1, 3), (1, 1), (3, 1));
    }

    [TestMethod]
    public void Add_FillsGap_MergesThree()
    {
        AssertRanges(With(1, 3, 2), (1, 3));
    }

    [TestMethod]
    public void Add_FillsGap_MergesLargerNeighbours()
    {
        var c = WithRanges((0, 5), (6, 5));

        c.Add(5);

        AssertRanges(c, (0, 11));
    }

    [TestMethod]
    public void Add_Unordered()
    {
        AssertRanges(With(10, 1, 5), (1, 1), (5, 1), (10, 1));
    }

    [TestMethod]
    public void Add_Duplicate_NoChange()
    {
        AssertRanges(With(1, 1), (1, 1));
        AssertRanges(With(1, 2, 3, 2), (1, 3));
    }

    [TestMethod]
    public void Add_Zero()
    {
        AssertRanges(With(0, 1), (0, 2));
    }

    [TestMethod]
    public void AddRange_Single()
    {
        AssertRanges(WithRanges((5, 3)), (5, 3));
    }

    [TestMethod]
    public void AddRange_AdjacentAfter_Coalesces()
    {
        AssertRanges(WithRanges((5, 3), (8, 2)), (5, 5));
    }

    [TestMethod]
    public void AddRange_AdjacentBefore_Coalesces()
    {
        AssertRanges(WithRanges((8, 2), (5, 3)), (5, 5));
    }

    [TestMethod]
    public void AddRange_Overlapping_Coalesces()
    {
        AssertRanges(WithRanges((5, 5), (8, 5)), (5, 8));
        AssertRanges(WithRanges((8, 5), (5, 5)), (5, 8));
    }

    [TestMethod]
    public void AddRange_Enclosed_NoChange()
    {
        AssertRanges(WithRanges((0, 10), (3, 2)), (0, 10));
    }

    [TestMethod]
    public void AddRange_Enclosing_Replaces()
    {
        AssertRanges(WithRanges((3, 2), (0, 10)), (0, 10));
    }

    [TestMethod]
    public void AddRange_BridgesMultiple()
    {
        var c = WithRanges((0, 2), (4, 2), (8, 2), (20, 2));

        c.AddRange(R(1, 8));

        AssertRanges(c, (0, 10), (20, 2));
    }

    [TestMethod]
    public void AddRange_BetweenWithGaps_Inserted()
    {
        var c = WithRanges((0, 2), (20, 2));

        c.AddRange(R(10, 2));

        AssertRanges(c, (0, 2), (10, 2), (20, 2));
    }

    [TestMethod]
    public void AddRange_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CoalescingRangeCollection().AddRange(null!));
    }

    #endregion

    #region ClearRange / Remove / Clear

    [TestMethod]
    public void ClearRange_Middle_Splits()
    {
        var c = WithRanges((1, 10));

        c.ClearRange(R(3, 2));

        AssertRanges(c, (1, 2), (5, 6));
    }

    [TestMethod]
    public void ClearRange_Start()
    {
        var c = WithRanges((1, 10));

        c.ClearRange(R(0, 3));

        AssertRanges(c, (3, 8));
    }

    [TestMethod]
    public void ClearRange_End()
    {
        var c = WithRanges((1, 10));

        c.ClearRange(R(8, 10));

        AssertRanges(c, (1, 7));
    }

    [TestMethod]
    public void ClearRange_Whole()
    {
        var c = WithRanges((1, 10));

        c.ClearRange(R(1, 10));

        AssertRanges(c);
    }

    [TestMethod]
    public void ClearRange_Enclosing()
    {
        var c = WithRanges((5, 2));

        c.ClearRange(R(0, 20));

        AssertRanges(c);
    }

    [TestMethod]
    public void ClearRange_NonIntersecting_NoChange()
    {
        var c = WithRanges((1, 3), (10, 3));

        c.ClearRange(R(5, 3));

        AssertRanges(c, (1, 3), (10, 3));
    }

    [TestMethod]
    public void ClearRange_Adjacent_NoChange()
    {
        var c = WithRanges((5, 3));

        c.ClearRange(R(8, 2));
        c.ClearRange(R(3, 2));

        AssertRanges(c, (5, 3));
    }

    [TestMethod]
    public void ClearRange_AcrossMultiple()
    {
        var c = WithRanges((0, 5), (10, 5), (20, 5), (30, 5));

        c.ClearRange(R(3, 20));

        AssertRanges(c, (0, 3), (23, 2), (30, 5));
    }

    [TestMethod]
    public void ClearRange_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CoalescingRangeCollection().ClearRange(null!));
    }

    [TestMethod]
    public void Remove_Middle_Splits()
    {
        var c = With(1, 2, 3);

        Assert.IsTrue(c.Remove(2));

        AssertRanges(c, (1, 1), (3, 1));
    }

    [TestMethod]
    public void Remove_First()
    {
        var c = With(1, 2, 3);

        Assert.IsTrue(c.Remove(1));

        AssertRanges(c, (2, 2));
    }

    [TestMethod]
    public void Remove_Last()
    {
        var c = With(1, 2, 3);

        Assert.IsTrue(c.Remove(3));

        AssertRanges(c, (1, 2));
    }

    [TestMethod]
    public void Remove_Only()
    {
        var c = With(7);

        Assert.IsTrue(c.Remove(7));

        AssertRanges(c);
    }

    [TestMethod]
    public void Remove_Absent_ReturnsFalse()
    {
        var c = With(1, 2, 3, 7);

        Assert.IsFalse(c.Remove(5));
        Assert.IsFalse(c.Remove(0));
        Assert.IsFalse(c.Remove(4));
        Assert.IsFalse(c.Remove(100));

        AssertRanges(c, (1, 3), (7, 1));
    }

    [TestMethod]
    public void Remove_FromEmpty_ReturnsFalse()
    {
        Assert.IsFalse(new CoalescingRangeCollection().Remove(1));
    }

    [TestMethod]
    public void Clear_Empties()
    {
        var c = WithRanges((0, 5), (10, 5));

        c.Clear();

        AssertRanges(c);
        Assert.IsFalse(c.Contains(0));
    }

    #endregion

    #region Contains / enumeration

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(9, false)]
    [DataRow(10, true)]
    [DataRow(14, true)]
    [DataRow(15, false)]
    public void Contains_Int(int value, bool expected)
    {
        var c = WithRanges((1, 3), (10, 5));

        Assert.AreEqual(expected, c.Contains(value));
        Assert.AreEqual(expected, ((ICollection<int>)c).Contains(value));
    }

    [TestMethod]
    [DataRow(1, 3, true)]
    [DataRow(2, 1, true)]
    [DataRow(10, 5, true)]
    [DataRow(11, 2, true)]
    [DataRow(0, 2, false)]
    [DataRow(3, 2, false)]
    [DataRow(1, 4, false)]
    [DataRow(2, 10, false)]
    [DataRow(1, 14, false)]
    [DataRow(5, 2, false)]
    [DataRow(14, 2, false)]
    public void Contains_Range(int start, int length, bool expected)
    {
        var c = WithRanges((1, 3), (10, 5));

        Assert.AreEqual(expected, c.Contains(R(start, length)));
    }

    [TestMethod]
    public void Enumerate_Ranges_YieldsEachRange()
    {
        var c = WithRanges((10, 5), (1, 3));

        var list = new List<Range>();
        foreach (Range r in c)
        {
            list.Add(r);
        }

        CollectionAssert.AreEquivalent(new[] { R(1, 3), R(10, 5) }, list);
    }

    [TestMethod]
    public void Enumerate_AsInts_YieldsEachMember()
    {
        // As an ICollection<int> it enumerates the member integers (order unspecified).
        var c = WithRanges((10, 2), (1, 3));

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 10, 11 }, ((IEnumerable<int>)c).ToList());
    }

    [TestMethod]
    public void AsICollectionOfInt_CopyTo()
    {
        // Assumed contract: as an ICollection<int>, CopyTo copies the member integers in order.
        ICollection<int> c = WithRanges((1, 3));
        var array = new int[5];

        c.CopyTo(array, 1);

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 0 }, array);
    }

    [TestMethod]
    public void AsICollectionOfInt_CountIsNumberOfIntegers()
    {
        // Assumed contract: ICollection<int>.Count must agree with what ICollection<int> enumerates/copies
        // (otherwise LINQ ToList/ToArray, which size by Count then call CopyTo, break).
        ICollection<int> c = WithRanges((1, 3), (10, 2));

        // Deliberately checks the Count property, not the enumerated count
#pragma warning disable MSTEST0037
        Assert.AreEqual(5, c.Count);
#pragma warning restore MSTEST0037
    }

    [TestMethod]
    public void AsICollectionOfInt_IsNotReadOnly()
    {
        Assert.IsFalse(((ICollection<int>)new CoalescingRangeCollection()).IsReadOnly);
    }

    #endregion

    #region Parse / ToString / Xml

    [TestMethod]
    [DataRow("1-3,5", new[] { 1, 3, 5, 1 })]
    [DataRow("5", new[] { 5, 1 })]
    [DataRow("0-9", new[] { 0, 10 })]
    [DataRow("1-3,4-6", new[] { 1, 6 })]
    [DataRow("1-3,2-6", new[] { 1, 6 })]
    [DataRow("5,1-3", new[] { 1, 3, 5, 1 })]
    [DataRow("1,2,3", new[] { 1, 3 })]
    [DataRow("1-1", new[] { 1, 1 })]
    public void Parse(string text, int[] startLengthPairs)
    {
        var c = CoalescingRangeCollection.Parse(text);

        var expected = new List<(int, int)>();
        for (int i = 0; i < startLengthPairs.Length; i += 2)
        {
            expected.Add((startLengthPairs[i], startLengthPairs[i + 1]));
        }
        AssertRanges(c, expected.ToArray());
    }

    [TestMethod]
    public void Parse_Whitespace_Tolerated()
    {
        // Assumed contract: whitespace is tolerated as in IntRange.GetRange.
        AssertRanges(CoalescingRangeCollection.Parse(" 1 - 3 , 5 "), (1, 3), (5, 1));
    }

    [TestMethod]
    public void Parse_Empty_IsEmpty()
    {
        AssertRanges(CoalescingRangeCollection.Parse(""));
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("1,-3")]
    [DataRow("-1-3")]
    public void Parse_LeadingDash_Throws(string text)
    {
        // Assumed contract: invalid text throws FormatException (conventional for Parse).
        Assert.Throws<FormatException>(() => CoalescingRangeCollection.Parse(text));
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("1-x")]
    public void Parse_Invalid_Throws(string text)
    {
        Assert.Throws<FormatException>(() => CoalescingRangeCollection.Parse(text));
    }

    [TestMethod]
    public void Parse_ReversedRange_Throws()
    {
        // ArgumentOutOfRangeException from Range is the existing (public) behavior
        Assert.Throws<ArgumentOutOfRangeException>(() => CoalescingRangeCollection.Parse("3-1"));
    }

    [TestMethod]
    [DataRow("1-10,^5", "1-4,6-10")]
    [DataRow("1-10;^3-4", "1-2,5-10")]
    [DataRow("1-10, ^ 5", "1-4,6-10")]
    public void Parse_Caret_RemovesElement(string text, string expected)
    {
        // Grammar from the source comment: Element = ACTION ELEMENT | ACTION START - END, where '^' clears.
        Assert.AreEqual(expected, CoalescingRangeCollection.Parse(text).ToString());
    }

    [TestMethod]
    public void ToString_Format()
    {
        Assert.AreEqual("1-3,5", WithRanges((1, 3), (5, 1)).ToString());
        Assert.AreEqual("5", With(5).ToString());
        Assert.AreEqual("0-9,20-21", WithRanges((0, 10), (20, 2)).ToString());
    }

    [TestMethod]
    public void ToString_Empty()
    {
        Assert.AreEqual("", new CoalescingRangeCollection().ToString());
    }

    [TestMethod]
    [DataRow("1-3,5")]
    [DataRow("0")]
    [DataRow("0-9,20-21,30,40-100")]
    public void ParseToString_RoundTrips(string text)
    {
        Assert.AreEqual(text, CoalescingRangeCollection.Parse(text).ToString());
    }

    private static CoalescingRangeCollection XmlRoundTrip(CoalescingRangeCollection value)
    {
        var serializer = new XmlSerializer(typeof(CoalescingRangeCollection));
        using var sw = new StringWriter();
        serializer.Serialize(sw, value);
        using var sr = new StringReader(sw.ToString());
        return (CoalescingRangeCollection)serializer.Deserialize(sr)!;
    }

    [TestMethod]
    public void XmlSerializer_RoundTrips()
    {
        var result = XmlRoundTrip(WithRanges((0, 10), (20, 2), (30, 1)));

        AssertRanges(result, (0, 10), (20, 2), (30, 1));
    }

    [TestMethod]
    public void XmlSerializer_Empty_RoundTrips()
    {
        AssertRanges(XmlRoundTrip(new CoalescingRangeCollection()));
    }

    public class Holder
    {
        public CoalescingRangeCollection? Pages { get; set; }
        public string? After { get; set; }
    }

    [TestMethod]
    public void XmlSerializer_AsProperty_RoundTripsAndDoesNotConsumeSiblings()
    {
        var serializer = new XmlSerializer(typeof(Holder));
        var holder = new Holder { Pages = WithRanges((1, 3), (5, 1)), After = "sentinel" };

        using var sw = new StringWriter();
        serializer.Serialize(sw, holder);
        using var sr = new StringReader(sw.ToString());
        var result = (Holder)serializer.Deserialize(sr)!;

        Assert.IsNotNull(result.Pages);
        AssertRanges(result.Pages, (1, 3), (5, 1));
        Assert.AreEqual("sentinel", result.After, sw.ToString());
    }

    [TestMethod]
    public void XmlSerializer_EmptyAsProperty_DoesNotConsumeSiblings()
    {
        var serializer = new XmlSerializer(typeof(Holder));
        var holder = new Holder { Pages = new CoalescingRangeCollection(), After = "sentinel" };

        using var sw = new StringWriter();
        serializer.Serialize(sw, holder);
        using var sr = new StringReader(sw.ToString());
        var result = (Holder)serializer.Deserialize(sr)!;

        Assert.IsNotNull(result.Pages);
        AssertRanges(result.Pages);
        Assert.AreEqual("sentinel", result.After, sw.ToString());
    }

    #endregion

    #region Change notification

    /// <summary>
    /// Mirrors the collection by replaying CollectionChanged events, the way a bound
    /// view would. Assumed contract: events are replayable like ObservableCollection's.
    /// </summary>
    private sealed class Mirror
    {
        private readonly CoalescingRangeCollection Source;
        public readonly List<Range> Items;
        public int CollectionChangedCount;
        public readonly List<string?> PropertyNames = new();

        public Mirror(CoalescingRangeCollection source)
        {
            Source = source;
            Items = Ranges(source).ToList();
            source.CollectionChanged += OnCollectionChanged;
            source.PropertyChanged += (s, e) => PropertyNames.Add(e.PropertyName);
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Assert.AreSame(Source, sender);
            CollectionChangedCount++;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Assert.IsGreaterThanOrEqualTo(0, e.NewStartingIndex, "Add without index");
                    Items.InsertRange(e.NewStartingIndex, e.NewItems!.Cast<Range>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Assert.IsGreaterThanOrEqualTo(0, e.OldStartingIndex, "Remove without index");
                    for (int i = 0; i < e.OldItems!.Count; i++)
                    {
                        Assert.AreEqual((Range)e.OldItems[i]!, Items[e.OldStartingIndex + i], "Removed item mismatch");
                    }
                    Items.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Assert.IsGreaterThanOrEqualTo(0, e.NewStartingIndex, "Replace without index");
                    Assert.HasCount(e.OldItems!.Count, e.NewItems!.Cast<object>());
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        Assert.AreEqual((Range)e.OldItems[i]!, Items[e.OldStartingIndex + i], "Replaced item mismatch");
                        Items[e.NewStartingIndex + i] = (Range)e.NewItems![i]!;
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    var moved = Items.GetRange(e.OldStartingIndex, e.OldItems!.Count);
                    Items.RemoveRange(e.OldStartingIndex, moved.Count);
                    Items.InsertRange(e.NewStartingIndex, moved);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    Items.AddRange(Ranges(Source));
                    break;
            }
        }

        public void AssertInSync(string step)
        {
            CollectionAssert.AreEqual(Ranges(Source).ToList(), Items,
                $"After {step}: source {string.Join(" ", Ranges(Source))}; mirror {string.Join(" ", Items)}");
        }
    }

    [TestMethod]
    public void Add_RaisesCollectionChangedAndPropertyChanged()
    {
        var c = new CoalescingRangeCollection();
        var m = new Mirror(c);

        c.Add(1);

        Assert.IsGreaterThan(0, m.CollectionChangedCount);
        Assert.IsGreaterThan(0, m.PropertyNames.Count);
        m.AssertInSync("Add(1)");
    }

    [TestMethod]
    public void CountChange_RaisesPropertyChangedForCount()
    {
        var c = new CoalescingRangeCollection();
        var m = new Mirror(c);

        c.Add(1);

        Assert.Contains("Count", m.PropertyNames);
    }

    [TestMethod]
    public void Events_ReplayToMatchingState()
    {
        var c = new CoalescingRangeCollection();
        var m = new Mirror(c);

        c.Add(5); m.AssertInSync("Add(5)");
        c.Add(1); m.AssertInSync("Add(1)");
        c.Add(10); m.AssertInSync("Add(10)");
        c.Add(6); m.AssertInSync("Add(6) extend");
        c.Add(4); m.AssertInSync("Add(4) extend left");
        c.AddRange(R(2, 2)); m.AssertInSync("AddRange [2,4) bridge");
        c.AddRange(R(20, 5)); m.AssertInSync("AddRange [20,25)");
        c.AddRange(R(8, 14)); m.AssertInSync("AddRange [8,22) bridge many");
        c.Remove(15); m.AssertInSync("Remove(15) split");
        c.Remove(1); m.AssertInSync("Remove(1) trim");
        c.ClearRange(R(3, 10)); m.AssertInSync("ClearRange [3,13)");
        c.ClearRange(R(0, 100)); m.AssertInSync("ClearRange all");
        c.AddRange(R(0, 3)); m.AssertInSync("AddRange [0,3)");
        c.Clear(); m.AssertInSync("Clear");
    }

    [TestMethod]
    public void Remove_RaisesCollectionChanged()
    {
        var c = With(1, 2, 3);
        var m = new Mirror(c);

        c.Remove(2);

        Assert.IsGreaterThan(0, m.CollectionChangedCount);
        m.AssertInSync("Remove(2)");
    }

    [TestMethod]
    public void ClearRange_RaisesCollectionChanged()
    {
        var c = WithRanges((0, 10));
        var m = new Mirror(c);

        c.ClearRange(R(0, 5));

        Assert.IsGreaterThan(0, m.CollectionChangedCount);
        m.AssertInSync("ClearRange");
    }

    [TestMethod]
    public void Clear_RaisesCollectionChanged()
    {
        var c = WithRanges((0, 10));
        var m = new Mirror(c);

        c.Clear();

        Assert.IsGreaterThan(0, m.CollectionChangedCount);
        m.AssertInSync("Clear");
    }

    [TestMethod]
    public void NoOpChanges_DoNotRaiseCollectionChanged()
    {
        // Assumed contract: events are raised only when the contents change.
        var c = WithRanges((0, 10));
        var m = new Mirror(c);

        c.Add(5);
        c.AddRange(R(2, 3));
        c.Remove(50);
        c.ClearRange(R(20, 5));

        Assert.AreEqual(0, m.CollectionChangedCount);
    }

    #endregion
}
