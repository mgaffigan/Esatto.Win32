using System.ComponentModel;

namespace Esatto.Win32.Registry.SampleApp;

public sealed class TeleportSettings : RegistrySettings
{
    public static TeleportSettings Instance { get; } = new();

    public TeleportSettings()
        : base(@"SOFTWARE\In Touch Technologies\Esatto\AppCoordination\Teleport")
    {
    }

    [DisplayName("Max In-memory File Size")]
    [Description("Limit over which files must be requested from the client rather than " +
        "sent in the invoke request.  Increasing this limit will increase the amount of " +
        "time the teleport process on the invoker's side will stay running.")]
    public int MaxMemoryFileSize
    {
        get => GetInt(nameof(MaxMemoryFileSize), 1 * 1024 * 1024 /* 1 MB */);
        set => SetInt(nameof(MaxMemoryFileSize), value);
    }

    [DisplayName("Max File Size")]
    [Description("Maximum size of a file which may be transmitted using Teleport.  " +
        "If equal to or less than MaxMemoryFileSize, streaming will be disabled.")]
    public int MaxFileSize
    {
        get => GetInt(nameof(MaxFileSize), 1 * 1024 * 1024 * 1024 /* 1 GB */);
        set => SetInt(nameof(MaxFileSize), value);
    }

    [DisplayName("Permitted File Types")]
    [Description("Semicolon-separated list of file extensions which may be sent via Teleport.  " +
        "If unset or empty, all files may be sent.  If non-empty, only files with extensions " +
        "in this list may be sent.  Note: Blocked File Types takes precedence over permitted.")]
    public string? PermittedFileTypes
    {
        get => GetString(nameof(PermittedFileTypes), null);
        set => SetString(nameof(PermittedFileTypes), value);
    }

    [DisplayName("Blocked File Types")]
    [Description("Semicolon-separated list of file extensions which may not be sent via Teleport.  " +
        "If unset or empty, all files may be sent.  If non-empty, files with extensions " +
        "in this list may not be sent.")]
    public string? BlockedFileTypes
    {
        get => GetString(nameof(BlockedFileTypes),
            "_exe;a6p;ac;acr;action;air;apk;app;applescript;awk;bas;bat;bin;cgi;chm;cmd;" +
            "com;cpl;crt;csh;dek;dld;dll;dmg;drv;ds;ebm;elf;emf;esh;exe;ezs;fky;frs;fxp;" +
            "gadget;gpe;gpu;hlp;hms;hta;icd;iim;inf;ins;inx;ipa;ipf;isp;isu;jar;js;jse;" +
            "jsp;jsx;kix;ksh;lib;lnk;mcr;mel;mem;mpkg;mpx;mrc;ms;msc;msi;msp;mst;mxe;obs;" +
            "ocx;pas;pcd;pex;pif;pkg;pl;plsc;pm;prc;prg;pvd;pwc;py;pyc;pyo;qpx;rbx;reg;" +
            "rgs;rox;rpj;scar;scpt;scr;script;sct;seed;sh;shb;shs;spr;sys;thm;tlb;tms;u3p;" +
            "udf;url;vb;vbe;vbs;vbscript;vdo;vxd;wcm;widget;wmf;workflow;wpk;ws;wsc;wsf;" +
            "wsh;xap;xqt;zlq");
        set => SetString(nameof(BlockedFileTypes), value);
    }

    [DisplayName("Permitted Url Schemes")]
    [Description("Semicolon-separated list of url schemes which may be sent via Teleport.  " +
        "If unset or empty, all urls may be sent.  If non-empty, only urls with schemes " +
        "in this list may be sent.  Note: Blocked Url Schemes takes precedence over permitted.")]
    public string? PermittedUrlSchemes
    {
        get => GetString(nameof(PermittedUrlSchemes), null);
        set => SetString(nameof(PermittedUrlSchemes), value);
    }

    [DisplayName("Blocked URL Schemes")]
    [Description("Semicolon-separated list of URL schemes which may not be sent via Teleport.  " +
        "If unset or empty, all URLs may be sent.  If non-empty, URLs with schemes " +
        "in this list may not be sent.")]
    public string? BlockedUrlSchemes
    {
        get => GetString(nameof(BlockedUrlSchemes), null);
        set => SetString(nameof(BlockedUrlSchemes), value);
    }

    [DisplayName("Max Read Interval")]
    [Description("Maximum time between reads from a streamed file.  " +
        "If no read occurs within this interval, the file will be closed.")]
    public TimeSpan MaxReadInterval
    {
        get => GetTimeSpan(nameof(MaxReadInterval), TimeSpan.FromMinutes(1));
        set => SetTimeSpan(nameof(MaxReadInterval), value);
    }

    [DisplayName("Max Read Time")]
    [Description("Maximum time a streamed file may be open.  If the file " +
        "is not closed within this interval, the teleport process will exit.")]
    public TimeSpan MaxReadTime
    {
        get => GetTimeSpan(nameof(MaxReadTime), TimeSpan.FromMinutes(30));
        set => SetTimeSpan(nameof(MaxReadTime), value);
    }

    [DisplayName("Recursion Limit")]
    [Description("Number of times teleport can be invoked from an application invoked by " +
        "teleport.  Used to avoid loops.  When the recursion limit is hit, the open-with " +
        "dialog will be used for the user to pick an application other than teleport")]
    public int RecursionLimit
    {
        get => GetInt(nameof(RecursionLimit), 0);
        set => SetInt(nameof(RecursionLimit), value);
    }

    [DisplayName("Prompt for Save File")]
    [Description("If set, the user will be prompted to save the file rather than " +
        "the file being saved to the default location.")]
    public bool PromptForSaveFile
    {
        get => GetBool(nameof(PromptForSaveFile), false);
        set => SetBool(nameof(PromptForSaveFile), value);
    }

    [DisplayName("Default Save Directory")]
    [Description("Windows FOLDERID Guid used as the base path for DefaultSaveDirectory.  " +
        "See https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid for a " +
        "list of all FolderIDs.  Default is the downloads folder.")]
    public Guid DefaultSaveDirectoryFolderID
    {
        get => GetGuid(nameof(DefaultSaveDirectoryFolderID), "{9AB2FF5A-7703-4887-A8F9-E521D39D3B76}");
        set => SetGuid(nameof(DefaultSaveDirectoryFolderID), value);
    }

    [DisplayName("Default Save Path")]
    [Description("Relative or absolute path where files should be saved.  Relative " +
        "paths are relative to DefaultSaveDirectoryFolderID")]
    public string? DefaultSaveDirectory
    {
        get => GetString(nameof(DefaultSaveDirectory), null);
        set => SetString(nameof(DefaultSaveDirectory), value);
    }

    [DisplayName("Set MOTW on Saved Files")]
    [Description("If set, the saved file will have the Mark of the Web set, " +
        "which will cause Windows to prompt before opening the file.  The " +
        "exact behavior differs per application, but Microsoft Office will open " +
        "files in Protected Mode when the MOTW is present. See " +
        "https://learn.microsoft.com/hu-hu/DeployOffice/security/internet-macros-blocked " +
        "for more details about the Mark of the Web.")]
    public bool SetMotwOnSavedFiles
    {
        get => GetBool(nameof(SetMotwOnSavedFiles), true);
        set => SetBool(nameof(SetMotwOnSavedFiles), value);
    }
}