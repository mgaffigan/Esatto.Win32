namespace Esatto.Utilities
{
    public interface IRangeMutator
    {
        Range Constrain(Range range);

        Range Range { get; set; }

        RangeMutatorMode UpdateMode { get; }
    }
}
