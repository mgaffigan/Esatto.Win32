using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Runtime.Serialization;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public partial class DateTimeRangeTests
{
    private static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(DateTimeRange));

    public static IEnumerable<object[]> RoundTripCases()
    {
        foreach (var culture in new[] { "", "en-US", "de-DE", "fr-FR", "ja-JP" })
        {
            foreach (var kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
            {
                yield return [culture, kind];
            }
        }
    }

    [TestMethod]
    public void GetConverter_ReturnsDateTimeRangeTypeConverter()
    {
        Assert.IsInstanceOfType<DateTimeRange.DateTimeRangeTypeConverter>(Converter);
        Assert.IsTrue(Converter.CanConvertFrom(typeof(string)));
        Assert.IsTrue(Converter.CanConvertTo(typeof(string)));
    }

    [TestMethod]
    [DynamicData(nameof(RoundTripCases))]
    public void ConvertToString_RoundTrips(string cultureName, DateTimeKind kind)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var range = new DateTimeRange(
            new DateTime(2024, 1, 2, 3, 4, 5, kind).AddTicks(1234567),
            new DateTime(2024, 3, 4, 5, 6, 7, kind));

        var text = Converter.ConvertToString(null, culture, range)!;
        var result = (DateTimeRange)Converter.ConvertFromString(null, culture, text)!;

        Assert.AreEqual(range, result, text);
        Assert.AreEqual(kind, result.Kind, text);
    }

    [TestMethod]
    public void ConvertToInvariantString_RoundTrips()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));

        var text = Converter.ConvertToInvariantString(range)!;
        var result = (DateTimeRange)Converter.ConvertFromInvariantString(text)!;

        Assert.AreEqual(range, result, text);
    }

    [TestMethod]
    public void ConvertFrom_SingleDate_IsOneDay()
    {
        var result = (DateTimeRange)Converter.ConvertFromInvariantString("2024-01-01")!;

        Assert.AreEqual(new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2)), result);
    }

    [TestMethod]
    public void ConvertToInvariantString_UsesIsoAndSeparator()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc));

        Assert.AreEqual("2024-01-01T00:00:00.0000000Z..2024-01-05T00:00:00.0000000Z", Converter.ConvertToInvariantString(range));
    }

    [TestMethod]
    [DataRow("", "2024-01-01..2024-01-05")]
    [DataRow("", "  2024-01-01 ..  2024-01-05  ")]
    [DataRow("", "2024-01-01 00:00:00..2024-01-05 00:00:00")]
    [DataRow("", "Jan 1, 2024..Jan 5, 2024")]
    [DataRow("en-US", "1/1/2024 12:00:00 AM..1/5/2024 12:00:00 AM")]
    [DataRow("de-DE", "01.01.2024..05.01.2024")]
    public void ConvertFrom_StartAndEnd(string cultureName, string text)
    {
        var result = (DateTimeRange)Converter.ConvertFromString(null, CultureInfo.GetCultureInfo(cultureName), text)!;

        Assert.AreEqual(new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5)), result);
    }

    [TestMethod]
    [DataRow("hu-HU")]
    [DataRow("de-DE")]
    [DataRow("en-US")]
    [DataRow("ja-JP")]
    public void ConvertFrom_CultureShortDates(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var expected = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));
        var text = $"{expected.Start.ToString("d", culture)}..{expected.End.ToString("d", culture)}";

        var result = (DateTimeRange)Converter.ConvertFromString(null, culture, text)!;

        Assert.AreEqual(expected, result, text);
    }

    [TestMethod]
    public void ConvertFrom_Utc_PreservesKind()
    {
        var result = (DateTimeRange)Converter.ConvertFromInvariantString("2024-01-01T00:00:00Z..2024-01-05T00:00:00Z")!;

        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        Assert.AreEqual(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Start);
        Assert.AreEqual(new DateTime(2024, 1, 5, 0, 0, 0, DateTimeKind.Utc), result.End);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" .. ")]
    [DataRow("2024-01-01..")]
    [DataRow("not a date")]
    [DataRow("2024-01-01, 2024-01-05")]
    [DataRow("2024-01-05..2024-01-01")]
    [DataRow("2024-01-01..2024-01-02..2024-01-03")]
    [DataRow("2024-01-01T00:00:00Z..2024-01-05T00:00:00")]
    public void ConvertFrom_Invalid_ThrowsFormatException(string text)
    {
        Assert.ThrowsExactly<FormatException>(() => Converter.ConvertFromInvariantString(text));
    }

    [TestMethod]
    public void ConvertTo_InstanceDescriptor_RecreatesValue()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 5));

        var descriptor = (InstanceDescriptor)Converter.ConvertTo(range, typeof(InstanceDescriptor))!;

        Assert.AreEqual(range, descriptor.Invoke());
    }

    [TestMethod]
    public void AsEnumerable_MultipleTimesOfDay_UsesEachTime()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        var result = range.AsEnumerable([TimeSpan.FromHours(8), TimeSpan.FromHours(20)]).ToList();

        CollectionAssert.AreEqual(new[]
        {
            new DateTime(2024, 1, 1, 8, 0, 0),
            new DateTime(2024, 1, 1, 20, 0, 0),
            new DateTime(2024, 1, 2, 8, 0, 0),
            new DateTime(2024, 1, 2, 20, 0, 0),
        }, result);
    }
}

