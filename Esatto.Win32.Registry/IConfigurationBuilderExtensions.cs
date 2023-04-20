using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Esatto.Win32.Registry
{
    public static class IConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddRegistry(this IConfigurationBuilder configurationBuilder,
            string path, bool optional = true)
        {
#pragma warning disable CA1416 // Validate platform compatibility (enum doesn't exist)
            configurationBuilder.AddRegistry(RegistryHive.LocalMachine, path, optional: optional);
            configurationBuilder.AddRegistry(RegistryHive.CurrentUser, path, optional: true);
#pragma warning restore CA1416 // Validate platform compatibility
            return configurationBuilder;
        }

#if NET
        [SupportedOSPlatform("windows")]
#endif
        public static IConfigurationBuilder AddRegistry(this IConfigurationBuilder configurationBuilder, 
            RegistryHive hive, string path, RegistryView view = RegistryView.Registry64, bool optional = true)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                var key = root.OpenSubKey(path);
                if (key != null)
                {
                    configurationBuilder.Add(new RegistryConfigurationSource(key));
                }
                else if (!optional)
                {
                    throw new FileNotFoundException($"Could not find key '{path}' in {hive} ({view})");
                }
            }
            return configurationBuilder;
        }
    }
}
