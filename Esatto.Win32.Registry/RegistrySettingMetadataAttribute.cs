using Microsoft.Win32;
using System;

namespace Esatto.Win32.Registry
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class RegistrySettingMetadataAttribute : Attribute
    {
        public RegistrySettingMetadataAttribute(string name, RegistryValueKind type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type;
        }

        public string Name { get; }
        public RegistryValueKind Type { get; }
        public string? DefaultValue { get; set; }
    }
}