public partial class DateTimeRangeTests
{
    private static readonly DateTime Base = new(2024, 1, 1);

    private static DateTime T(double hours, DateTimeKind kind = DateTimeKind.Unspecified)
        => DateTime.SpecifyKind(Base, kind).AddHours(hours);

    private static DateTimeRange H(double startHours, double endHours, DateTimeKind kind = DateTimeKind.Unspecified)
        => new(T(startHours, kind), T(endHours, kind));

    private static void AssertRange(DateTime start, DateTime end, DateTimeRange? actual, string? message = null)
    {
        Assert.IsNotNull(actual, message);
        Assert.AreEqual(start, actual.Value.Start, $"Start. {message}");
        Assert.AreEqual(end, actual.Value.End, $"End. {message}");
        Assert.AreEqual(end - start, actual.Value.Length, $"Length. {message}");
    }

    private static void AssertHours(double startHours, double endHours, DateTimeRange? actual, string? message = null)
        => AssertRange(T(startHours), T(endHours), actual, message);

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
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void Ctor_SetsProperties(DateTimeKind kind)
    {
        var r = new DateTimeRange(T(1, kind), T(5, kind));

        Assert.AreEqual(T(1, kind), r.Start);
        Assert.AreEqual(T(5, kind), r.End);
        Assert.AreEqual(TimeSpan.FromHours(4), r.Length);
        Assert.AreEqual(kind, r.Kind);
        Assert.AreEqual(kind, r.Start.Kind);
        Assert.AreEqual(kind, r.End.Kind);
    }

    [TestMethod]
    public void Ctor_ZeroLength_Allowed()
    {
        var r = H(3, 3);

        Assert.AreEqual(TimeSpan.Zero, r.Length);
        Assert.AreEqual(T(3), r.Start);
        Assert.AreEqual(T(3), r.End);
    }

