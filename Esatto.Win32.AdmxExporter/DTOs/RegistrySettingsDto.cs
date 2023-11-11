#nullable disable

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Esatto.Win32.Registry.AdmxExporter
{
    [DataContract(Namespace = "urn:esatto:registry", Name = "RegistrySettings")]
    public sealed class RegistrySettingsDto
    {
        [DataMember]
        public string AssemblyName { get; set; }

        [DataMember]
        public string Uuid { get; set; }

        [DataMember]
        public List<RegistrySettingsKey> Keys { get; set; } = new();

        [DataMember]
        public List<RegistryCategoryDto> Categories { get; set; } = new();
    }
}
