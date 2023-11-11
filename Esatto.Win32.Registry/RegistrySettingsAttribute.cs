using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Esatto.Win32.Registry
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class RegistrySettingsAttribute : Attribute
    {
        public RegistrySettingsAttribute(string path)
        {
            this.Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public string Path { get; }

        public bool IsParameterized => Path.IndexOf("{param}", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
