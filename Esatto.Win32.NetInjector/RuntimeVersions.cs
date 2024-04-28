﻿namespace Esatto.Win32.NetInjector;

public class RuntimeVersions
{
    /// <summary>
    /// Use the loaded version of .NET Framework or the latest version if the .Net Framework is not loaded.
    /// Ignores all versions of .NET Core.  Default behavior for Injector.Inject.
    /// </summary>
    public const string? NetFxAny = null;

    /// <summary>
    /// Use or load the latest version of .NET Framework 2.0
    /// </summary>
    public const string? NetFx2 = "v2";

    /// <summary>
    /// Use or load the latest version of .NET Framework 4
    /// </summary>
    public const string? NetFx4 = "v4";

    /// <summary>
    /// Use the loaded version of .NET Core.  Fails if no .NET Core runtime is loaded in the process
    /// </summary>
    public const string? LoadedNetCore = "netcore";
}
