# Esatto Win32 Common Controls

## Save Printer Settings

The entirety of the `PrinterSettings` &ndash; including vendor specific options &ndash; can be stored by persisting the `HDEVMODE` structure received via `PrinterSettings.GetHdevmode()`, then restored via `PrinterSettings.SetHdevmode(IntPtr)`.

The class below will add two extension methods to save and restore `PrinterSettings` to and from a `byte` array.  

*Caveat programmer:* some printer drivers do not have backwards or forwards compatibility, and may crash if using persisted data from another version or architecture of the driver.

Example Use:

    PrinterSettings CurrentSettings;
    const string myAppKeyName = @"Software\MyApplicationName";
    const string printerSettingValueName = "PrinterSettings"

    // save
    using (var sk = Registry.CurrentUser.CreateSubKey(myAppKeyName))
    {
        sk.SetValue(printerSettingValueName, this.CurrentSettings.GetDevModeData(), RegistryValueKind.Binary);
    }

    // restore
    using (var sk = Registry.CurrentUser.CreateSubKey(myAppKeyName))
    {
        var data = sk.GetValue(printerSettingValueName, RegistryValueKind.Binary) as byte[];

        this.CurrentSettings = new PrinterSettings();
        if (data != null)
        {
            this.CurrentSettings.SetDevModeData(data);
        }
    }

Related: [How can I save and restore `PrinterSettings`?](https://stackoverflow.com/questions/28007554/how-can-i-save-and-restore-printersettings/28007555#28007555)

## Non-closable window

Creates a WPF Window that has no "x" button.

    <win32:NonClosableWindow x:Class="Example Window"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:win32="clr-namespace:Esatto.Win32.Wpf;assembly=Esatto.Win32.CommonControls"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" mc:Ignorable="d"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
        CanClose="{Binding Path=CanClose}"
        Title="Task Progress" ResizeMode="NoResize" SizeToContent="WidthAndHeight">
        <Grid>
        </Grid>
    </win32:NonClosableWindow>

## Watch for windows created from a specific process

    this.WindowCreatedHook = new WinEventHook(this.FrameworkProcess, null, WinEvent.EVENT_OBJECT_SHOW, WinEvent.EVENT_OBJECT_HIDE, syncCtx: Sync);
    this.WindowCreatedHook
        .Where(c => c.WinObject == WinObject.OBJID_WINDOW && c.WinChild == WinChild.CHILDID_SELF)
        .Subscribe(Hook_EventReceived);
        
    private void Hook_EventReceived(WinEventEventArgs args)
    {
        Console.WriteLine(args);
    }

## Create a "docked" window to some other process

    var DockTarget = new Win32Window(0x12345678 /* Target HWND */)
    this.LocationMonitor = new WindowLocationMonitor(DockTarget, DockTarget.GetParent());
    this.LocationMonitor.WindowMoved += (_, e) => this.Location = Point.Add(e.WindowRect.Location.ToGdi(), DockTargetPositionOffset);
    this.LocationMonitor.DragBegin += (_, _) => this.Hide();
    this.LocationMonitor.DragEnd += (_, _) => this.Show();
    this.LocationMonitor.Exception += (_, e) => Console.WriteLine("Exception on window location monitor: {0}", e.ExceptionObject);
    this.Show();

## Show Excel, Word, and other shell Preview handlers as a control

Add `ShellPreviewControl` to a Windows Form or WPF Window.  Set `DisplayedPath` to an absolute path.

## Create an "Open With" menu

    var assocs = AssociatedHandler.ForPath(tempPath);
    var cxm = new ContextMenu(
        assocs.Select(at => new MenuItem(at.DisplayName, (_1, _2) => at.Invoke())).ToArray());
    cxm.Show(this, this.PointToClient(MousePosition));

## Find and manipulate HWNDs

    var foregroundCaption = Win32Window.GetForegrundWindow().CachedName;
    var window = Win32Window.Find(process, w => w.CachedClass == "Example"" && window.GetIsShown());
    var child = window.FindChild(ww => ww.CachedClass == FrameworkConstants.MdiClientClass);
    child.Show();