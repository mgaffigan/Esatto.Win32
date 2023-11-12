# Esatto Win32 Registry

Allows use of Registry to store config and preferences via MEDC or wrapper classes.

## Microsoft.Extensions.Configuration

Use `IConfigurationBuilder.AddRegistry` to incorporate values from the registry into MEDC.  Keys
created under the path specified will be added as values will be added recursively.  On 
non-windows platforms (Linux, MacOS) the call has no effect.

Example registry settings:

    Set-ItemProperty -Path "HKLM:\Software\Company Name\Product" -Name "Setting1" -Value "Example value" -Type String

Example use:

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration
        .AddRegistry(@"Software\Company Name\Product")
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables();

    var exampleConfig = builder.Configuration.GetSection("Example").Get<ExampleAppSettings>();
    Assert.AreEqual("Example value", exampleConfig?.Setting1);

    class ExampleAppSettings 
    {
        public string? Setting1 { get; set; }
    }

## Wrapper classes

Inherit from `RegistrySettings` to create a wrapper class that exposes a key for 
read/write.  Writes are only made to HKCU.  Changes are monitored and trigger 
events.  Reads are "live" so setting changes in one app are immediately available 
in other copies of the app.

Types supported:

|Registry Type|.Net Type|Get|Set|
|-|-|-|-|
|`REG_DWORD`|`int`|`GetInt`|`SetInt`|
|`REG_DWORD` (`1` or `0`)|`bool` (`value != 0`)|`GetBool`|`SetBool`|
|`REG_DWORD` (milliseconds)|`TimeSpan`|`GetTimeSpan`|`SetTimeSpan`|
|`REG_SZ`|`string`|`GetString`|`SetString`|
|`REG_SZ`|`Guid`|`GetGuid`|`SetGuid`|
|`REG_SZ` (`enum.ToString()`)|`Enum` (`enum.Parse`)|`GetEnum<T>`|`SetEnum<T>`|
|`REG_EXPAND_SZ`|`string[]`|`GetMultiString`|`SetMultiString`|

Settings are pulled in via first match from:

1. User Group Policy (`HKCU\Policies\{Path}`)
2. Computer Group Policy (`HKLM\Policies\{Path}`)
3. User registry (`HKCU\{Path}`)
4. Computer registry (`HKLM\{Path}`)

Example use:

    Console.WriteLine(ExampleSettings.Instance.ReceiptCodeValidityPeriod);
    ExampleSettings.Instance.ServerName = "example.com";

    internal sealed class ExampleSettings : RegistrySettings
    {
        public static ExampleSettings Instance { get; } = new();

        public ExampleSettings()
            : base(@"Company Name\Product Name")
        {
        }

        public int ReceiptCodeValidityPeriod
        {
            get => GetInt(nameof(ReceiptCodeValidityPeriod), 60);
            set => SetInt(nameof(ReceiptCodeValidityPeriod), value);
        }

        public string ServerName
        {
            get => GetString(nameof(ServerName), null);
            set => SetString(nameof(ServerName), value);
        }

        public bool AutoConnect
        {
            get => GetBool(nameof(AutoConnect), false);
            set => SetBool(nameof(AutoConnect), value);
        }

        public string[] RecentDocuments
        {
            get => GetBool(nameof(RecentDocuments), new string[0]);
            set => SetBool(nameof(RecentDocuments), value);
        }
    }

Subkeys may be exposed as nested `RegistrySettings` instances to permit lists / dictionaries.

## ADMX Export for Wrapper Classes

Wrapper classes may be exported to admx files for use in Group Policy.  Add the nuget package
`Esatto.Win32.Registry.AdmxExporter` to the project containing the wrapper classes.
Annotate the settings with `[DisplayName("Setting Name")]` and other attributes to make things pretty.  See 
[AdmxExporter](https://github.com/mgaffigan/Esatto.Win32/tree/master/Esatto.Win32.Registry.AdmxExporter)
for more details.

```MSBuild
<ItemGroup>
    <PackageReference Include="Esatto.Win32.Registry" Version="3.0.5" />
    <PackageReference Include="Esatto.Win32.Registry.AdmxExporter" Version="3.0.5" />
</ItemGroup>
```

Example appearance in Group Policy Management Center (GPMC):

![Group Policy Management Center showing the loaded ADMX file](https://github.com/mgaffigan/Esatto.Win32/raw/master/Esatto.Win32.Registry/Readme_GPMC.png)