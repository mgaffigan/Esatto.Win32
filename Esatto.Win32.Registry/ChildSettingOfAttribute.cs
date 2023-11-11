using System;

namespace Esatto.Win32.Registry
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ChildSettingOfAttribute : Attribute
    {
        public ChildSettingOfAttribute(string parentSettingName)
        {
            ParentName = parentSettingName ?? throw new ArgumentNullException(nameof(parentSettingName));
        }

        public string ParentName { get; }
    }
}
