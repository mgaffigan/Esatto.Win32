using System.Runtime.InteropServices;

namespace HookExample;

public class SampleHook
{
    public static int Inject(nint pArg, int cbArg)
    {
        var arg = Marshal.PtrToStringUni(pArg);
        MessageBox.Show(arg);
        return 0;
    }
}