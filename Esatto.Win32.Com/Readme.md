# Esatto Win32 COM

## Example out-of-process server

    [ComVisible(true)]
    public interface IShellApplication
    {
        void Navigate(string destination);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId(SessionApplication.ShellApplicationProgId)]
    public sealed class ShellApplication : StandardOleMarshalObject, IShellApplication, IDisposable
    {
        private readonly INavigationManager NavMgr;
        private readonly ClassObjectRegistration ThisReg;

        public ShellApplication(INavigationManager navMgr)
        {
            this.NavMgr = navMgr;
            this.ThisReg = new ClassObjectRegistration(typeof(ShellApplication).GUID,
                ComInterop.CreateClassFactoryFor(() => this), CLSCTX.LOCAL_SERVER, REGCLS.MULTIPLEUSE | REGCLS.SUSPENDED);
            ComInterop.CoResumeClassObjects();
        }

        public void Dispose()
        {
            this.ThisReg.Dispose();
        }

        public void Navigate(string destination) 
        {
            NavMgr.Navigate(destination);
        }

        #region Registration

        [Obsolete("Implemented only to support regasm, use .ctor(INavigationManager)")]
        public ShellApplication()
        {
            // required for regasm
            throw new InvalidOperationException();
        }

        [ComRegisterFunction]
        internal static void RegasmRegisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring("HKEY_CLASSES_ROOT\\".Length);
            using (var keyCLSID = Registry.ClassesRoot.OpenSubKey(path, writable: true)!)
            {
                // Remove the auto-generated InprocServer32 key after registration
                // (REGASM puts it there but we are going out-of-proc).
                keyCLSID.DeleteSubKeyTree("InprocServer32");
                using (var ls32 = keyCLSID.CreateSubKey("LocalServer32"))
                {
                    ls32.SetValue(null, Assembly.GetExecutingAssembly().Location);
                }
            }
        }

        [ComUnregisterFunction]
        internal static void RegasmUnregisterLocalServer(string path)
        {
            // path is HKEY_CLASSES_ROOT\\CLSID\\{clsid}", we only want CLSID...
            path = path.Substring("HKEY_CLASSES_ROOT\\".Length);
            Registry.ClassesRoot.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
        }

        #endregion Registration
    }

Register with `regasm` or [DSCOM](https://github.com/dspace-group/dscom)

## Example out-of-process client

    SessionApplication.Navigate("Example");

    public static class SessionApplication
    {
        public const string ShellApplicationProgId = "Example.Shell.Application.1";

        public static IShellApplication GetOrOpenShell()
        {
            var app = (IShellApplication)ComInterop.CreateLocalServer(ShellApplicationProgId);
            try
            {
                ComInterop.CoAllowSetForegroundWindow(app);
            }
            catch
            {
                // Nop, we may not have foreground
            }
            return app;
        }
    }

## Run with impersonation

    ComInterop.RunImpersonated(() =>
    {
        var ident = WindowsIdentity.GetCurrent(true);
        Console.WriteLine(ident.User);
    });