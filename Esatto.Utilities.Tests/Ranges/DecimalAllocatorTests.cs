using System.Globalization;

namespace Esatto.Utilities.Tests.Ranges;

[TestClass]
public class DecimalAllocatorTests
{
    private static decimal Unit(int decimals) => 1m / (decimal)Math.Pow(10, decimals);

    [TestMethod]
    public void RoundsToTotalBeforeFullWeight_DoesNotThrow()
    {
        // 999.999 of 1000 rounds to the full 1.00 before the last sliver of weight is allocated
        var results = AllocateAll(1.00m, 2, 999.999m, 0.001m);

        CollectionAssert.AreEqual(new[] { 1.00m, 0.00m }, results);
    }

    private static void AssertRounded(decimal value, int decimals, string? message = null)
        => Assert.AreEqual(Math.Round(value, decimals), value, $"{value} not rounded to {decimals} places. {message}");

    /// <summary>
    /// Allocates all <paramref name="weights"/> and checks the invariants of the contract.
    /// </summary>
    private static List<decimal> AllocateAll(decimal total, int decimals, params decimal[] weights)
    {
        var totalWeight = weights.Sum();
        var allocator = new DecimalAllocator(total, totalWeight, decimals);
        var results = new List<decimal>();
        decimal cumulativeWeight = 0;
        foreach (var w in weights)
        {
            var amount = allocator.AllocateAmount(w);
            results.Add(amount);
            cumulativeWeight += w;

            AssertRounded(amount, decimals, $"allocation #{results.Count}");
            var exactShare = total * w / totalWeight;
            Assert.IsLessThanOrEqualTo(Unit(decimals), Math.Abs(amount - exactShare),
                $"allocation #{results.Count} = {amount}, exact share {exactShare}");
            Assert.AreEqual(results.Sum(), allocator.AllocatedAmount, "AllocatedAmount");
            Assert.AreEqual(cumulativeWeight, allocator.AllocatedWeight, "AllocatedWeight");
        }

        Assert.AreEqual(total, results.Sum(), $"Sum of {string.Join(", ", results)}");
        Assert.AreEqual(total, allocator.AllocatedAmount);
        allocator.AssertFullyAllocated();
        return results;
    }

    [TestMethod]
    public void Ctor_SetsProperties()
    {
        var a = new DecimalAllocator(100m, 3m, 2);

        Assert.AreEqual(100m, a.TotalAmount);
        Assert.AreEqual(3m, a.TotalWeight);
        Assert.AreEqual(2, a.Decimals);
        Assert.AreEqual(0m, a.AllocatedAmount);
        Assert.AreEqual(0m, a.AllocatedWeight);
    }

    [TestMethod]
    public void EvenSplit_Exact()
    {
        CollectionAssert.AreEqual(new[] { 25m, 75m }, AllocateAll(100m, 2, 1m, 3m));
    }

    [TestMethod]
    public void HalfAndHalf_ZeroDecimals()
    {
        CollectionAssert.AreEqual(new[] { 5m, 5m }, AllocateAll(10m, 0, 0.5m, 0.5m));
    }

    [TestMethod]
    public void Thirds_SumExactly()
    {
        var results = AllocateAll(100m, 2, 1m, 1m, 1m);

        Assert.IsTrue(results.All(r => r == 33.33m || r == 33.34m), string.Join(", ", results));
    }

    [TestMethod]
    public void OneDollarThreeWays_SumExactly()
    {
        AllocateAll(1m, 2, 1m, 1m, 1m);
    }

    [TestMethod]
    public void Sevenths_SumExactly()
    {
        AllocateAll(100m, 2, 1m, 1m, 1m, 1m, 1m, 1m, 1m);
    }

    [TestMethod]
    public void ManySmallAllocations_SumExactly()
    {
        AllocateAll(10m, 2, Enumerable.Repeat(1m, 300).ToArray());
    }

    [TestMethod]
    public void UnevenWeights_SumExactly()
    {
        AllocateAll(99.99m, 2, 0.1m, 0.2m, 0.3m, 0.4m);
        AllocateAll(1234.57m, 2, 3m, 7m, 11m, 13m, 17m);
        AllocateAll(0.05m, 2, 1m, 1m, 1m, 1m, 1m, 1m, 1m);
    }

    [TestMethod]
    public void ZeroDecimals_SumExactly()
    {
        AllocateAll(10m, 0, 1m, 1m, 1m);
        AllocateAll(7m, 0, 2m, 2m, 2m, 2m);
    }

    [TestMethod]
    public void FourDecimals_SumExactly()
    {
        AllocateAll(1m, 4, 1m, 1m, 1m, 1m, 1m, 1m);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void AllocateAmount_NonPositiveWeight_Throws(int weight)
    {
        var a = new DecimalAllocator(100m, 2m, 2);

        Assert.Throws<ArgumentException>(() => a.AllocateAmount(weight));
    }

    [TestMethod]
    public void SingleAllocation_GetsEverything()
    {
        CollectionAssert.AreEqual(new[] { 123.45m }, AllocateAll(123.45m, 2, 5m));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-100")]
    [DataRow("0.001")]
    public void Ctor_NonPositiveAmount_Throws(string totalAmount)
    {
        Assert.Throws<ArgumentException>(() => new DecimalAllocator(decimal.Parse(totalAmount, CultureInfo.InvariantCulture), 3m, 2));
    }

    [TestMethod]
    public void AssertFullyAllocated_Fresh_Throws()
    {
        var a = new DecimalAllocator(100m, 3m, 2);

        Assert.ThrowsExactly<InvalidOperationException>(() => a.AssertFullyAllocated());
    }

    [TestMethod]
    public void AssertFullyAllocated_Partial_Throws()
    {
        var a = new DecimalAllocator(100m, 3m, 2);
        a.AllocateAmount(1m);
        a.AllocateAmount(1m);

        Assert.ThrowsExactly<InvalidOperationException>(() => a.AssertFullyAllocated());
    }

    [TestMethod]
    public void AssertFullyAllocated_Full_DoesNotThrow()
    {
        var a = new DecimalAllocator(100m, 3m, 2);
        a.AllocateAmount(1m);
        a.AllocateAmount(2m);

        a.AssertFullyAllocated();
    }

    [TestMethod]
    public void RunningTotals_Track()
    {
        var a = new DecimalAllocator(100m, 4m, 2);

        var first = a.AllocateAmount(1m);
        Assert.AreEqual(25m, first);
        Assert.AreEqual(25m, a.AllocatedAmount);
        Assert.AreEqual(1m, a.AllocatedWeight);

        var second = a.AllocateAmount(2m);
        Assert.AreEqual(50m, second);
        Assert.AreEqual(75m, a.AllocatedAmount);
        Assert.AreEqual(3m, a.AllocatedWeight);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void Ctor_InvalidDecimals_Throws(int decimals)
    {
        // Assumed contract: decimals must be a valid rounding precision (0..28), as for Math.Round.
        Assert.Throws<ArgumentException>(() => new DecimalAllocator(100m, 1m, decimals));
    }

    [TestMethod]
    [DataRow(0.0)]
    [DataRow(-1.0)]
    public void Ctor_NonPositiveTotalWeight_Throws(double totalWeight)
    {
        // Assumed contract: total weight must be positive (it is a divisor).
        Assert.Throws<ArgumentException>(() => new DecimalAllocator(100m, (decimal)totalWeight, 2));
    }
}
