using System.Runtime.InteropServices;

namespace Scout.Scribe;

internal static class ScribeTheme
{
    internal static readonly Color Background = Color.FromArgb(30, 32, 36);
    internal static readonly Color Surface = Color.FromArgb(43, 46, 51);
    internal static readonly Color Foreground = Color.FromArgb(245, 246, 248);
    internal static readonly Color Muted = Color.FromArgb(174, 178, 186);
    internal static readonly Color Accent = Color.FromArgb(72, 190, 240);

    internal static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Foreground;
        form.Font = new Font("Segoe UI", 10);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.HandleCreated += (_, _) =>
        {
            var enabled = 1;
            DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int));
        };
    }

    internal static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = false, Height = 42, Width = 120,
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Accent : Color.FromArgb(57, 60, 66),
            ForeColor = primary ? Color.FromArgb(15, 30, 40) : Foreground,
            Cursor = Cursors.Hand, UseVisualStyleBackColor = false,
            Margin = new Padding(0, 0, 8, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    internal static Label Label(string text, float size = 10, bool bold = false) => new()
    {
        Text = text, AutoSize = true, ForeColor = Foreground,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(0, 0, 0, 12)
    };

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
