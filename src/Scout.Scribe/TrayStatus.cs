using System.Runtime.InteropServices;

namespace Scout.Scribe;

internal sealed class TrayStatus : IDisposable
{
    private readonly Icon idle = MakeIcon(recording: false);
    private readonly Icon recording = MakeIcon(recording: true);
    private readonly NotifyIcon icon;
    public TrayStatus(ContextMenuStrip menu, bool visible = true)
    {
        icon = new NotifyIcon { Icon = idle, Text = "Scout.Scribe — not recording",
            ContextMenuStrip = menu, Visible = visible };
    }
    public event MouseEventHandler? MouseClick { add => icon.MouseClick += value; remove => icon.MouseClick -= value; }
    public void SetRecording(bool active, string text)
    {
        icon.Icon = active ? recording : idle;
        icon.Text = text;
    }
    public void Remove() { icon.Visible = false; }
    public void Dispose()
    {
        icon.Visible = false;
        icon.Dispose(); idle.Dispose(); recording.Dispose();
    }
    private static Icon MakeIcon(bool recording)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(recording ? Color.Red : Color.DimGray);
        if (recording) graphics.FillEllipse(brush, 3, 3, 26, 26);
        else
        {
            graphics.FillRectangle(brush, 4, 4, 24, 24);
            graphics.FillRectangle(Brushes.White, 11, 10, 3, 12);
            graphics.FillRectangle(Brushes.White, 18, 10, 3, 12);
        }
        var handle = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(handle); return (Icon)borrowed.Clone(); }
        finally { DestroyIcon(handle); }
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
