using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public partial class DateTimeOffsetRangeTests
{
    private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(DateTimeOffsetRange));

    public static IEnumerable<object[]> RoundTripCases()
    {
        foreach (var culture in new[] { "", "en-US", "de-DE", "fr-FR", "ja-JP" })
        {
            foreach (var offsetHours in new[] { 0, -5, 9 })
            {
                yield return [culture, offsetHours];
            }
        }
    }

    private static void AssertExact(DateTimeOffsetRange expected, DateTimeOffsetRange actual, string? message = null)
    {
        Assert.IsTrue(expected.Start.EqualsExact(actual.Start), $"Start: expected {expected.Start:o}, actual {actual.Start:o}. {message}");
        Assert.IsTrue(expected.End.EqualsExact(actual.End), $"End: expected {expected.End:o}, actual {actual.End:o}. {message}");
    }

    [TestMethod]
    public void GetConverter_ReturnsDateTimeOffsetRangeTypeConverter()
    {
        Assert.IsInstanceOfType<DateTimeOffsetRange.DateTimeOffsetRangeTypeConverter>(Converter);
        Assert.IsTrue(Converter.CanConvertFrom(typeof(string)));
        Assert.IsTrue(Converter.CanConvertTo(typeof(string)));
    }

    [TestMethod]
    [DynamicData(nameof(RoundTripCases))]
    public void ConvertToString_RoundTrips(string cultureName, int offsetHours)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var offset = TimeSpan.FromHours(offsetHours);
        var range = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, offset).AddTicks(1234567),
            new DateTimeOffset(2024, 3, 4, 5, 6, 7, offset));

        var text = Converter.ConvertToString(null, culture, range)!;
        var result = (DateTimeOffsetRange)Converter.ConvertFromString(null, culture, text)!;

        AssertExact(range, result, text);
    }

    [TestMethod]
    public void ConvertToInvariantString_UsesIsoAndSeparator()
    {
        var range = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.FromHours(-5)));

        Assert.AreEqual("2024-01-01T00:00:00.0000000-05:00..2024-01-05T00:00:00.0000000-05:00", Converter.ConvertToInvariantString(range));
    }

    [TestMethod]
    public void ConvertFrom_SingleDate_IsOneDay()
    {
        var result = (DateTimeOffsetRange)Converter.ConvertFromInvariantString("2024-01-01T00:00:00-05:00")!;

        AssertExact(new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.FromHours(-5))), result);
    }

    [TestMethod]
    [DataRow("", "2024-01-01T00:00:00-05:00..2024-01-05T00:00:00-05:00")]
    [DataRow("", "  2024-01-01T00:00:00-05:00 ..  2024-01-05T00:00:00-05:00  ")]
    [DataRow("", "2024-01-01 00:00:00 -05:00..2024-01-05 00:00:00 -05:00")]
    [DataRow("en-US", "1/1/2024 12:00:00 AM -05:00..1/5/2024 12:00:00 AM -05:00")]
    [DataRow("de-DE", "01.01.2024 00:00:00 -05:00..05.01.2024 00:00:00 -05:00")]
    public void ConvertFrom_StartAndEnd(string cultureName, string text)
    {
        var result = (DateTimeOffsetRange)Converter.ConvertFromString(null, CultureInfo.GetCultureInfo(cultureName), text)!;

        AssertExact(new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.FromHours(-5))), result);
    }

    [TestMethod]
    [DataRow("hu-HU")]
    [DataRow("de-DE")]
    [DataRow("en-US")]
    [DataRow("ja-JP")]
    public void ConvertFrom_CultureShortDates(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var expected = new DateTimeOffsetRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));
        var text = $"{expected.Start.Date.ToString("d", culture)}..{expected.End.Date.ToString("d", culture)}";

        var result = (DateTimeOffsetRange)Converter.ConvertFromString(null, culture, text)!;

        AssertExact(expected, result, text);
    }

    [TestMethod]
    public void ConvertFrom_MixedOffsets_PreservesEach()
    {
        var result = (DateTimeOffsetRange)Converter.ConvertFromInvariantString("2024-01-01T00:00:00Z..2024-01-01T09:00:00+09:00")!;

        Assert.AreEqual(TimeSpan.Zero, result.Start.Offset);
        Assert.AreEqual(TimeSpan.FromHours(9), result.End.Offset);
        Assert.AreEqual(TimeSpan.Zero, result.Length);
    }

    [TestMethod]
    public void ConvertFrom_NoOffset_AssumesLocal()
    {
        var result = (DateTimeOffsetRange)Converter.ConvertFromInvariantString("2024-01-01..2024-01-05")!;

        AssertExact(new DateTimeOffsetRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5)), result);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" .. ")]
    [DataRow("2024-01-01..")]
    [DataRow("not a date")]
    [DataRow("2024-01-01T00:00:00Z, 2024-01-05T00:00:00Z")]
    [DataRow("2024-01-05T00:00:00Z..2024-01-01T00:00:00Z")]
    [DataRow("2024-01-01T00:00:00Z..2024-01-02T00:00:00Z..2024-01-03T00:00:00Z")]
    public void ConvertFrom_Invalid_ThrowsFormatException(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => Converter.ConvertFromInvariantString(text));
    }

    [TestMethod]
    public void ConvertTo_InstanceDescriptor_RecreatesValue()
    {
        var range = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.FromHours(-5)),
            new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.FromHours(-5)));

        var descriptor = (InstanceDescriptor)Converter.ConvertTo(range, typeof(InstanceDescriptor))!;

        AssertExact(range, (DateTimeOffsetRange)descriptor.Invoke()!);
    }

    [TestMethod]
    public void Equals_ComparesInstants()
    {
        var a = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero));
        var b = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 9, 0, 0, TimeSpan.FromHours(9)),
            new DateTimeOffset(2024, 1, 2, 9, 0, 0, TimeSpan.FromHours(9)));

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    public void Intersect_And_Union_AcrossOffsets()
    {
        var utc = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        // 2024-01-01 06:00Z .. 18:00Z
        var tokyo = new DateTimeOffsetRange(
            new DateTimeOffset(2024, 1, 1, 15, 0, 0, TimeSpan.FromHours(9)),
            new DateTimeOffset(2024, 1, 2, 3, 0, 0, TimeSpan.FromHours(9)));

        var intersect = utc.Intersect(tokyo);
        Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 6, 0, 0, TimeSpan.Zero), intersect.Start);
        Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero), intersect.End);

        var union = utc.Union(tokyo);
        Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), union.Start);
        Assert.AreEqual(new DateTimeOffset(2024, 1, 1, 18, 0, 0, TimeSpan.Zero), union.End);
    }
}

