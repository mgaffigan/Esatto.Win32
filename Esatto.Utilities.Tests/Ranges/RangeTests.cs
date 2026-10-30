using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;
using Range = Esatto.Utilities.Range;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public class RangeTests
{
    private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(Range));

    private static Range R(int start, int length) => new(start, length);

    private static void AssertRange(int start, int length, Range? actual, string? message = null)
    {
        Assert.IsNotNull(actual, message);
        Assert.AreEqual(start, actual.Start, $"Start. {message}");
        Assert.AreEqual(length, actual.Length, $"Length. {message}");
        Assert.AreEqual(start + length, actual.End, $"End. {message}");
    }

    private static T DataContractRoundTrip<T>(T value)
    {
        var serializer = new DataContractSerializer(typeof(T));
        using var ms = new MemoryStream();
        serializer.WriteObject(ms, value);
        ms.Position = 0;
        return (T)serializer.ReadObject(ms)!;
    }

    #region Construction

    [TestMethod]
    public void Ctor_SetsStartLengthEnd()
    {
        var r = R(5, 3);

        Assert.AreEqual(5, r.Start);
        Assert.AreEqual(3, r.Length);
        Assert.AreEqual(8, r.End);
    }

    [TestMethod]
    public void Ctor_ZeroStart_Allowed()
    {
        AssertRange(0, 1, R(0, 1));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_NonPositiveLength_Throws(int length)
    {
        Assert.Throws<ArgumentException>(() => new Range(5, length));
    }

    [TestMethod]
    public void Ctor_NegativeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Range(-1, 3));
    }

    #endregion

    #region Contains / AsEnumerable

    [TestMethod]
    [DataRow(4, false)]
    [DataRow(5, true)]
    [DataRow(6, true)]
    [DataRow(7, true)]
    [DataRow(8, false)]
    public void Contains_IsHalfOpen(int value, bool expected)
    {
        Assert.AreEqual(expected, R(5, 3).Contains(value));
    }

    [TestMethod]
    public void AsEnumerable_YieldsEachInteger()
    {
        CollectionAssert.AreEqual(new[] { 5, 6, 7 }, R(5, 3).AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_LengthOne_YieldsStart()
    {
        CollectionAssert.AreEqual(new[] { 0 }, R(0, 1).AsEnumerable().ToList());
    }

    #endregion

    #region IsIntersecting / IsTouching / IsEnclosedBy

    [TestMethod]
    // overlap
    [DataRow(0, 5, 3, 5, true)]
    [DataRow(3, 5, 0, 5, true)]
    // enclosed both directions
    [DataRow(0, 10, 3, 2, true)]
    [DataRow(3, 2, 0, 10, true)]
    // identical
    [DataRow(2, 3, 2, 3, true)]
    // single-point overlap
    [DataRow(0, 5, 4, 5, true)]
    // adjacent both directions
    [DataRow(0, 5, 5, 5, false)]
    [DataRow(5, 5, 0, 5, false)]
    // disjoint both directions
    [DataRow(0, 5, 6, 5, false)]
    [DataRow(6, 5, 0, 5, false)]
    public void IsIntersecting(int s1, int l1, int s2, int l2, bool expected)
    {
        Assert.AreEqual(expected, R(s1, l1).IsIntersecting(R(s2, l2)));
    }

    [TestMethod]
    [DataRow(0, 5, 3, 5, true)]
    [DataRow(0, 10, 3, 2, true)]
    [DataRow(3, 2, 0, 10, true)]
    [DataRow(2, 3, 2, 3, true)]
    // adjacent both directions
    [DataRow(0, 5, 5, 5, true)]
    [DataRow(5, 5, 0, 5, true)]
    // gap of one
    [DataRow(0, 5, 6, 5, false)]
    [DataRow(6, 5, 0, 5, false)]
    public void IsTouching(int s1, int l1, int s2, int l2, bool expected)
    {
        Assert.AreEqual(expected, R(s1, l1).IsTouching(R(s2, l2)));
    }

    [TestMethod]
    // strictly inside
    [DataRow(3, 2, 0, 10, true)]
    // outer is not enclosed by inner
    [DataRow(0, 10, 3, 2, false)]
    // identical
    [DataRow(2, 3, 2, 3, true)]
    // shared start / shared end
    [DataRow(0, 3, 0, 10, true)]
    [DataRow(7, 3, 0, 10, true)]
    // partial overlap both sides
    [DataRow(0, 5, 3, 5, false)]
    [DataRow(3, 5, 0, 5, false)]
    // extends one past
    [DataRow(8, 3, 0, 10, false)]
    // adjacent / disjoint
    [DataRow(10, 2, 0, 10, false)]
    [DataRow(20, 2, 0, 10, false)]
    public void IsEnclosedBy(int s1, int l1, int s2, int l2, bool expected)
    {
        Assert.AreEqual(expected, R(s1, l1).IsEnclosedBy(R(s2, l2)));
    }

    #endregion

    #region Null arguments

    public static IEnumerable<object[]> NullArgOperations()
    {
        yield return ["IsIntersecting"];
        yield return ["IsTouching"];
        yield return ["IsEnclosedBy"];
        yield return ["Union"];
        yield return ["Intersect"];
        yield return ["ExpandToInclude"];
        yield return ["Subtract(out)"];
        yield return ["Subtract(IEnumerable)"];
        yield return ["SubtractNonIntersecting(out)"];
    }

    private static void InvokeOp(string op, Range a, Range b)
    {
        switch (op)
        {
            case "IsIntersecting": a.IsIntersecting(b); break;
            case "IsTouching": a.IsTouching(b); break;
            case "IsEnclosedBy": a.IsEnclosedBy(b); break;
            case "Union": a.Union(b); break;
            case "Intersect": a.Intersect(b); break;
            case "ExpandToInclude": a.ExpandToInclude(b); break;
            case "Subtract(out)": a.Subtract(b, out _); break;
            case "Subtract(IEnumerable)": a.Subtract(b).ToList(); break;
            case "SubtractNonIntersecting(out)": a.SubtractNonIntersecting(b, out _); break;
            default: throw new ArgumentOutOfRangeException(nameof(op));
        }
    }

    [TestMethod]
    [DynamicData(nameof(NullArgOperations))]
    public void NullArgument_ThrowsArgumentNullException(string op)
    {
        Assert.Throws<ArgumentNullException>(() => InvokeOp(op, R(0, 5), null!));
    }

    #endregion

    #region Union / Intersect / ExpandToInclude

    [TestMethod]
    public void Union_Overlapping()
    {
        AssertRange(0, 10, R(0, 5).Union(R(3, 7)));
        AssertRange(0, 10, R(3, 7).Union(R(0, 5)));
    }

    [TestMethod]
    public void Union_Adjacent()
    {
        AssertRange(0, 10, R(0, 5).Union(R(5, 5)));
        AssertRange(0, 10, R(5, 5).Union(R(0, 5)));
    }

    [TestMethod]
    public void Union_Enclosed_ReturnsOuter()
    {
        AssertRange(0, 10, R(0, 10).Union(R(3, 2)));
        AssertRange(0, 10, R(3, 2).Union(R(0, 10)));
    }

    [TestMethod]
    public void Union_Identical()
    {
        AssertRange(2, 3, R(2, 3).Union(R(2, 3)));
    }

    [TestMethod]
    public void Union_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(0, 5).Union(R(6, 5)));
        Assert.Throws<ArgumentException>(() => R(6, 5).Union(R(0, 5)));
    }

    [TestMethod]
    public void Intersect_Overlapping()
    {
        AssertRange(3, 2, R(0, 5).Intersect(R(3, 7)));
        AssertRange(3, 2, R(3, 7).Intersect(R(0, 5)));
    }

    [TestMethod]
    public void Intersect_Enclosed_ReturnsInner()
    {
        AssertRange(3, 2, R(0, 10).Intersect(R(3, 2)));
        AssertRange(3, 2, R(3, 2).Intersect(R(0, 10)));
    }

    [TestMethod]
    public void Intersect_SinglePoint()
    {
        AssertRange(4, 1, R(0, 5).Intersect(R(4, 5)));
    }

    [TestMethod]
    public void Intersect_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(0, 5).Intersect(R(5, 5)));
        Assert.Throws<ArgumentException>(() => R(5, 5).Intersect(R(0, 5)));
    }

    [TestMethod]
    public void Intersect_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(0, 5).Intersect(R(10, 5)));
    }

    [TestMethod]
    public void ExpandToInclude_Disjoint_CoversBoth()
    {
        AssertRange(0, 12, R(0, 2).ExpandToInclude(R(10, 2)));
        AssertRange(0, 12, R(10, 2).ExpandToInclude(R(0, 2)));
    }

    [TestMethod]
    public void ExpandToInclude_Enclosed_ReturnsOuter()
    {
        AssertRange(0, 10, R(0, 10).ExpandToInclude(R(3, 2)));
        AssertRange(0, 10, R(3, 2).ExpandToInclude(R(0, 10)));
    }

    [TestMethod]
    public void ExpandToInclude_Overlapping()
    {
        AssertRange(0, 10, R(0, 5).ExpandToInclude(R(3, 7)));
    }

    #endregion

    #region Subtract

    [TestMethod]
    public void Subtract_Middle_SplitsInTwo()
    {
        var left = R(0, 10).Subtract(R(3, 2), out var right);

        AssertRange(0, 3, left);
        AssertRange(5, 5, right);
    }

    [TestMethod]
    public void Subtract_OverlapsLeftSide_ReturnsRightRemainder()
    {
        var result = R(5, 5).Subtract(R(0, 7), out var newRange);

        AssertRange(7, 3, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_OverlapsRightSide_ReturnsLeftRemainder()
    {
        var result = R(5, 5).Subtract(R(8, 10), out var newRange);

        AssertRange(5, 3, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_SharedStart_ReturnsRightRemainder()
    {
        var result = R(5, 5).Subtract(R(5, 2), out var newRange);

        AssertRange(7, 3, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_SharedEnd_ReturnsLeftRemainder()
    {
        var result = R(5, 5).Subtract(R(8, 2), out var newRange);

        AssertRange(5, 3, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_LeavesSinglePointEachSide()
    {
        var left = R(0, 3).Subtract(R(1, 1), out var right);

        AssertRange(0, 1, left);
        AssertRange(2, 1, right);
    }

    [TestMethod]
    [DataRow(0, 10, DisplayName = "Identical")]
    [DataRow(0, 20, DisplayName = "Shared start, longer")]
    public void Subtract_EnclosingOther_Throws(int start, int length)
    {
        Assert.Throws<ArgumentException>(() => R(0, 10).Subtract(R(start, length), out _));
    }

    [TestMethod]
    public void Subtract_StrictlyEnclosingOther_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(5, 5).Subtract(R(0, 20), out _));
    }

    [TestMethod]
    public void Subtract_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(0, 5).Subtract(R(10, 5), out _));
    }

    [TestMethod]
    public void Subtract_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(0, 5).Subtract(R(5, 5), out _));
        Assert.Throws<ArgumentException>(() => R(5, 5).Subtract(R(0, 5), out _));
    }

    [TestMethod]
    public void Subtract_DoesNotMutateOperands()
    {
        var a = R(0, 10);
        var b = R(3, 2);

        a.Subtract(b, out _);

        AssertRange(0, 10, a);
        AssertRange(3, 2, b);
    }

    [TestMethod]
    public void SubtractEnumerable_Middle_YieldsBothPiecesInOrder()
    {
        var result = R(0, 10).Subtract(R(3, 2)).ToList();

        Assert.HasCount(2, result);
        AssertRange(0, 3, result[0]);
        AssertRange(5, 5, result[1]);
    }

    [TestMethod]
    public void SubtractEnumerable_OneSide_YieldsOnePiece()
    {
        var result = R(5, 5).Subtract(R(0, 7)).ToList();

        Assert.HasCount(1, result);
        AssertRange(7, 3, result[0]);
    }

    [TestMethod]
    public void SubtractEnumerable_RightSide_YieldsOnePiece()
    {
        var result = R(5, 5).Subtract(R(8, 10)).ToList();

        Assert.HasCount(1, result);
        AssertRange(5, 3, result[0]);
    }

    [TestMethod]
    public void SubtractEnumerable_Enclosing_Throws()
    {
        // Assumed contract: the IEnumerable overload of Subtract has the same preconditions as Subtract(other, out).
        Assert.Throws<ArgumentException>(() => R(5, 5).Subtract(R(0, 20)).ToList());
    }

    [TestMethod]
    public void SubtractNonIntersecting_Disjoint_ReturnsThis()
    {
        var result = R(0, 5).SubtractNonIntersecting(R(10, 5), out var newRange);

        AssertRange(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Adjacent_ReturnsThis()
    {
        var result = R(0, 5).SubtractNonIntersecting(R(5, 5), out var newRange);
        AssertRange(0, 5, result);
        Assert.IsNull(newRange);

        result = R(5, 5).SubtractNonIntersecting(R(0, 5), out newRange);
        AssertRange(5, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Enclosed_ReturnsNull()
    {
        var result = R(5, 5).SubtractNonIntersecting(R(0, 20), out var newRange);

        Assert.IsNull(result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Identical_ReturnsNull()
    {
        var result = R(5, 5).SubtractNonIntersecting(R(5, 5), out var newRange);

        Assert.IsNull(result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Middle_SplitsInTwo()
    {
        var left = R(0, 10).SubtractNonIntersecting(R(3, 2), out var right);

        AssertRange(0, 3, left);
        AssertRange(5, 5, right);
    }

    [TestMethod]
    public void SubtractNonIntersecting_OneSide()
    {
        var result = R(5, 5).SubtractNonIntersecting(R(0, 7), out var newRange);
        AssertRange(7, 3, result);
        Assert.IsNull(newRange);

        result = R(5, 5).SubtractNonIntersecting(R(8, 10), out newRange);
        AssertRange(5, 3, result);
        Assert.IsNull(newRange);
    }

    #endregion

    #region Shift / Inflate / With

    [TestMethod]
    public void Shift_Positive()
    {
        AssertRange(8, 5, R(5, 5).Shift(3));
    }

    [TestMethod]
    public void Shift_Negative()
    {
        AssertRange(3, 5, R(5, 5).Shift(-2));
    }

    [TestMethod]
    public void Shift_Zero_ReturnsEqual()
    {
        AssertRange(5, 5, R(5, 5).Shift(0));
    }

    [TestMethod]
    public void Shift_DoesNotMutateOriginal()
    {
        var r = R(5, 5);
        r.Shift(3);
        AssertRange(5, 5, r);
    }

    [TestMethod]
    public void Shift_ToNegativeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(5, 5).Shift(-6));
    }

    [TestMethod]
    public void Inflate_Positive()
    {
        AssertRange(3, 10, R(5, 5).Inflate(2, 3));
    }

    [TestMethod]
    public void Inflate_LeftOnly()
    {
        AssertRange(3, 7, R(5, 5).Inflate(padLeft: 2));
    }

    [TestMethod]
    public void Inflate_RightOnly()
    {
        AssertRange(5, 8, R(5, 5).Inflate(padRight: 3));
    }

    [TestMethod]
    public void Inflate_Negative_Shrinks()
    {
        AssertRange(6, 3, R(5, 5).Inflate(-1, -1));
    }

    [TestMethod]
    public void Inflate_Default_ReturnsEqual()
    {
        AssertRange(5, 5, R(5, 5).Inflate());
    }

    [TestMethod]
    public void Inflate_ToZeroLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(5, 4).Inflate(-2, -2));
    }

    [TestMethod]
    public void Inflate_ToNegativeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(1, 4).Inflate(2, 0));
    }

    [TestMethod]
    public void With_Start_KeepsLength()
    {
        AssertRange(2, 5, R(5, 5).With(start: 2));
    }

    [TestMethod]
    public void With_Length_KeepsStart()
    {
        AssertRange(5, 2, R(5, 5).With(length: 2));
    }

    [TestMethod]
    public void With_Both()
    {
        AssertRange(1, 9, R(5, 5).With(1, 9));
    }

    [TestMethod]
    public void With_Nothing_ReturnsEqual()
    {
        AssertRange(5, 5, R(5, 5).With());
    }

    [TestMethod]
    public void With_InvalidLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => R(5, 5).With(length: 0));
    }

    #endregion

    #region Subclass via CreateRange

    private sealed class TaggedRange(int start, int length) : Range(start, length)
    {
        protected override Range CreateRange(int newStart, int newLength) => new TaggedRange(newStart, newLength);
    }

    [TestMethod]
    public void Operations_UseCreateRange_ToPreserveSubclass()
    {
        // Assumed contract: CreateRange is the factory used by all range-producing operations.
        Range r = new TaggedRange(5, 5);
        Range other = new TaggedRange(8, 5);

        Assert.IsInstanceOfType<TaggedRange>(r.Shift(1), "Shift");
        Assert.IsInstanceOfType<TaggedRange>(r.Inflate(1, 1), "Inflate");
        Assert.IsInstanceOfType<TaggedRange>(r.With(start: 1), "With");
        Assert.IsInstanceOfType<TaggedRange>(r.Union(other), "Union");
        Assert.IsInstanceOfType<TaggedRange>(r.Intersect(other), "Intersect");
        Assert.IsInstanceOfType<TaggedRange>(r.ExpandToInclude(other), "ExpandToInclude");
        Assert.IsInstanceOfType<TaggedRange>(r.Subtract(other, out _), "Subtract");
    }

    #endregion

    #region Equality / comparison

    [TestMethod]
    public void Equals_SameValues_True()
    {
        var a = R(5, 3);
        var b = R(5, 3);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    [DataRow(6, 3)]
    [DataRow(5, 4)]
    public void Equals_DifferentValues_False(int start, int length)
    {
        var a = R(5, 3);
        var b = R(start, length);

        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals((object)b));
        Assert.IsFalse(a == b);
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void Equals_Null_False()
    {
        var a = R(5, 3);

        Assert.IsFalse(a.Equals((Range?)null));
        Assert.IsFalse(a.Equals((object?)null));
        Assert.IsFalse(a == null);
        Assert.IsFalse(null == a);
        Assert.IsTrue(a != null);
    }

    [TestMethod]
    public void EqualityOperator_BothNull_True()
    {
        Range? a = null;
        Range? b = null;

        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
    }

    [TestMethod]
    public void Equals_OtherType_False()
    {
        Assert.IsFalse(R(5, 3).Equals("5, 3"));
    }

    [TestMethod]
    public void CompareTo_OrdersByStartThenLength()
    {
        Assert.IsLessThan(0, R(1, 10).CompareTo(R(2, 1)));
        Assert.IsGreaterThan(0, R(2, 1).CompareTo(R(1, 10)));
        Assert.IsLessThan(0, R(1, 2).CompareTo(R(1, 3)));
        Assert.IsGreaterThan(0, R(1, 3).CompareTo(R(1, 2)));
        Assert.AreEqual(0, R(1, 3).CompareTo(R(1, 3)));
    }

    [TestMethod]
    public void CompareToObject_MatchesTyped()
    {
        Assert.IsLessThan(0, R(1, 10).CompareTo((object)R(2, 1)));
        Assert.IsGreaterThan(0, R(1, 3).CompareTo((object)R(1, 2)));
        Assert.AreEqual(0, R(1, 3).CompareTo((object)R(1, 3)));
    }

    [TestMethod]
    public void CompareTo_Null_IsGreater()
    {
        Assert.IsGreaterThan(0, R(1, 3).CompareTo((Range?)null));
        Assert.IsGreaterThan(0, R(1, 3).CompareTo((object?)null));
    }

    [TestMethod]
    public void CompareToObject_OtherType_DoesNotThrow()
    {
        _ = R(1, 3).CompareTo("hello");
    }

    [TestMethod]
    public void ComparisonOperators()
    {
        var a = R(1, 3);
        var b = R(2, 1);
        var a2 = R(1, 3);

        Assert.IsTrue(a < b);
        Assert.IsTrue(a <= b);
        Assert.IsFalse(a > b);
        Assert.IsFalse(a >= b);
        Assert.IsTrue(b > a);
        Assert.IsTrue(b >= a);
        Assert.IsTrue(a <= a2);
        Assert.IsTrue(a >= a2);
        Assert.IsFalse(a < a2);
        Assert.IsFalse(a > a2);
    }

    [TestMethod]
    public void ComparisonOperators_WithNull_NullSortsFirst()
    {
        // Assumed contract: operators follow CompareTo, with null sorting first (and not throwing).
        Range? n = null;
        var a = R(1, 3);

        Assert.IsTrue(n! < a);
        Assert.IsTrue(n! <= a);
        Assert.IsFalse(n! > a);
        Assert.IsTrue(a > n!);
        Assert.IsTrue(a >= n!);
        Assert.IsFalse(a < n!);
    }

    [TestMethod]
    public void Sort_OrdersByStartThenLength()
    {
        var list = new List<Range> { R(5, 1), R(1, 3), R(1, 2), R(0, 10) };

        list.Sort();

        CollectionAssert.AreEqual(new[] { R(0, 10), R(1, 2), R(1, 3), R(5, 1) }, list);
    }

    #endregion

    #region Type converter

    [TestMethod]
    public void GetConverter_ReturnsRangeTypeConverter()
    {
        Assert.IsInstanceOfType<Range.RangeTypeConverter>(Converter);
        Assert.IsTrue(Converter.CanConvertFrom(typeof(string)));
        Assert.IsTrue(Converter.CanConvertTo(typeof(string)));
        Assert.IsTrue(Converter.CanConvertTo(typeof(InstanceDescriptor)));
    }

    [TestMethod]
    [DataRow("1", 1, 1)]
    [DataRow("1,3", 1, 3)]
    [DataRow("1, 3", 1, 3)]
    [DataRow(" 1 , 3 ", 1, 3)]
    [DataRow("0,10", 0, 10)]
    public void ConvertFromInvariantString(string text, int start, int length)
    {
        AssertRange(start, length, (Range)Converter.ConvertFromInvariantString(text)!, text);
    }

    public static IEnumerable<object[]> ConverterRoundTripCases()
    {
        foreach (var culture in new[] { "", "en-US", "de-DE", "fr-FR" })
        {
            yield return [culture, 1, 1];
            yield return [culture, 1, 3];
            yield return [culture, 0, 1];
            yield return [culture, 12345, 678];
        }
    }

    [TestMethod]
    [DynamicData(nameof(ConverterRoundTripCases))]
    public void ConvertToString_RoundTrips(string cultureName, int start, int length)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var range = R(start, length);

        var text = Converter.ConvertToString(null, culture, range)!;
        var result = (Range)Converter.ConvertFromString(null, culture, text)!;

        AssertRange(start, length, result, text);
    }

    [TestMethod]
    [DataRow("abc")]
    [DataRow("1,2,3")]
    [DataRow("1,x")]
    public void ConvertFrom_Invalid_ThrowsFormatException(string text)
    {
        // Assumed contract: malformed text throws FormatException (as the DateTimeRange converters do).
        Assert.Throws<FormatException>(() => Converter.ConvertFromInvariantString(text));
    }

    [TestMethod]
    public void ConvertTo_InstanceDescriptor_RecreatesValue()
    {
        var range = R(4, 7);

        var descriptor = (InstanceDescriptor)Converter.ConvertTo(range, typeof(InstanceDescriptor))!;

        AssertRange(4, 7, (Range)descriptor.Invoke()!);
    }

    #endregion

    #region DataContract

    [TestMethod]
    public void DataContract_RoundTrips()
    {
        var result = DataContractRoundTrip(R(4, 7));

        Assert.IsInstanceOfType<Range>(result);
        AssertRange(4, 7, result);
    }

    #endregion
}
