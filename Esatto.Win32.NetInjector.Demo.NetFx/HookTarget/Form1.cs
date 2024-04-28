using System.Runtime.InteropServices;

namespace HookTarget
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);

            RunAssembly(null, "HookExample.dll", "HookExample.Class1", "Inject", "Hello world");
        }

        [DllImport("Esatto.Win32.NetInjector.Netfx.dll", PreserveSig = false, ExactSpelling = true)]
        public static extern void RunAssembly(
            [MarshalAs(UnmanagedType.LPWStr)] string? pwzPreferredVersion,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzAssemblyPath,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzTypeName,
            [MarshalAs(UnmanagedType.LPWStr)] string pwzMethodName,
            [MarshalAs(UnmanagedType.LPWStr)] string? pwzArgument
        );
    }
}