public partial class DateTimeOffsetRangeTests
{
    private static readonly TimeSpan Tokyo = TimeSpan.FromHours(9);
    private static readonly TimeSpan NewYork = TimeSpan.FromHours(-5);

    /// <summary>Instant at 2024-01-01T00:00Z + hours, expressed in the given offset.</summary>
    private static DateTimeOffset T(double hours, TimeSpan? offset = null)
        => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(hours).ToOffset(offset ?? TimeSpan.Zero);

    private static DateTimeOffsetRange H(double startHours, double endHours, TimeSpan? offset = null)
        => new(T(startHours, offset), T(endHours, offset));

    private static void AssertHours(double startHours, double endHours, DateTimeOffsetRange? actual, string? message = null)
    {
        Assert.IsNotNull(actual, message);
        Assert.AreEqual(T(startHours), actual.Value.Start, $"Start. {message}");
        Assert.AreEqual(T(endHours), actual.Value.End, $"End. {message}");
        Assert.AreEqual(TimeSpan.FromHours(endHours - startHours), actual.Value.Length, $"Length. {message}");
    }

    private static TValue DataContractRoundTrip<TValue>(TValue value)
    {
        var serializer = new DataContractSerializer(typeof(TValue));
        using var ms = new MemoryStream();
        serializer.WriteObject(ms, value);
        ms.Position = 0;
        return (TValue)serializer.ReadObject(ms)!;
    }

    #region Construction

    [TestMethod]
    public void Ctor_SetsProperties()
    {
        var r = new DateTimeOffsetRange(T(1, Tokyo), T(5, Tokyo));

        Assert.IsTrue(T(1, Tokyo).EqualsExact(r.Start));
        Assert.IsTrue(T(5, Tokyo).EqualsExact(r.End));
        Assert.AreEqual(TimeSpan.FromHours(4), r.Length);
    }

    [TestMethod]
    public void Ctor_MixedOffsets_LengthIsByInstant()
    {
        var r = new DateTimeOffsetRange(T(1, NewYork), T(5, Tokyo));

        Assert.AreEqual(TimeSpan.FromHours(4), r.Length);
    }

