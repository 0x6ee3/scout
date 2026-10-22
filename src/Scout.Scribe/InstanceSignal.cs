using System.Runtime.InteropServices;

namespace Scout.Scribe;

internal static class InstanceSignal
{
    internal static readonly uint ShowMessage = RegisterWindowMessage("Scout.Scribe.ShowStatus.v1");
    internal static void ShowExisting() => PostMessage((nint)0xffff, ShowMessage, 0, 0);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
