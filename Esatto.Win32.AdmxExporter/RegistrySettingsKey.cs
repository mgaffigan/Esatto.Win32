using Esatto.Win32.Registry;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Esatto.Win32.Registry.AdmxExporter
{
    [DataContract(Namespace = "urn:esatto:registry", Name = "Key")]
    public sealed class RegistrySettingsKey
    {
        public RegistrySettingsKey(RegistrySettingsMetadata t)
        {
            this.SettingsTypeName = t.SettingsType.FullName;
            this.SoftwarePath = t.SoftwarePath;
            this.PolicyPath = t.PolicyPath;
            this.Uuid = Program.CalculateMD5Hash(SoftwarePath + SettingsTypeName);

            this.DisplayName = t.DisplayName;
            this.Description = t.Description;
            this.Category = RegistryCategoryDto.ForPath(t.Category).Last();

            this.Settings = t.Settings.Select(s => new RegistrySettingValue(SoftwarePath, s)).ToList();
            foreach (var setting in Settings)
            {
                if (setting.ParentSettingName != null)
                {
                    setting.ParentSettingUuid = Settings.FirstOrDefault(f => f.Name == setting.ParentSettingName)?.Uuid;
                }
            }
        }

        [DataMember]
        public string SettingsTypeName { get; set; }
        [DataMember]
        public string SoftwarePath { get; set; }
        [DataMember]
        public string PolicyPath { get; set; }

        [DataMember]
        public string Uuid { get; set; }

        [DataMember]
        public string DisplayName { get; set; }
        [DataMember]
        public string? Description { get; set; }
        [DataMember]
        public RegistryCategoryDto Category { get; set; }

        [DataMember]
        public List<RegistrySettingValue> Settings { get; set; }
    }
}
