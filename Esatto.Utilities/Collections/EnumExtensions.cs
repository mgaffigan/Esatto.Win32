namespace Esatto.Utilities
{
    public static class EnumExtensions
    {
        public static bool In<TEnum>(this TEnum e, params TEnum[] list)
        {
            return list.Contains(e);
        }
    }
}
