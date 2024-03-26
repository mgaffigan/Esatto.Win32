namespace HookExample;

public class SampleHook
{
    public static int Inject(string arg)
    {
        MessageBox.Show(arg);
        return 0;
    }
}