    [TestMethod]
    public void Ctor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateTimeRange(T(5), T(4)));
    }

    [TestMethod]
    [DataRow(DateTimeKind.Utc, DateTimeKind.Local)]
    [DataRow(DateTimeKind.Local, DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Utc, DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Unspecified, DateTimeKind.Local)]
    public void Ctor_MismatchedKind_Throws(DateTimeKind startKind, DateTimeKind endKind)
    {
        Assert.Throws<ArgumentException>(() => new DateTimeRange(T(1, startKind), T(5, endKind)));
    }

    [TestMethod]
    public void Max_MatchesDateRangeEpoch()
    {
        // Max is [1904, 2104), the same span as DateRange.Max
        Assert.AreEqual(new DateTime(1904, 1, 1), DateTimeRange.Max.Start);
        Assert.AreEqual(new DateTime(2104, 1, 1), DateTimeRange.Max.End);
        Assert.AreEqual((DateTimeRange)DateRange.Max, DateTimeRange.Max);
    }

    [TestMethod]
    public void Max_EnclosesTypicalRange()
    {
        Assert.IsTrue(H(0, 100).IsEnclosedBy(DateTimeRange.Max));
    }

    #endregion

    #region Contains / relations

    [TestMethod]
    [DataRow(0.5, false)]
    [DataRow(1.0, true)]
    [DataRow(3.0, true)]
    [DataRow(4.999, true)]
    [DataRow(5.0, false)]
    [DataRow(6.0, false)]
    public void Contains_IsHalfOpen(double hours, bool expected)
    {
        Assert.AreEqual(expected, H(1, 5).Contains(T(hours)));
    }

    [TestMethod]
    public void Contains_JustBeforeEnd_True()
    {
        Assert.IsTrue(H(1, 5).Contains(T(5).AddTicks(-1)));
    }

    [TestMethod]
    public void Contains_ZeroLength_ContainsNothing()
    {
        Assert.IsFalse(H(3, 3).Contains(T(3)));
    }

    [TestMethod]
    [DataRow(0.0, 5.0, 3.0, 8.0, true)]
    [DataRow(3.0, 8.0, 0.0, 5.0, true)]
    [DataRow(0.0, 10.0, 3.0, 5.0, true)]
    [DataRow(3.0, 5.0, 0.0, 10.0, true)]
    [DataRow(2.0, 4.0, 2.0, 4.0, true)]
    [DataRow(0.0, 5.0, 5.0, 8.0, false)]
    [DataRow(5.0, 8.0, 0.0, 5.0, false)]
    [DataRow(0.0, 5.0, 6.0, 8.0, false)]
    [DataRow(6.0, 8.0, 0.0, 5.0, false)]
    public void IsIntersecting(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsIntersecting(H(s2, e2)));
    }

    [TestMethod]
    [DataRow(0.0, 5.0, 3.0, 8.0, true)]
    [DataRow(0.0, 10.0, 3.0, 5.0, true)]
    [DataRow(3.0, 5.0, 0.0, 10.0, true)]
    [DataRow(0.0, 5.0, 5.0, 8.0, true)]
    [DataRow(5.0, 8.0, 0.0, 5.0, true)]
    [DataRow(0.0, 5.0, 6.0, 8.0, false)]
    [DataRow(6.0, 8.0, 0.0, 5.0, false)]
    public void IsTouching(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsTouching(H(s2, e2)));
    }

    [TestMethod]
    [DataRow(3.0, 5.0, 0.0, 10.0, true)]
    [DataRow(0.0, 10.0, 3.0, 5.0, false)]
    [DataRow(2.0, 4.0, 2.0, 4.0, true)]
    [DataRow(0.0, 3.0, 0.0, 10.0, true)]
    [DataRow(7.0, 10.0, 0.0, 10.0, true)]
    [DataRow(0.0, 5.0, 3.0, 8.0, false)]
    [DataRow(3.0, 8.0, 0.0, 5.0, false)]
    [DataRow(8.0, 11.0, 0.0, 10.0, false)]
    [DataRow(10.0, 12.0, 0.0, 10.0, false)]
    [DataRow(20.0, 22.0, 0.0, 10.0, false)]
    public void IsEnclosedBy(double s1, double e1, double s2, double e2, bool expected)
    {
        Assert.AreEqual(expected, H(s1, e1).IsEnclosedBy(H(s2, e2)));
    }

    #endregion

    #region Kind mismatch / default arguments

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

    private static void InvokeOp(string op, DateTimeRange a, DateTimeRange b)
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
    public void BinaryOperation_MismatchedKind_ThrowsArgumentException(string op)
    {
        // Operands overlap (and neither encloses the other), so the only problem is the Kind mismatch.
        var utc = H(0, 5, DateTimeKind.Utc);
        var local = H(3, 8, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => InvokeOp(op, utc, local), "utc op local");
        Assert.Throws<ArgumentException>(() => InvokeOp(op, local, utc), "local op utc");
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
    public void Union_Overlapping()
    {
        AssertHours(0, 8, H(0, 5).Union(H(3, 8)));
        AssertHours(0, 8, H(3, 8).Union(H(0, 5)));
    }

    [TestMethod]
    public void Union_Adjacent()
    {
        AssertHours(0, 8, H(0, 5).Union(H(5, 8)));
        AssertHours(0, 8, H(5, 8).Union(H(0, 5)));
    }

    [TestMethod]
    public void Union_Enclosed()
    {
        AssertHours(0, 10, H(0, 10).Union(H(3, 5)));
        AssertHours(0, 10, H(3, 5).Union(H(0, 10)));
    }

    [TestMethod]
    public void Union_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Union(H(6, 8)));
        Assert.Throws<ArgumentException>(() => H(6, 8).Union(H(0, 5)));
    }

    [TestMethod]
    public void Union_PreservesKind()
    {
        var result = H(0, 5, DateTimeKind.Utc).Union(H(3, 8, DateTimeKind.Utc));
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    [TestMethod]
    public void Intersect_Overlapping()
    {
        AssertHours(3, 5, H(0, 5).Intersect(H(3, 8)));
        AssertHours(3, 5, H(3, 8).Intersect(H(0, 5)));
    }

    [TestMethod]
    public void Intersect_Enclosed()
    {
        AssertHours(3, 5, H(0, 10).Intersect(H(3, 5)));
        AssertHours(3, 5, H(3, 5).Intersect(H(0, 10)));
    }

    [TestMethod]
    public void Intersect_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Intersect(H(5, 8)));
        Assert.Throws<ArgumentException>(() => H(5, 8).Intersect(H(0, 5)));
    }

    [TestMethod]
    public void Intersect_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Intersect(H(6, 8)));
    }

    [TestMethod]
    public void Intersect_PreservesKind()
    {
        var result = H(0, 5, DateTimeKind.Local).Intersect(H(3, 8, DateTimeKind.Local));
        Assert.AreEqual(DateTimeKind.Local, result.Kind);
    }

    [TestMethod]
    public void ExpandToInclude_Disjoint()
    {
        AssertHours(0, 10, H(0, 2).ExpandToInclude(H(8, 10)));
        AssertHours(0, 10, H(8, 10).ExpandToInclude(H(0, 2)));
    }

    [TestMethod]
    public void ExpandToInclude_Enclosed()
    {
        AssertHours(0, 10, H(0, 10).ExpandToInclude(H(3, 5)));
        AssertHours(0, 10, H(3, 5).ExpandToInclude(H(0, 10)));
    }

    #endregion

    #region Subtract

    [TestMethod]
    public void Subtract_Middle_Splits()
    {
        var left = H(0, 10).Subtract(H(3, 5), out var right);

        AssertHours(0, 3, left);
        AssertHours(5, 10, right);
    }

    [TestMethod]
    public void Subtract_LeftSide()
    {
        var result = H(3, 10).Subtract(H(0, 5), out var newRange);

        AssertHours(5, 10, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_RightSide()
    {
        var result = H(0, 10).Subtract(H(5, 15), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_SharedStart()
    {
        var result = H(0, 10).Subtract(H(0, 4), out var newRange);

        AssertHours(4, 10, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_SharedEnd()
    {
        var result = H(0, 10).Subtract(H(6, 10), out var newRange);

        AssertHours(0, 6, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void Subtract_Enclosing_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(3, 5).Subtract(H(0, 10), out _), "strictly enclosing");
        Assert.Throws<ArgumentException>(() => H(3, 5).Subtract(H(3, 5), out _), "identical");
        Assert.Throws<ArgumentException>(() => H(3, 5).Subtract(H(3, 10), out _), "shared start");
    }

    [TestMethod]
    public void Subtract_Disjoint_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Subtract(H(6, 8), out _));
    }

    [TestMethod]
    public void Subtract_Adjacent_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(0, 5).Subtract(H(5, 8), out _));
        Assert.Throws<ArgumentException>(() => H(5, 8).Subtract(H(0, 5), out _));
    }

    [TestMethod]
    public void Subtract_PreservesKind()
    {
        var left = H(0, 10, DateTimeKind.Utc).Subtract(H(3, 5, DateTimeKind.Utc), out var right);

        Assert.AreEqual(DateTimeKind.Utc, left!.Value.Kind);
        Assert.AreEqual(DateTimeKind.Utc, right!.Value.Kind);
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
        var result = H(3, 10).Subtract(H(0, 5)).ToList();

        Assert.HasCount(1, result);
        AssertHours(5, 10, result[0]);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Disjoint_ReturnsThis()
    {
        var result = H(0, 5).SubtractNonIntersecting(H(6, 8), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Adjacent_ReturnsThis()
    {
        var result = H(0, 5).SubtractNonIntersecting(H(5, 8), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Enclosed_ReturnsNull()
    {
        var result = H(3, 5).SubtractNonIntersecting(H(0, 10), out var newRange);

        Assert.IsNull(result);
        Assert.IsNull(newRange);
    }

    [TestMethod]
    public void SubtractNonIntersecting_Identical_ReturnsNull()
    {
        var result = H(3, 5).SubtractNonIntersecting(H(3, 5), out var newRange);

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

    [TestMethod]
    public void SubtractNonIntersecting_OneSide()
    {
        var result = H(0, 10).SubtractNonIntersecting(H(5, 15), out var newRange);

        AssertHours(0, 5, result);
        Assert.IsNull(newRange);
    }

    #endregion

    #region Shift / Inflate / With / BoundTo

    [TestMethod]
    public void Shift_MovesBothEnds()
    {
        AssertHours(3, 7, H(1, 5).Shift(TimeSpan.FromHours(2)));
        AssertHours(-1, 3, H(1, 5).Shift(TimeSpan.FromHours(-2)));
    }

    [TestMethod]
    public void Shift_PreservesKind()
    {
        Assert.AreEqual(DateTimeKind.Utc, H(1, 5, DateTimeKind.Utc).Shift(TimeSpan.FromHours(2)).Kind);
    }

    [TestMethod]
    public void Inflate_Pads()
    {
        AssertHours(0, 8, H(1, 5).Inflate(TimeSpan.FromHours(1), TimeSpan.FromHours(3)));
        AssertHours(0, 5, H(1, 5).Inflate(padLeft: TimeSpan.FromHours(1)));
        AssertHours(1, 8, H(1, 5).Inflate(padRight: TimeSpan.FromHours(3)));
    }

    [TestMethod]
    public void Inflate_Negative_Shrinks()
    {
        AssertHours(2, 4, H(1, 5).Inflate(TimeSpan.FromHours(-1), TimeSpan.FromHours(-1)));
    }

    [TestMethod]
    public void Inflate_ShrinkToZero_Allowed()
    {
        AssertHours(3, 3, H(1, 5).Inflate(TimeSpan.FromHours(-2), TimeSpan.FromHours(-2)));
    }

    [TestMethod]
    public void Inflate_ShrinkPastZero_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(1, 5).Inflate(TimeSpan.FromHours(-3), TimeSpan.FromHours(-3)));
    }

    [TestMethod]
    public void Inflate_PreservesKind()
    {
        Assert.AreEqual(DateTimeKind.Utc, H(1, 5, DateTimeKind.Utc).Inflate(TimeSpan.FromHours(1), TimeSpan.FromHours(1)).Kind);
    }

    [TestMethod]
    public void With_Start()
    {
        AssertHours(0, 5, H(1, 5).With(start: T(0)));
    }

    [TestMethod]
    public void With_End()
    {
        AssertHours(1, 9, H(1, 5).With(end: T(9)));
    }

    [TestMethod]
    public void With_Both()
    {
        AssertHours(7, 9, H(1, 5).With(T(7), T(9)));
    }

    [TestMethod]
    public void With_Nothing_ReturnsEqual()
    {
        AssertHours(1, 5, H(1, 5).With());
    }

    [TestMethod]
    public void With_StartAfterEnd_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(1, 5).With(start: T(6)));
    }

    [TestMethod]
    public void With_MismatchedKind_Throws()
    {
        Assert.Throws<ArgumentException>(() => H(1, 5, DateTimeKind.Utc).With(end: T(9, DateTimeKind.Local)));
    }

    [TestMethod]
    public void BoundTo_StartOnly()
    {
        AssertHours(3, 10, H(0, 10).BoundTo(T(3)));
    }

    [TestMethod]
    public void BoundTo_EndOnly()
    {
        AssertHours(0, 6, H(0, 10).BoundTo(endDate: T(6)));
    }

    [TestMethod]
    public void BoundTo_Both()
    {
        AssertHours(3, 6, H(0, 10).BoundTo(T(3), T(6)));
    }

    [TestMethod]
    public void BoundTo_WiderBounds_ReturnsEqual()
    {
        AssertHours(3, 6, H(3, 6).BoundTo(T(0), T(10)));
    }

    [TestMethod]
    public void BoundTo_NoBounds_ReturnsEqual()
    {
        AssertHours(3, 6, H(3, 6).BoundTo());
    }

    [TestMethod]
    public void BoundTo_PreservesKind()
    {
        var result = H(0, 10, DateTimeKind.Utc).BoundTo(T(3, DateTimeKind.Utc), T(6, DateTimeKind.Utc));

        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        AssertRange(T(3, DateTimeKind.Utc), T(6, DateTimeKind.Utc), result);
    }

    #endregion

    #region Equality / comparison

    [TestMethod]
    public void Equals_SameValues()
    {
        var a = H(1, 5);
        var b = H(1, 5);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [TestMethod]
    [DataRow(1.0, 6.0)]
    [DataRow(0.0, 5.0)]
    public void Equals_DifferentValues(double s, double e)
    {
        var a = H(1, 5);
        var b = H(s, e);

        Assert.IsFalse(a.Equals(b));
        Assert.IsFalse(a.Equals((object)b));
        Assert.IsFalse(a == b);
        Assert.IsTrue(a != b);
    }

    [TestMethod]
    public void Equals_NullOrOtherType_False()
    {
        Assert.IsFalse(H(1, 5).Equals((object)null!));
        Assert.IsFalse(H(1, 5).Equals("x"));
    }

    [TestMethod]
    [DataRow(DateTimeKind.Utc, DateTimeKind.Local)]
    [DataRow(DateTimeKind.Utc, DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Local, DateTimeKind.Unspecified)]
    public void Equals_DifferentKind_DoesNotThrow_AndHashIsConsistent(DateTimeKind k1, DateTimeKind k2)
    {
        var a = H(1, 5, k1);
        var b = H(1, 5, k2);

        if (a.Equals(b))
        {
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }

    [TestMethod]
    public void CompareTo_OrdersByStartThenLength()
    {
        Assert.IsLessThan(0, H(1, 10).CompareTo(H(2, 3)));
        Assert.IsGreaterThan(0, H(2, 3).CompareTo(H(1, 10)));
        Assert.IsLessThan(0, H(1, 2).CompareTo(H(1, 3)));
        Assert.IsGreaterThan(0, H(1, 3).CompareTo(H(1, 2)));
        Assert.AreEqual(0, H(1, 3).CompareTo(H(1, 3)));
    }

    [TestMethod]
    public void CompareToObject_MatchesTyped()
    {
        Assert.IsLessThan(0, H(1, 10).CompareTo((object)H(2, 3)));
        Assert.IsGreaterThan(0, H(1, 3).CompareTo((object)H(1, 2)));
        Assert.AreEqual(0, H(1, 3).CompareTo((object)H(1, 3)));
    }

    [TestMethod]
    public void CompareToObject_Null_IsGreater()
    {
        Assert.IsGreaterThan(0, H(1, 3).CompareTo((object?)null));
    }

    [TestMethod]
    public void CompareToObject_OtherType_DoesNotThrow()
    {
        _ = H(1, 3).CompareTo("hello");
    }

    [TestMethod]
    public void ComparisonOperators()
    {
        var a = H(1, 3);
        var b = H(2, 3);
        var c = H(1, 4);
        var a2 = H(1, 3);

        Assert.IsTrue(a < b);
        Assert.IsTrue(a <= b);
        Assert.IsFalse(a > b);
        Assert.IsFalse(a >= b);
        Assert.IsTrue(b > a);
        Assert.IsTrue(b >= a);
        Assert.IsTrue(a < c);
        Assert.IsTrue(c > a);
        Assert.IsTrue(a <= a2);
        Assert.IsTrue(a >= a2);
        Assert.IsFalse(a < a2);
        Assert.IsFalse(a > a2);
    }

    #endregion

    #region AsEnumerable

    [TestMethod]
    public void AsEnumerable_MidnightToMidnight_YieldsEachDay()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 4));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 1), new DateTime(2024, 1, 2), new DateTime(2024, 1, 3) },
            range.AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_MidDayBounds_YieldsMidnightsInRange()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 3, 12, 0, 0));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 2), new DateTime(2024, 1, 3) },
            range.AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_WithinOneDay_NoMidnight_Empty()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 6, 0, 0), new DateTime(2024, 1, 1, 18, 0, 0));

        Assert.HasCount(0, range.AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_ZeroLength_Empty()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1));

        Assert.HasCount(0, range.AsEnumerable().ToList());
    }

    [TestMethod]
    public void AsEnumerable_PreservesKind()
    {
        // Assumed contract: yielded DateTimes carry the range's Kind.
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var result = range.AsEnumerable().ToList();

        Assert.HasCount(2, result);
        Assert.IsTrue(result.All(d => d.Kind == DateTimeKind.Utc));
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_RespectsBounds()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 3, 12, 0, 0));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 2, 8, 0, 0), new DateTime(2024, 1, 3, 8, 0, 0) },
            range.AsEnumerable(TimeSpan.FromHours(8)).ToList());
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_StartInclusiveEndExclusive()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 3, 12, 0, 0));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 2, 12, 0, 0) },
            range.AsEnumerable(TimeSpan.FromHours(12)).ToList());
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_Midnight_ExcludesEnd()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 1), new DateTime(2024, 1, 2) },
            range.AsEnumerable(TimeSpan.Zero).ToList());
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_WithinSingleDay()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 6, 0, 0), new DateTime(2024, 1, 1, 18, 0, 0));

        CollectionAssert.AreEqual(new[] { new DateTime(2024, 1, 1, 8, 0, 0) },
            range.AsEnumerable(TimeSpan.FromHours(8)).ToList());
        Assert.HasCount(0, range.AsEnumerable(TimeSpan.FromHours(20)).ToList());
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_PreservesKind()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var result = range.AsEnumerable(TimeSpan.FromHours(8)).ToList();

        Assert.HasCount(2, result);
        Assert.IsTrue(result.All(d => d.Kind == DateTimeKind.Utc));
    }

    [TestMethod]
    public void AsEnumerable_TimesOfDay_UnsortedInput_YieldsChronologically()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        var result = range.AsEnumerable([TimeSpan.FromHours(20), TimeSpan.FromHours(8)]).ToList();

        CollectionAssert.AreEqual(new[]
        {
            new DateTime(2024, 1, 1, 8, 0, 0),
            new DateTime(2024, 1, 1, 20, 0, 0),
            new DateTime(2024, 1, 2, 8, 0, 0),
            new DateTime(2024, 1, 2, 20, 0, 0),
        }, result);
    }

    [TestMethod]
    public void AsEnumerable_TimesOfDay_UnsortedInput_DoesNotSkipOnLastDay()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2, 10, 0, 0));

        var result = range.AsEnumerable([TimeSpan.FromHours(20), TimeSpan.FromHours(8)]).ToList();

        CollectionAssert.Contains(result, new DateTime(2024, 1, 2, 8, 0, 0));
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public void AsEnumerable_TimesOfDay_PartialDays()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 2, 12, 0, 0));

        var result = range.AsEnumerable([TimeSpan.FromHours(8), TimeSpan.FromHours(20)]).ToList();

        CollectionAssert.AreEqual(new[]
        {
            new DateTime(2024, 1, 1, 20, 0, 0),
            new DateTime(2024, 1, 2, 8, 0, 0),
        }, result);
    }

    [TestMethod]
    public void AsEnumerable_TimesOfDay_Empty_YieldsNothing()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        Assert.HasCount(0, range.AsEnumerable(Array.Empty<TimeSpan>()).ToList());
    }

    [TestMethod]
    [DataRow(24.0)]
    [DataRow(25.0)]
    public void AsEnumerable_TimeOfDay_24hOrMore_Throws(double hours)
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        Assert.Throws<ArgumentException>(() => range.AsEnumerable(TimeSpan.FromHours(hours)).ToList(), "single");
        Assert.Throws<ArgumentException>(() => range.AsEnumerable([TimeSpan.FromHours(8), TimeSpan.FromHours(hours)]).ToList(), "array");
    }

    [TestMethod]
    public void AsEnumerable_TimeOfDay_Negative_Throws()
    {
        // Assumed contract: a negative time of day is invalid, like one >= 24h.
        var range = new DateTimeRange(new DateTime(2024, 1, 1), new DateTime(2024, 1, 3));

        Assert.Throws<ArgumentException>(() => range.AsEnumerable(TimeSpan.FromHours(-1)).ToList());
    }

    [TestMethod]
    public void AsEnumerable_Transform_YieldsTransformedInChronologicalOrder()
    {
        var range = new DateTimeRange(new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 3, 12, 0, 0));
        var sources = new[] { "evening", "morning" };

        var result = range.AsEnumerable<string, string>(sources,
            s => s == "morning" ? TimeSpan.FromHours(8) : TimeSpan.FromHours(20),
            (s, dt) => s + "@" + dt.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)).ToList();

        Assert.AreEqual(string.Join(" ", new[]
        {
            "evening@2024-01-01T20:00",
            "morning@2024-01-02T08:00",
            "evening@2024-01-02T20:00",
            "morning@2024-01-03T08:00",
        }), string.Join(" ", result));
    }

    #endregion

    #region DataContract

    [TestMethod]
    [DataRow(DateTimeKind.Unspecified)]
    [DataRow(DateTimeKind.Utc)]
    [DataRow(DateTimeKind.Local)]
    public void DataContract_RoundTrips(DateTimeKind kind)
    {
        var range = H(1.5, 30.25, kind);

        var result = DataContractRoundTrip(range);

        Assert.AreEqual(range, result);
        Assert.AreEqual(kind, result.Kind);
        Assert.AreEqual(range.Start.Ticks, result.Start.Ticks);
        Assert.AreEqual(range.End.Ticks, result.End.Ticks);
    }

    #endregion
}
