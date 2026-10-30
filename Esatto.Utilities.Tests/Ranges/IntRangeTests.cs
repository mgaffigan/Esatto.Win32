namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public class IntRangeTests
{
    [TestMethod]
    [DataRow("1-3,5,7-9", new[] { 1, 2, 3, 5, 7, 8, 9 })]
    [DataRow(" 1 - 3 , 5 ,7- 9 ", new[] { 1, 2, 3, 5, 7, 8, 9 })]
    [DataRow("5", new[] { 5 })]
    [DataRow("0", new[] { 0 })]
    [DataRow("0-2", new[] { 0, 1, 2 })]
    [DataRow("4-4", new[] { 4 })]
    [DataRow("1,2,3", new[] { 1, 2, 3 })]
    [DataRow("10-12,20", new[] { 10, 11, 12, 20 })]
    public void GetRange_ParsesList(string text, int[] expected)
    {
        CollectionAssert.AreEqual(expected, IntRange.GetRange(text).ToList(), text);
    }

    [TestMethod]
    public void GetRange_EmptyString_YieldsNothing()
    {
        // Assumed contract: an empty list parses to no numbers.
        Assert.HasCount(0, IntRange.GetRange("").ToList());
    }

    [TestMethod]
    [DataRow("a")]
    [DataRow("1-b")]
    [DataRow("1-2-3")]
    [DataRow("x1-2")]
    [DataRow("1-2y")]
    public void GetRange_Invalid_ThrowsInvalidCastException(string text)
    {
        // InvalidCastException is the existing (public) behavior for malformed input
        Assert.ThrowsExactly<InvalidCastException>(() => IntRange.GetRange(text).ToList());
    }

    [TestMethod]
    [DataRow(new[] { 1, 2, 3, 5, 7, 8, 9 }, "1-3,5,7-9")]
    [DataRow(new[] { 5 }, "5")]
    [DataRow(new[] { 1, 3, 5 }, "1,3,5")]
    [DataRow(new[] { 0, 1, 2, 3 }, "0-3")]
    [DataRow(new[] { 1, 2, 3, 10 }, "1-3,10")]
    [DataRow(new[] { 1, 10, 11, 12 }, "1,10-12")]
    public void GetRange_FormatsCompactList(int[] numbers, string expected)
    {
        // Whitespace in the output is not part of the contract.
        Assert.AreEqual(expected, StripWhitespace(IntRange.GetRange(numbers)));
    }

    private static string StripWhitespace(string s) => new(s.Where(c => !char.IsWhiteSpace(c)).ToArray());

    [TestMethod]
    public void GetRange_TwoConsecutive_CollapsedToDashRange()
    {
        // Assumed contract: any run of consecutive numbers (even of length 2) is written a-b.
        Assert.AreEqual("1-2,4", StripWhitespace(IntRange.GetRange(new[] { 1, 2, 4 })));
    }

    [TestMethod]
    public void GetRange_Empty_ReturnsEmptyString()
    {
        Assert.AreEqual("", IntRange.GetRange(Array.Empty<int>()));
    }

    [TestMethod]
    [DataRow(new[] { 1, 2, 3, 5, 7, 8, 9 })]
    [DataRow(new[] { 0 })]
    [DataRow(new[] { 1, 2 })]
    [DataRow(new[] { 1, 3 })]
    [DataRow(new[] { 0, 1, 2, 4, 6, 7, 100, 101, 102, 103 })]
    public void RoundTrip(int[] numbers)
    {
        var text = IntRange.GetRange(numbers);
        CollectionAssert.AreEqual(numbers, IntRange.GetRange(text).ToList(), text);
    }

    [TestMethod]
    [DataRow("1-3,5,7-9")]
    [DataRow("5")]
    [DataRow("1,3")]
    [DataRow("0-100")]
    public void RoundTrip_FromCanonicalText(string text)
    {
        Assert.AreEqual(text, StripWhitespace(IntRange.GetRange(IntRange.GetRange(text))));
    }
}
