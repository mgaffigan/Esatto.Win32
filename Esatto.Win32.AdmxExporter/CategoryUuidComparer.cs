using System.Collections.Generic;

namespace Esatto.Win32.Registry.AdmxExporter
{
    internal sealed class CategoryUuidComparer : IEqualityComparer<RegistryCategoryDto>
    {
        public bool Equals(RegistryCategoryDto x, RegistryCategoryDto y)
        {
            return x?.Uuid == y?.Uuid;
        }

        public int GetHashCode(RegistryCategoryDto obj)
        {
            return obj.Uuid.GetHashCode();
        }
    }
}
