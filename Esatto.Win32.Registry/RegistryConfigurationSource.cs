using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Runtime.Versioning;

namespace Esatto.Win32.Registry
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    internal class RegistryConfigurationSource : ConfigurationProvider, IConfigurationSource
    {
        private readonly RegistryKey key;

        public RegistryConfigurationSource(RegistryKey key)
        {
            this.key = key;
        }

        IConfigurationProvider IConfigurationSource.Build(IConfigurationBuilder builder) => this;

        public override void Load()
        {
            AddKey(key, "");
        }

        private void AddKey(RegistryKey key, string prefix)
        {
            // block loops (symlinks)
            if (prefix.Length > 1000)
            {
                throw new StackOverflowException("Registry key contains cycles or path exceeds max_len");
            }

            foreach (var name in key.GetValueNames())
            {
                var cName = name;
                if (string.IsNullOrEmpty(name))
                {
                    cName = "Default";
                }
                Data.Add(prefix + cName, key.GetValue(name)?.ToString() ?? "");
            }

            foreach (var subkeyName in key.GetSubKeyNames())
            {
                using var subkey = key.OpenSubKey(subkeyName)
                    ?? throw new UnauthorizedAccessException($"Cannot open '{subkeyName}'");
                var subkeyPrefix = $"{prefix}{subkeyName}{ConfigurationPath.KeyDelimiter}";
                AddKey(subkey, subkeyPrefix);
            }
        }
    }
}