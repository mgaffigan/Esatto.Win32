## ADMX Export for Esatto.Win32.Registry wrapper classes
[![Nuget](https://img.shields.io/nuget/v/Esatto.Win32.Registry.AdmxExporter)](https://www.nuget.org/packages/Esatto.Win32.Registry.AdmxExporter)

Wrapper classes using [`Esatto.Win32.Registry`](https://github.com/mgaffigan/Esatto.Win32/blob/master/Esatto.Win32.Registry/Readme.md)
can be exported to an ADMX file by adding a reference to 
[`Esatto.Win32.Registry.AdmxExporter`](https://www.nuget.org/packages/Esatto.Win32.Registry.AdmxExporter).

```MSBuild
<ItemGroup>
    <PackageReference Include="Esatto.Win32.Registry" Version="3.0.5" />
    <PackageReference Include="Esatto.Win32.Registry.AdmxExporter" Version="3.0.5" />
</ItemGroup>
```

The ADMX file will be generated in the build output directory 
under `PolicyDefinitions` and can be loaded into Active Directory 
by adding them to the [group policy central store](https://learn.microsoft.com/en-us/troubleshoot/windows-client/group-policy/create-and-manage-central-store#updating-the-administrative-templates-files), 
or by uploading them to Intune or another MDM.  For testing, add them to `C:\Windows\PolicyDefinitions` on your local machine 
and use `gpedit.msc`.

Example appearance in Group Policy Management Center (GPMC):

![Group Policy Management Center showing the loaded ADMX file](https://github.com/mgaffigan/Esatto.Win32/raw/master/Esatto.Win32.Registry/Readme_GPMC.png)

## Customizing the ADMX file

* The category ("In Touch Technologies > Example Product > Desktop Client" in the example) will default to the registry path, but can be overridden with `[Category("")]`.
* `[DisplayName("")]` will set the name of the setting.
* `[Description("")]` will set the help text for the setting.
* `[ChildSettingOf(""]` will cause a setting to be added to the "Options" pane of another setting (`presentation` in the ADMX schema).
* `[RegistrySettingsMetadata(string)]` can be used to export a non-public type, a type with a parameterized constructor, or a type which does not directly inherit from `RegistrySettings`.
* `[RegistrySettingMetadata(string, RegistryValueKind, DefaultValue = string)]` can be used for computed and non-trivial properties / values.

Example:

    public sealed class DesktopClientSettings : RegistrySettings
    {
        public DesktopClientSettings()
            : base(@"In Touch Technologies\Esatto\Example App")
        {
        }

        public static DesktopClientSettings Instance { get; } = new();

        [DisplayName("Home URL")]
        [Description("Initial URL to be used for login.  Typically set to the IdP login page for the RP")]
        public string HomeUrl
        {
            get => GetString(nameof(HomeUrl), null);
            set => SetString(nameof(HomeUrl), value);
        }

        [DisplayName("Block Close")]
        [Description("If set, the user cannot use the \"X\" or Alt-F4 to close the window.  You can still close the window by clicking the application icon (Alt+Space) and selecting Quit.  Attempts to close the application will minimize the application instead.")]
        public bool BlockClose
        {
            get => GetBool(nameof(BlockClose), true);
            set => SetBool(nameof(BlockClose), value);
        }

        [DisplayName("Block Close When Minimized")]
        [Description("If Block Close is not set, this setting has no effect.  When block close is set, the user can close the application by right clicking the taskbar and selecting \"Close\".  When this option is set, attempts to close via the taskbar are ignored.")]
        [ChildSettingOf(nameof(BlockClose))]
        public bool BlockCloseWhenMinimized
        {
            get => GetBool(nameof(BlockClose), false);
            set => SetBool(nameof(BlockClose), value);
        }

        [DisplayName("CCP Window Width")]
        [Description("Width of web browser for CCP state")]
        public int CcpWidth
        {
            get => GetInt(nameof(CcpWidth), 400);
            set => SetInt(nameof(CcpWidth), value);
        }

        [DisplayName("CCP Window Height")]
        [Description("Height of web browser for CCP state")]
        public int CcpHeight
        {
            get => GetInt(nameof(CcpHeight), 600);
            set => SetInt(nameof(CcpHeight), value);
        }
    }
