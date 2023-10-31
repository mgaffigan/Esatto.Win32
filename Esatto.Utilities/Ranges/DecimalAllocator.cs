namespace Esatto.Utilities
{
    public sealed class DecimalAllocator
    {
        public int Decimals { get; }

        public decimal TotalAmount { get; }

        public decimal TotalWeight { get; }

        public decimal AllocatedWeight { get; private set; }

        public decimal AllocatedAmount { get; private set; }

        public DecimalAllocator(decimal totalAmount, decimal totalWeight, int decimals)
        {
            if (!(Math.Round(totalAmount, decimals) > 0m))
            {
                throw new ArgumentException("Contract assertion not met: Math.Round(totalAmount, decimals) > 0m", nameof(totalAmount));
            }
            if (!(Math.Round(totalAmount, decimals) == totalAmount))
            {
                throw new ArgumentException("Contract assertion not met: Math.Round(totalAmount, decimals) == totalAmount", nameof(totalAmount));
            }
            if (!(totalWeight > 0m))
            {
                throw new ArgumentOutOfRangeException(nameof(totalWeight), "Contract assertion not met: totalWeight > 0m");
            }

            this.Decimals = decimals;
            this.TotalAmount = totalAmount;
            this.TotalWeight = totalWeight;
            this.AllocatedAmount = 0m;
            this.AllocatedWeight = 0m;
        }

        public decimal AllocateAmount(decimal incrementalWeight)
        {
            if (!(incrementalWeight > 0m))
            {
                throw new ArgumentOutOfRangeException(nameof(incrementalWeight), "Contract assertion not met: incrementalWeight > 0m");
            }
            if (!(incrementalWeight <= TotalWeight - AllocatedWeight))
            {
                throw new ArgumentException("Contract assertion not met: incrementalWeight <= TotalWeight - AllocatedWeight", nameof(incrementalWeight));
            }

            // http://softwareengineering.stackexchange.com/questions/340393/allocating-an-integer-sum-proportionally-to-a-set-of-reals
            decimal newAllocatedWeight = AllocatedWeight + incrementalWeight;
            decimal newAllocatedAmount = Math.Round((TotalAmount * newAllocatedWeight) / TotalWeight, Decimals);
            decimal thisDelta = newAllocatedAmount - AllocatedAmount;

            this.AllocatedWeight = newAllocatedWeight;
            this.AllocatedAmount = newAllocatedAmount;

            if (!(AllocatedWeight <= TotalWeight))
            {
                throw new InvalidOperationException("Contract assertion not met: AllocatedWeight <= TotalWeight");
            }
            if (!(AllocatedAmount <= TotalAmount))
            {
                throw new InvalidOperationException("Contract assertion not met: AllocatedAmount <= TotalAmount");
            }
            if (!((AllocatedAmount == TotalAmount) == (AllocatedWeight == TotalWeight)))
            {
                throw new InvalidOperationException("Contract assertion not met: (AllocatedAmount == TotalAmount) == (AllocatedWeight == TotalWeight)");
            }
            return thisDelta;
        }

        public void AssertFullyAllocated()
        {
            if (AllocatedAmount != TotalAmount
                || AllocatedWeight != TotalWeight)
            {
                throw new InvalidOperationException("Amount was not fully allocated");
            }
        }
    }
}
