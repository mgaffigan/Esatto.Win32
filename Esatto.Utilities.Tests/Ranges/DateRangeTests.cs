using System.Runtime.Serialization;
using Range = Esatto.Utilities.Range;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public class DateRangeTests
{
    private static DateTime D(int month, int day, int year = 2024) => new(year, month, day);

    // Jan 1 .. Jan 10 (exclusive), 9 days
    private static DateRange Jan(int startDay, int endDayExclusive) => new(D(1, startDay), D(1, endDayExclusive));

    private static void AssertDates(DateTime start, DateTime endExclusive, DateRange? actual, string? message = null)
    {
        Assert.IsNotNull(actual, message);
        Assert.AreEqual(start, actual.StartDate, $"StartDate. {message}");
        Assert.AreEqual(endExclusive, actual.EndDate, $"EndDate. {message}");
        Assert.AreEqual((int)(endExclusive - start).TotalDays, actual.Length, $"Length. {message}");
        Assert.AreEqual(endExclusive.AddDays(-1), actual.LastDayOfRange, $"LastDayOfRange. {message}");
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
    public void Ctor_StartAndDays()
    {
        var r = new DateRange(D(1, 1), 3);

        Assert.AreEqual(D(1, 1), r.StartDate);
        Assert.AreEqual(D(1, 4), r.EndDate);
        Assert.AreEqual(D(1, 3), r.LastDayOfRange);
        Assert.AreEqual(3, r.Length);
        Assert.AreEqual(r.Start + 3, r.End);
    }

    [TestMethod]
    public void Ctor_StartAndEnd()
    {
        var r = new DateRange(D(1, 1), D(1, 4));

        Assert.AreEqual(D(1, 1), r.StartDate);
        Assert.AreEqual(D(1, 4), r.EndDate);
        Assert.AreEqual(D(1, 3), r.LastDayOfRange);
        Assert.AreEqual(3, r.Length);
    }

    [TestMethod]
    public void Ctor_SingleDay()
    {
        var r = new DateRange(D(1, 1), 1);

        Assert.AreEqual(D(1, 1), r.StartDate);
        Assert.AreEqual(D(1, 2), r.EndDate);
        Assert.AreEqual(D(1, 1), r.LastDayOfRange);
        Assert.AreEqual(1, r.Length);
    }

    [TestMethod]
    public void Ctor_BothForms_AreEqual()
    {
        var a = new DateRange(D(1, 1), 3);
        var b = new DateRange(D(1, 1), D(1, 4));

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Ctor_AcrossLeapDayAndYear()
    {
        Assert.AreEqual(2, new DateRange(D(2, 28), D(3, 1)).Length);
        Assert.AreEqual(1, new DateRange(D(2, 28, 2023), D(3, 1, 2023)).Length);
        Assert.AreEqual(366, new DateRange(D(1, 1, 2024), D(1, 1, 2025)).Length);
        AssertDates(D(12, 30, 2023), D(1, 2, 2024), new DateRange(D(12, 30, 2023), 3));
    }

    [TestMethod]
    public void Start_IsConsecutiveIntegerPerDay()
    {
        var a = new DateRange(D(1, 31), 1);
        var b = new DateRange(D(2, 1), 1);

        Assert.AreEqual(a.Start + 1, b.Start);
        Assert.IsTrue(a.IsTouching(b));
        Assert.IsFalse(a.IsIntersecting(b));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_NonPositiveDays_Throws(int days)
    {
        Assert.Throws<ArgumentException>(() => new DateRange(D(1, 1), days));
    }

    [TestMethod]
    public void Ctor_EndEqualsStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(D(1, 1), D(1, 1)));
    }

    [TestMethod]
    public void Ctor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(D(1, 5), D(1, 1)));
    }

    [TestMethod]
    public void Max_EnclosesTypicalRanges()
    {
        // Assumed contract: Max is the widest representable DateRange.
        Assert.IsTrue(new DateRange(D(1, 1, 2000), D(1, 1, 2100)).IsEnclosedBy(DateRange.Max));
        Assert.IsTrue(Jan(1, 10).IsEnclosedBy(DateRange.Max));
    }

    #endregion

    #region Contains / AsEnumerable / conversions

    [TestMethod]
    [DataRow(12, 31, 2023, false)]
    [DataRow(1, 1, 2024, true)]
    [DataRow(1, 2, 2024, true)]
    [DataRow(1, 3, 2024, true)]
    [DataRow(1, 4, 2024, false)]
    public void Contains_Date(int month, int day, int year, bool expected)
    {
        Assert.AreEqual(expected, new DateRange(D(1, 1), 3).Contains(D(month, day, year)));
    }

    [TestMethod]
    public void Contains_TimeWithinLastDay_True()
    {
        // Assumed contract: Contains tests whether the date's day is in the range (time of day ignored).
        Assert.IsTrue(new DateRange(D(1, 1), 3).Contains(D(1, 3).AddHours(23)));
        Assert.IsTrue(new DateRange(D(1, 1), 3).Contains(D(1, 1).AddHours(12)));
        Assert.IsFalse(new DateRange(D(1, 1), 3).Contains(D(12, 31, 2023).AddHours(23)));
    }

    [TestMethod]
    public void AsEnumerable_YieldsEachDay()
    {
        CollectionAssert.AreEqual(new[] { D(1, 1), D(1, 2), D(1, 3) }, new DateRange(D(1, 1), 3).AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_SingleDay()
    {
        CollectionAssert.AreEqual(new[] { D(2, 29) }, new DateRange(D(2, 29), 1).AsEnumerable().ToList());
    }

    [TestMethod]
    public void ImplicitToDateTimeRange()
    {
        DateTimeRange dtr = new DateRange(D(1, 1), 3);

        Assert.AreEqual(D(1, 1), dtr.Start);
        Assert.AreEqual(D(1, 4), dtr.End);
        Assert.AreEqual(TimeSpan.FromDays(3), dtr.Length);
    }

    #endregion

    #region Relations

    [TestMethod]
    public void IsIntersecting_IsTouching_IsEnclosedBy()
    {
        var a = Jan(1, 5);

        Assert.IsTrue(a.IsIntersecting(Jan(4, 8)));
        Assert.IsFalse(a.IsIntersecting(Jan(5, 8)));
        Assert.IsTrue(a.IsTouching(Jan(5, 8)));
        Assert.IsFalse(a.IsTouching(Jan(6, 8)));
        Assert.IsTrue(Jan(2, 3).IsEnclosedBy(a));
        Assert.IsFalse(a.IsEnclosedBy(Jan(2, 3)));
    }

    #endregion

    #region Operations return DateRange

    [TestMethod]
    public void Shift_Days()
    {
        AssertDates(D(1, 2), D(1, 5), Jan(1, 4).Shift(1));
        AssertDates(D(12, 30, 2023), D(1, 2), Jan(1, 4).Shift(-2));
    }

    [TestMethod]
    public void Inflate_Days()
    {
        AssertDates(D(12, 31, 2023), D(1, 6), Jan(1, 4).Inflate(1, 2));
        AssertDates(D(1, 2), D(1, 4), Jan(1, 4).Inflate(padLeft: -1));
        AssertDates(D(1, 1), D(1, 3), Jan(1, 4).Inflate(padRight: -1));
    }

    [TestMethod]
    public void Inflate_ToEmpty_Throws()
    {
        Assert.Throws<ArgumentException>(() => Jan(1, 4).Inflate(-2, -1));
    }

    [TestMethod]
    public void With_StartDelta_ShiftsStartKeepsLength()
    {
        AssertDates(D(1, 2), D(1, 5), Jan(1, 4).With(startDelta: 1));
    }

    [TestMethod]
    public void With_NegativeStartDelta()
    {
        AssertDates(D(12, 31, 2023), D(1, 3), Jan(1, 4).With(startDelta: -1));
    }

    [TestMethod]
    public void With_NewLength_KeepsStart()
    {
        AssertDates(D(1, 1), D(1, 6), Jan(1, 4).With(newLength: 5));
    }

    [TestMethod]
    public void With_Both()
    {
        AssertDates(D(1, 3), D(1, 4), Jan(1, 4).With(2, 1));
    }

    [TestMethod]
    public void With_Nothing_ReturnsEqual()
    {
        AssertDates(D(1, 1), D(1, 4), Jan(1, 4).With());
    }

    [TestMethod]
    public void BoundTo_StartOnly()
    {
        AssertDates(D(1, 3), D(1, 10), Jan(1, 10).BoundTo(D(1, 3)));
    }

    [TestMethod]
    public void BoundTo_EndOnly()
    {
        AssertDates(D(1, 1), D(1, 5), Jan(1, 10).BoundTo(endDate: D(1, 5)));
    }

    [TestMethod]
    public void BoundTo_Both()
    {
        AssertDates(D(1, 3), D(1, 5), Jan(1, 10).BoundTo(D(1, 3), D(1, 5)));
    }

    [TestMethod]
    public void BoundTo_Wider_ReturnsEqual()
    {
        AssertDates(D(1, 3), D(1, 5), Jan(3, 5).BoundTo(D(1, 1), D(1, 10)));
    }

    [TestMethod]
    public void BoundTo_Null_ReturnsEqual()
    {
        AssertDates(D(1, 3), D(1, 5), Jan(3, 5).BoundTo());
    }

    [TestMethod]
    public void Union_Adjacent()
    {
        AssertDates(D(1, 1), D(1, 8), Jan(1, 5).Union(Jan(5, 8)));
        AssertDates(D(1, 1), D(1, 8), Jan(5, 8).Union(Jan(1, 5)));
    }

    [TestMethod]
    public void Union_Overlapping()
    {
        AssertDates(D(1, 1), D(1, 8), Jan(1, 5).Union(Jan(3, 8)));
    }

    [TestMethod]
    public void Union_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => Jan(1, 5).Union(Jan(6, 8)));
    }

    [TestMethod]
    public void Intersect_Overlapping()
    {
        AssertDates(D(1, 3), D(1, 5), Jan(1, 5).Intersect(Jan(3, 8)));
        AssertDates(D(1, 3), D(1, 5), Jan(3, 8).Intersect(Jan(1, 5)));
    }

    [TestMethod]
    public void Intersect_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => Jan(1, 5).Intersect(Jan(5, 8)));
    }

    [TestMethod]
    public void ExpandToInclude_Disjoint()
    {
        AssertDates(D(1, 1), D(1, 10), Jan(1, 3).ExpandToInclude(Jan(8, 10)));
        AssertDates(D(1, 1), D(1, 10), Jan(8, 10).ExpandToInclude(Jan(1, 3)));
    }

    [TestMethod]
    public void Subtract_Middle_Splits()
    {
        var left = Jan(1, 10).Subtract(Jan(3, 5), out var right);

        AssertDates(D(1, 1), D(1, 3), left);
        AssertDates(D(1, 5), D(1, 10), right);
    }

    [TestMethod]
    public void Subtract_LeftSide()
    {
        var result = Jan(3, 10).Subtract(Jan(1, 5), out var newRange);

        AssertDates(D(1, 5), D(1, 10), result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_RightSide()
    {
        var result = Jan(1, 10).Subtract(Jan(5, 15), out var newRange);

        AssertDates(D(1, 1), D(1, 5), result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_Enclosing_Throws()
    {
        Assert.Throws<ArgumentException>(() => Jan(3, 5).Subtract(Jan(1, 10), out _));
        Assert.Throws<ArgumentException>(() => Jan(3, 5).Subtract(Jan(3, 5), out _));
    }

    [TestMethod]
    public void Subtract_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => Jan(1, 3).Subtract(Jan(5, 8), out _));
    }

    [TestMethod]
    public void SubtractEnumerable_Middle()
    {
        var result = Jan(1, 10).Subtract(Jan(3, 5)).ToList();

        Assert.HasCount(2, result);
        AssertDates(D(1, 1), D(1, 3), result[0]);
        AssertDates(D(1, 5), D(1, 10), result[1]);
    }

    [TestMethod]
    public void SubtractEnumerable_OneSide()
    {
        var result = Jan(1, 10).Subtract(Jan(5, 15)).ToList();

        Assert.HasCount(1, result);
        AssertDates(D(1, 1), D(1, 5), result[0]);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Disjoint_ReturnsThis()
    {
        var result = Jan(1, 3).SubtractNonIntersecting(Jan(5, 8), out var newRange);

        AssertDates(D(1, 1), D(1, 3), result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Enclosed_ReturnsNull()
    {
        var result = Jan(3, 5).SubtractNonIntersecting(Jan(1, 10), out var newRange);

        Assert.IsNull(result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Middle_Splits()
    {
        var left = Jan(1, 10).SubtractNonIntersecting(Jan(3, 5), out var right);

        AssertDates(D(1, 1), D(1, 3), left);
        AssertDates(D(1, 5), D(1, 10), right);
    }

    [TestMethod]
    public void SubtractNonIntersectingEnumerable_Disjoint_YieldsThis()
    {
        var result = Jan(1, 3).SubtractNonIntersecting(Jan(5, 8)).ToList();

        Assert.HasCount(1, result);
        AssertDates(D(1, 1), D(1, 3), result[0]);
    }

    [TestMethod]
    public void SubtractNonIntersectingEnumerable_Adjacent_YieldsThis()
    {
        var result = Jan(1, 3).SubtractNonIntersecting(Jan(3, 8)).ToList();

        Assert.HasCount(1, result);
        AssertDates(D(1, 1), D(1, 3), result[0]);
    }

    [TestMethod]
    public void SubtractNonIntersectingEnumerable_Enclosed_YieldsNothing()
    {
        var result = Jan(3, 5).SubtractNonIntersecting(Jan(1, 10)).ToList();

        Assert.HasCount(0, result);
    }

    [TestMethod]
    public void SubtractNonIntersectingEnumerable_Middle_YieldsBothInOrder()
    {
        var result = Jan(1, 10).SubtractNonIntersecting(Jan(3, 5)).ToList();

        Assert.HasCount(2, result);
        AssertDates(D(1, 1), D(1, 3), result[0]);
        AssertDates(D(1, 5), D(1, 10), result[1]);
    }

    [TestMethod]
    public void SubtractNonIntersectingEnumerable_OneSide()
    {
        var result = Jan(3, 10).SubtractNonIntersecting(Jan(1, 5)).ToList();

        Assert.HasCount(1, result);
        AssertDates(D(1, 5), D(1, 10), result[0]);
    }

    [TestMethod]
    public void BaseRangeOperations_ReturnDateRange()
    {
        Range r = Jan(1, 5);
        Range other = Jan(3, 8);

        Assert.IsInstanceOfType<DateRange>(r.Shift(1), "Shift");
        Assert.IsInstanceOfType<DateRange>(r.Inflate(1, 1), "Inflate");
        Assert.IsInstanceOfType<DateRange>(r.With(start: r.Start + 1), "With");
        Assert.IsInstanceOfType<DateRange>(r.Union(other), "Union");
        Assert.IsInstanceOfType<DateRange>(r.Intersect(other), "Intersect");
        Assert.IsInstanceOfType<DateRange>(r.ExpandToInclude(other), "ExpandToInclude");
        Assert.IsInstanceOfType<DateRange>(r.Subtract(other, out _), "Subtract");
        Assert.IsInstanceOfType<DateRange>(r.SubtractNonIntersecting(Jan(10, 12), out _), "SubtractNonIntersecting");

        var pieces = Jan(1, 10).AsRange().Subtract(Jan(3, 5)).ToList();
        Assert.IsTrue(pieces.All(p => p is DateRange), "Subtract(IEnumerable)");
    }

    [TestMethod]
    public void BaseRangeShift_HasCorrectDates()
    {
        Range r = Jan(1, 5);

        var shifted = (DateRange)r.Shift(1);

        AssertDates(D(1, 2), D(1, 6), shifted);
    }

    [TestMethod]
    public void BaseRangeWith_HasCorrectDates()
    {
        Range r = Jan(1, 5);

        var result = (DateRange)r.With(start: r.Start + 2, length: 1);

        AssertDates(D(1, 3), D(1, 4), result);
    }

    #endregion

    #region Null arguments

    [TestMethod]
    public void NullArguments_ThrowArgumentNullException()
    {
        var a = Jan(1, 5);
        DateRange n = null!;

        Assert.Throws<ArgumentNullException>(() => a.Union(n), "Union");
        Assert.Throws<ArgumentNullException>(() => a.Intersect(n), "Intersect");
        Assert.Throws<ArgumentNullException>(() => a.ExpandToInclude(n), "ExpandToInclude");
        Assert.Throws<ArgumentNullException>(() => a.Subtract(n, out _), "Subtract(out)");
        Assert.Throws<ArgumentNullException>(() => a.Subtract(n).ToList(), "Subtract(IEnumerable)");
        Assert.Throws<ArgumentNullException>(() => a.SubtractNonIntersecting(n, out _), "SubtractNonIntersecting(out)");
        Assert.Throws<ArgumentNullException>(() => a.SubtractNonIntersecting(n).ToList(), "SubtractNonIntersecting(IEnumerable)");
        Assert.Throws<ArgumentNullException>(() => a.IsIntersecting(n), "IsIntersecting");
        Assert.Throws<ArgumentNullException>(() => a.IsTouching(n), "IsTouching");
        Assert.Throws<ArgumentNullException>(() => a.IsEnclosedBy(n), "IsEnclosedBy");
    }

    #endregion

    #region Comparison

    [TestMethod]
    public void CompareTo_OrdersByStartThenLength()
    {
        Assert.IsLessThan(0, Jan(1, 10).CompareTo(Jan(2, 3)));
        Assert.IsGreaterThan(0, Jan(2, 3).CompareTo(Jan(1, 10)));
        Assert.IsLessThan(0, Jan(1, 3).CompareTo(Jan(1, 4)));
        Assert.AreEqual(0, Jan(1, 3).CompareTo(Jan(1, 3)));
    }

    [TestMethod]
    public void CompareTo_Null_IsGreater()
    {
        Assert.IsGreaterThan(0, Jan(1, 3).CompareTo((DateRange?)null));
    }

    [TestMethod]
    public void Equals_DifferentDates_False()
    {
        Assert.AreNotEqual(Jan(1, 3), Jan(1, 4));
        Assert.AreNotEqual(Jan(1, 3), Jan(2, 4));
        Assert.IsTrue(Jan(1, 3) != Jan(2, 4));
    }

    [TestMethod]
    public void ToString_DoesNotThrow()
    {
        Assert.IsFalse(string.IsNullOrEmpty(Jan(1, 3).ToString()));
    }

    #endregion

    #region DataContract

    [TestMethod]
    public void DataContract_RoundTrips()
    {
        var result = DataContractRoundTrip(Jan(1, 5));

        AssertDates(D(1, 1), D(1, 5), result);
    }

    [TestMethod]
    public void DataContract_AsRange_DeserializesAsDateRange()
    {
        Range value = Jan(1, 5);

        var result = DataContractRoundTrip(value);

        Assert.IsInstanceOfType<DateRange>(result);
        AssertDates(D(1, 1), D(1, 5), (DateRange)result);
    }

    #endregion
}

internal static class DateRangeTestExtensions
{
    public static Range AsRange(this DateRange r) => r;
}
