using System.Diagnostics;

namespace Scout.Scribe;

// The recording panel owns the tray and session for the application's lifetime.
internal sealed class ExperimentForm : Form
{
    private readonly Button record = ScribeTheme.Button("Start recording", primary: true);
    private readonly Button settings = ScribeTheme.Button("Settings");
    private readonly Button open = ScribeTheme.Button("Folder");
    private readonly Label state = ScribeTheme.Label("Ready to record", 16, true);
    private readonly Label duration = ScribeTheme.Label("00:00:00", 36);
    private readonly Label summary = ScribeTheme.Label("Microphone + system audio");
    private readonly TextBox details = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical, BackColor = ScribeTheme.Background, ForeColor = ScribeTheme.Foreground,
        BorderStyle = BorderStyle.None };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 500 };
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem trayStart = new("Start recording");
    private readonly ToolStripMenuItem trayStop = new("Stop recording") { Enabled = false };
    private readonly TrayStatus tray;
    private readonly Stopwatch elapsed = new();
    private readonly Func<string, CaptureSession> createSession;
    private ScribePreferences preferences;
    private CaptureSession? session;
    private string? lastFolder;
    private bool busy;
    private bool closingAfterStop;
    private bool exitRequested;
    private bool statusRequested;
    private bool settingsFailed;

    public ExperimentForm(bool showTrayIcon = true, Func<string, CaptureSession>? createSession = null)
    {
        this.createSession = createSession ?? (path => new CaptureSession(path));
        ScribeTheme.Apply(this);
        Text = "Scout.Scribe";
        ClientSize = new(400, 356);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        KeyPreview = true;
        open.AccessibleName = "Open recording folder";
        preferences = new(ScribePreferences.DefaultRecordingPath);
        try { preferences = ScribePreferences.Load(); }
        catch (Exception error)
        {
            settingsFailed = true;
            state.Text = "Check settings";
            summary.Text = "Your saved folder could not be loaded. Save Settings before recording.";
            Append($"Settings could not be loaded. {error.Message}");
        }

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(24), ColumnCount = 1, RowCount = 6 };
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.Absolute, 38));
        layout.RowStyles.Add(new(SizeType.Absolute, 45));
        layout.RowStyles.Add(new(SizeType.Absolute, 76));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.Absolute, 48));
        layout.RowStyles.Add(new(SizeType.Absolute, 48));
        var heading = ScribeTheme.Label("SCRIBE", 10, true); heading.ForeColor = ScribeTheme.Muted;
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(state, 0, 1);
        duration.Margin = Padding.Empty;
        layout.Controls.Add(duration, 0, 2);
        summary.ForeColor = ScribeTheme.Muted;
        summary.AutoSize = false; summary.Dock = DockStyle.Fill;
        layout.Controls.Add(summary, 0, 3);
        record.Dock = DockStyle.Fill; record.Margin = new(0, 0, 0, 8);
        layout.Controls.Add(record, 0, 4);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Margin = Padding.Empty };
        for (var i = 0; i < 3; i++) actions.ColumnStyles.Add(new(SizeType.Percent, 100f / 3));
        var diagnostics = ScribeTheme.Button("Details");
        foreach (var button in new[] { settings, open, diagnostics }) button.Dock = DockStyle.Fill;
        diagnostics.Margin = Padding.Empty;
        actions.Controls.Add(settings, 0, 0); actions.Controls.Add(open, 1, 0); actions.Controls.Add(diagnostics, 2, 0);
        layout.Controls.Add(actions, 0, 5);
        Controls.Add(layout);

        trayMenu.Items.AddRange([trayStart, trayStop, new ToolStripSeparator(),
            new ToolStripMenuItem("Show recording", null, (_, _) => ShowStatus()),
            new ToolStripMenuItem("Settings…", null, (_, _) => ShowSettings()),
            new ToolStripMenuItem("Open output folder", null, (_, _) => OpenOutput()),
            new ToolStripSeparator(), new ToolStripMenuItem("Exit", null, (_, _) => RequestExit())]);
        tray = new TrayStatus(trayMenu, showTrayIcon);
        record.Enabled = trayStart.Enabled = !settingsFailed;
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowStatus(); };
        trayStart.Click += async (_, _) => await StartSession();
        trayStop.Click += (_, _) => RequestStop();
        record.Click += async (_, _) => { if (session is null) await StartSession(); else RequestStop(); };
        settings.Click += (_, _) => ShowSettings();
        open.Click += (_, _) => OpenOutput();
        diagnostics.Click += (_, _) => ShowDetails();
        timer.Tick += (_, _) =>
        {
            duration.Text = FormatDuration(elapsed.Elapsed);
            UpdateTray(true, $"Scout.Scribe — recording {duration.Text}");
        };
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; } };
        FormClosing += (_, e) =>
        {
            if (!exitRequested) { e.Cancel = true; Hide(); return; }
            if (session is null) return;
            e.Cancel = true;
            if (closingAfterStop) return;
            if (MessageBox.Show(this, "Stop and finalize this recording before exiting?", "Scribe recording", MessageBoxButtons.YesNo) == DialogResult.Yes)
            { closingAfterStop = true; RequestStop(); }
            else exitRequested = false;
        };
        // Receive single-instance messages and capture callbacks even before first showing.
        _ = Handle;
    }

    protected override void SetVisibleCore(bool value) => base.SetVisibleCore(value && statusRequested);
    internal static string FormatDuration(TimeSpan value) => $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";

    internal async Task StartSession()
    {
        if (busy || session is not null || settingsFailed) return;
        busy = true; record.Enabled = false;
        elapsed.Reset(); duration.Text = "00:00:00";
        state.Text = "Starting…"; summary.Text = "Opening microphone and system audio";
        UpdateTray(false, "Scout.Scribe — starting");
        try
        {
            var current = createSession(preferences.RecordingPath);
            session = current;
            lastFolder = current.DirectoryPath;
            Append($"Session: {lastFolder}\r\nPID: {Environment.ProcessId}");
            current.Status += message => Post(() => Append(message));
            try
            {
                await current.Started;
                elapsed.Start();
                if (!closingAfterStop)
                {
                    busy = false; record.Enabled = true; record.Text = "Stop recording";
                    record.BackColor = Color.FromArgb(241, 112, 112);
                    state.Text = "● Recording"; state.ForeColor = Color.FromArgb(255, 140, 140);
                    summary.Text = "Microphone + system audio";
                    timer.Start();
                    UpdateTray(true, "Scout.Scribe — recording");
                }
            }
            catch (Exception error) { Append(error.Message); }
            var result = await current.Completion;
            state.Text = result.Completed ? "Recording saved" : "Recording failed";
            summary.Text = result.Completed ? "Your audio is saved. Open folder to find it." : "Output may be partial. See Details for the error.";
            if (result.Error is not null) Append(result.Error);
            if (!result.Completed && !closingAfterStop) ShowStatus();
        }
        catch (Exception error)
        {
            state.Text = "Could not start";
            summary.Text = "See Details for the error, then try again.";
            Append(error.ToString());
            if (!closingAfterStop) ShowStatus();
        }
        finally
        {
            timer.Stop(); elapsed.Stop(); duration.Text = FormatDuration(elapsed.Elapsed);
            busy = false; record.Enabled = true; record.Text = "Start recording";
            record.BackColor = ScribeTheme.Accent; state.ForeColor = ScribeTheme.Foreground;
            session = null;
            UpdateTray(false, "Scout.Scribe — not recording");
            if (closingAfterStop) Close();
        }
    }

    internal void RequestStop()
    {
        if (session is null) return;
        timer.Stop(); elapsed.Stop(); busy = true; record.Enabled = false;
        state.Text = "Saving recording…"; summary.Text = "Finishing audio files. Please wait.";
        UpdateTray(false, "Scout.Scribe — finalizing");
        session.Stop();
    }

    private void ShowSettings()
    {
        // Modal settings prevent a new session being started while editing preferences.
        Hide();
        using var dialog = new SettingsForm(preferences, busy || session is not null);
        trayMenu.Enabled = false;
        try
        {
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                preferences = dialog.Preferences;
                if (settingsFailed)
                {
                    settingsFailed = false;
                    state.Text = "Ready to record"; summary.Text = "Microphone + system audio";
                    record.Enabled = trayStart.Enabled = true;
                }
            }
        }
        finally { trayMenu.Enabled = true; }
    }

    private void ShowDetails()
    {
        using var dialog = new Form { Text = "Scout.Scribe · Recording details", ClientSize = new(720, 400),
            Padding = new(20), StartPosition = FormStartPosition.CenterScreen };
        ScribeTheme.Apply(dialog);
        dialog.Controls.Add(details);
        try { dialog.ShowDialog(this); }
        finally { dialog.Controls.Remove(details); }
    }

    public void HandleFailure(Exception error)
    {
        Append(error.ToString());
        closingAfterStop = true;
        exitRequested = true;
        if (session is null) Close(); else RequestStop();
    }
    private void Post(Action action) { if (!IsDisposed && IsHandleCreated) BeginInvoke(action); }
    private void Append(string message) => details.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
    private void UpdateTray(bool recording, string text)
    {
        tray.SetRecording(recording, text);
        trayStart.Enabled = !busy && session is null && !settingsFailed;
        trayStop.Enabled = !busy && session is not null;
    }
    internal void ShowStatus()
    {
        if (Modal || OwnedForms.Any(form => form.Visible)) return;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new(Math.Clamp(Cursor.Position.X - Width / 2, area.Left, Math.Max(area.Left, area.Right - Width)),
            Math.Clamp(Cursor.Position.Y - Height - 12, area.Top, Math.Max(area.Top, area.Bottom - Height)));
        statusRequested = true;
        Show(); WindowState = FormWindowState.Normal; Activate();
    }
    internal void RequestExit() { exitRequested = true; Close(); }
    private void OpenOutput()
    {
        try
        {
            var path = lastFolder ?? preferences.RecordingPath;
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Could not open recording folder", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    internal void RemoveTrayIcon() => tray.Remove();
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == InstanceSignal.ShowMessage) ShowStatus();
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); tray.Dispose(); trayMenu.Dispose(); details.Dispose(); }
        base.Dispose(disposing);
    }
}
