using Esatto.Win32.Registry;
using System;
using System.Runtime.Serialization;

namespace Esatto.Win32.Registry.AdmxExporter
{
    [DataContract(Namespace = "urn:esatto:registry", Name = "Setting")]
    public sealed class RegistrySettingValue
    {
        public RegistrySettingValue(string parentPath, RegistrySettingMetadata s)
        {
            this.Name = s.Name;
            this.DisplayName = s.DisplayName;
            this.Description = s.Description;
            this.ParentSettingName = s.ParentSettingName;
            this.PropertyType = s.PropertyType;
            this.ValueKind = s.ValueKind.ToString();
            this.DefaultValue = s.DefaultValue;
            this.Uuid = Program.CalculateMD5Hash(parentPath + Name);
        }

        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string DisplayName { get; set; }
        [DataMember]
        public string? Description { get; set; }
        [DataMember]
        public string? ParentSettingName { get; set; }
        [DataMember]
        public string? ParentSettingUuid { get; set; }
        [DataMember]
        public string PropertyType { get; set; }
        [DataMember]
        public string ValueKind { get; set; }
        [DataMember]
        public string? DefaultValue { get; set; }
        [DataMember]
        public string Uuid { get; set; }
    }
}