    [TestMethod]
    public void Ctor_ZeroLength_Allowed()
    {
        Assert.AreEqual(TimeSpan.Zero, H(3, 3).Length);
    }

    [TestMethod]
    public void Ctor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateTimeOffsetRange(T(5), T(4)));
    }

    [TestMethod]
    public void Ctor_EndBeforeStartByInstant_ClockTimeLater_Throws()
    {
        // End's clock time (08:00+09:00) is later than start's (01:00Z) but its instant (23:00Z prev day) is earlier.
        Assert.Throws<ArgumentException>(() => new DateTimeOffsetRange(T(1), T(-1, Tokyo)));
    }

    [TestMethod]
    public void Ctor_EndAfterStartByInstant_ClockTimeEarlier_Allowed()
    {
        // Start 10:00+09:00 (= 01:00Z), End 02:00Z: clock time earlier, instant later.
        var r = new DateTimeOffsetRange(T(1, Tokyo), T(2));

        Assert.AreEqual(TimeSpan.FromHours(1), r.Length);
    }

    [TestMethod]
    public void Max_MatchesDateRangeEpoch()
    {
        // Max is [1904, 2104) UTC, the same span as DateRange.Max
        AssertExact(new DateTimeOffsetRange(
            new DateTimeOffset(1904, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2104, 1, 1, 0, 0, 0, TimeSpan.Zero)), DateTimeOffsetRange.Max);
    }

    [TestMethod]
    public void Max_EnclosesTypicalRange()
    {
        Assert.IsTrue(H(0, 100, Tokyo).IsEnclosedBy(DateTimeOffsetRange.Max));
    }

    [TestMethod]
    public void ImplicitFromUtcDateTimeRange()
    {
        DateTimeOffsetRange r = new DateTimeRange(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        Assert.IsTrue(T(0).EqualsExact(r.Start));
        Assert.IsTrue(T(24).EqualsExact(r.End));
    }

    [TestMethod]
    public void ImplicitFromLocalDateTimeRange()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local);
        var end = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Local);

        DateTimeOffsetRange r = new DateTimeRange(start, end);

        Assert.IsTrue(new DateTimeOffset(start).EqualsExact(r.Start));
        Assert.IsTrue(new DateTimeOffset(end).EqualsExact(r.End));
    }

    #endregion

    #region Contains / relations

    [TestMethod]
    [DataRow(0.5, false)]
    [DataRow(1.0, true)]
    [DataRow(3.0, true)]
    [DataRow(5.0, false)]
    [DataRow(6.0, false)]
    public void Contains_IsHalfOpen_AcrossOffsets(double hours, bool expected)
    {
        Assert.AreEqual(expected, H(1, 5).Contains(T(hours, Tokyo)));
        Assert.AreEqual(expected, H(1, 5, NewYork).Contains(T(hours)));
    }

    [TestMethod]
    [DataRow(0.0, 5.0, 3.0, 8.0, true)]
    [DataRow(3.0, 8.0, 0.0, 5.0, true)]
    [DataRow(0.0, 10.0, 3.0, 5.0, true)]
    [DataRow(3.0, 5.0, 0.0, 10.0, true)]
    [DataRow(0.0, 5.0, 5.0, 8.0, false)]
    [DataRow(5.0, 8.0, 0.0, 5.0, false)]
    [DataRow(0.0, 5.0, 6.0, 8.0, false)]
    public void IsIntersecting_AcrossOffsets(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsIntersecting(H(s2, e2, Tokyo)));
        Assert.AreEqual(expected, H(s1, e1, NewYork).IsIntersecting(H(s2, e2)));
    }

    [TestMethod]
    [DataRow(0.0, 5.0, 3.0, 8.0, true)]
    [DataRow(0.0, 5.0, 5.0, 8.0, true)]
    [DataRow(5.0, 8.0, 0.0, 5.0, true)]
    [DataRow(0.0, 5.0, 6.0, 8.0, false)]
    [DataRow(6.0, 8.0, 0.0, 5.0, false)]
    public void IsTouching_AcrossOffsets(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsTouching(H(s2, e2, Tokyo)));
        Assert.AreEqual(expected, H(s1, e1, NewYork).IsTouching(H(s2, e2)));
    }

    [TestMethod]
    [DataRow(3.0, 5.0, 0.0, 10.0, true)]
    [DataRow(0.0, 10.0, 3.0, 5.0, false)]
    [DataRow(2.0, 4.0, 2.0, 4.0, true)]
    [DataRow(0.0, 3.0, 0.0, 10.0, true)]
    [DataRow(7.0, 10.0, 0.0, 10.0, true)]
    [DataRow(0.0, 5.0, 3.0, 8.0, false)]
    [DataRow(8.0, 11.0, 0.0, 10.0, false)]
    [DataRow(10.0, 12.0, 0.0, 10.0, false)]
    public void IsEnclosedBy_AcrossOffsets(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsEnclosedBy(H(s2, e2, Tokyo)));
        Assert.AreEqual(expected, H(s1, e1, NewYork).IsEnclosedBy(H(s2, e2)));
    }

    #endregion

    #region Default arguments

    public static IEnumerable<object[]> BinaryOperations()
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

    private static void InvokeOp(string op, DateTimeOffsetRange a, DateTimeOffsetRange b)
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
    [DynamicData(nameof(BinaryOperations))]
    public void BinaryOperation_DefaultArgument_ThrowsArgumentNullException(string op)
    {
        Assert.Throws<ArgumentNullException>(() => InvokeOp(op, H(0, 5), default));
    }

    #endregion

    #region Union / Intersect / ExpandToInclude

    [TestMethod]
    public void Union_Adjacent_AcrossOffsets()
    {
        AssertHours(0, 8, H(0, 5).Union(H(5, 8, Tokyo)));
        AssertHours(0, 8, H(5, 8, Tokyo).Union(H(0, 5)));
    }

    [TestMethod]
    public void Union_Enclosed()
    {
        AssertHours(0, 10, H(0, 10).Union(H(3, 5, NewYork)));
        AssertHours(0, 10, H(3, 5, NewYork).Union(H(0, 10)));
    }

    [TestMethod]
    public void Union_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Union(H(6, 8, Tokyo)));
    }

    [TestMethod]
    public void Intersect_Enclosed()
    {
        AssertHours(3, 5, H(0, 10).Intersect(H(3, 5, NewYork)));
        AssertHours(3, 5, H(3, 5, NewYork).Intersect(H(0, 10)));
    }

    [TestMethod]
    public void Intersect_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Intersect(H(5, 8, Tokyo)));
    }

    [TestMethod]
    public void ExpandToInclude_Disjoint()
    {
        AssertHours(0, 10, H(0, 2).ExpandToInclude(H(8, 10, Tokyo)));
        AssertHours(0, 10, H(8, 10, Tokyo).ExpandToInclude(H(0, 2)));
    }

    #endregion

    #region Subtract

    [TestMethod]
    public void Subtract_Middle_Splits()
    {
        var left = H(0, 10).Subtract(H(3, 5, Tokyo), out var right);

        AssertHours(0, 3, left);
        AssertHours(5, 10, right);
    }

    [TestMethod]
    public void Subtract_LeftSide()
    {
        var result = H(3, 10).Subtract(H(0, 5, Tokyo), out var newRange);

        AssertHours(5, 10, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_RightSide()
    {
        var result = H(0, 10).Subtract(H(5, 15, Tokyo), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_Enclosing_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(3, 5).Subtract(H(0, 10, Tokyo), out _), "strictly enclosing");
        Assert.Throws<ArgumentException>(() => H(3, 5).Subtract(H(3, 5, Tokyo), out _), "identical instants");
    }

    [TestMethod]
    public void Subtract_DisjointOrAdjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Subtract(H(6, 8), out _), "disjoint");
        Assert.Throws<ArgumentException>(() => H(0, 5).Subtract(H(5, 8, Tokyo), out _), "adjacent");
    }

    [TestMethod]
    public void SubtractEnumerable_Middle()
    {
        var result = H(0, 10).Subtract(H(3, 5)).ToList();

        Assert.HasCount(2, result);
        AssertHours(0, 3, result[0]);
        AssertHours(5, 10, result[1]);
    }

    [TestMethod]
    public void SubtractEnumerable_OneSide()
    {
        var result = H(0, 10).Subtract(H(5, 15)).ToList();

        Assert.HasCount(1, result);
        AssertHours(0, 5, result[0]);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Disjoint_ReturnsThis()
    {
        var result = H(0, 5).SubtractNonIntersecting(H(5, 8, Tokyo), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Enclosed_ReturnsNull()
    {
        var result = H(3, 5).SubtractNonIntersecting(H(3, 5, Tokyo), out var newRange);

        Assert.IsNull(result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Middle_Splits()
    {
        var left = H(0, 10).SubtractNonIntersecting(H(3, 5), out var right);

        AssertHours(0, 3, left);
        AssertHours(5, 10, right);
    }

    #endregion

    #region Shift / Inflate / With / BoundTo

    [TestMethod]
    public void Shift_MovesBothEnds_PreservesOffsets()
    {
        var result = H(1, 5, Tokyo).Shift(TimeSpan.FromHours(2));

        Assert.IsTrue(T(3, Tokyo).EqualsExact(result.Start));
        Assert.IsTrue(T(7, Tokyo).EqualsExact(result.End));
    }

    [TestMethod]
    public void Inflate_Pads()
    {
        AssertHours(0, 8, H(1, 5).Inflate(TimeSpan.FromHours(1), TimeSpan.FromHours(3)));
        AssertHours(2, 4, H(1, 5).Inflate(TimeSpan.FromHours(-1), TimeSpan.FromHours(-1)));
    }

    [TestMethod]
    public void Inflate_ShrinkPastZero_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(1, 5).Inflate(TimeSpan.FromHours(-3), TimeSpan.FromHours(-3)));
    }

    [TestMethod]
    public void With_Start_End()
    {
        AssertHours(0, 5, H(1, 5).With(start: T(0, Tokyo)));
        AssertHours(1, 9, H(1, 5).With(end: T(9, NewYork)));
        AssertHours(1, 5, H(1, 5).With());
    }

    [TestMethod]
    public void With_StartAfterEnd_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(1, 5).With(start: T(6)));
    }

    [TestMethod]
    public void BoundTo()
    {
        AssertHours(3, 10, H(0, 10).BoundTo(T(3, Tokyo)), "start only");
        AssertHours(0, 6, H(0, 10).BoundTo(endDate: T(6, NewYork)), "end only");
        AssertHours(3, 6, H(0, 10).BoundTo(T(3, Tokyo), T(6, NewYork)), "both");
        AssertHours(3, 6, H(3, 6).BoundTo(T(0), T(10)), "wider");
        AssertHours(3, 6, H(3, 6).BoundTo(), "none");
    }

    #endregion

    #region Equality / comparison

    [TestMethod]
    public void EqualityOperators_ComparesInstants()
    {
        var a = H(1, 5);
        var b = H(1, 5, Tokyo);
        var c = H(1, 6, Tokyo);

        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsFalse(a == c);
        Assert.IsTrue(a != c);
        Assert.IsFalse(a.Equals(c));
    }

    [TestMethod]
    public void Equals_NullOrOtherType_False()
    {
        Assert.IsFalse(H(1, 5).Equals((object)null!));
        Assert.IsFalse(H(1, 5).Equals("x"));
    }

    [TestMethod]
    public void CompareTo_ByInstantThenLength()
    {
        // a starts 01:00Z, b starts 00:00Z expressed as 09:00+09:00 (later clock time, earlier instant).
        var a = H(1, 2);
        var b = H(0, 5, Tokyo);

        Assert.IsGreaterThan(0, a.CompareTo(b));
        Assert.IsLessThan(0, b.CompareTo(a));
        Assert.IsLessThan(0, H(1, 2, Tokyo).CompareTo(H(1, 3)));
        Assert.AreEqual(0, H(1, 3, Tokyo).CompareTo(H(1, 3, NewYork)));
    }

    [TestMethod]
    public void CompareToObject()
    {
        Assert.IsGreaterThan(0, H(1, 2).CompareTo((object)H(0, 5, Tokyo)));
        Assert.AreEqual(0, H(1, 3, Tokyo).CompareTo((object)H(1, 3)));
        Assert.IsGreaterThan(0, H(1, 3).CompareTo((object?)null));
        _ = H(1, 3).CompareTo("hello");
    }

    [TestMethod]
    public void ComparisonOperators()
    {
        var a = H(0, 5, Tokyo);
        var b = H(1, 2);
        var a2 = H(0, 5, NewYork);

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

    #endregion

    #region DataContract

    [TestMethod]
    [DataRow(0)]
    [DataRow(9)]
    [DataRow(-5)]
    public void DataContract_RoundTrips(int offsetHours)
    {
        var range = H(1.5, 30.25, TimeSpan.FromHours(offsetHours));

        var result = DataContractRoundTrip(range);

        AssertExact(range, result);
    }

    #endregion
}
