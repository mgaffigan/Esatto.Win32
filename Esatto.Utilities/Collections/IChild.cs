namespace Esatto.Utilities
{
    public interface IChild<TParent>
    {
        TParent? Parent { get; set; }
    }
}
