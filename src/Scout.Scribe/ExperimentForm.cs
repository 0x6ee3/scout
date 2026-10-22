using System.Diagnostics;

namespace Scout.Scribe;

// The experiment window also serves as the tray's settings/diagnostics window.
internal sealed class ExperimentForm : Form
{
    private readonly TextBox output = new() { Dock = DockStyle.Fill,
        Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Scout.Scribe", "recordings") };
    private readonly Button browse = new() { Text = "Output folder…", AutoSize = true };
    private readonly Button start = new() { Text = "Start recording", AutoSize = true };
    private readonly Button stop = new() { Text = "Stop recording", AutoSize = true, Enabled = false };
    private readonly Button open = new() { Text = "Open session folder", AutoSize = true, Enabled = false };
    private readonly Label state = new() { Text = "Idle — not recording", AutoSize = true };
    private readonly TextBox details = new() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill,
        ScrollBars = ScrollBars.Vertical };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private readonly ContextMenuStrip trayMenu = new();
    private readonly ToolStripMenuItem trayStart = new("Start recording");
    private readonly ToolStripMenuItem trayStop = new("Stop recording") { Enabled = false };
    private readonly TrayStatus tray;
    private CaptureSession? session;
    private DateTimeOffset started;
    private string? lastFolder;
    private bool closingAfterStop;
    private bool exitRequested;

    public ExperimentForm(bool showTrayIcon = true)
    {
        Text = "Scout.Scribe — capture experiment (not recording)";
        Width = 760; Height = 460; MinimumSize = new(640, 400);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(16), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Windows default microphone + application audio before listening volume (all outputs).\nSeparate microphone.mp3 / playback.mp3: mono, 48 kHz, 256 kbps; five-second checkpoints.\nMicrophone refreshes each Start; playback excludes Scribe and its child processes.", AutoSize = true });
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2 };
        pathRow.ColumnStyles.Add(new(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new(SizeType.AutoSize));
        pathRow.Controls.Add(output); pathRow.Controls.Add(browse);
        layout.Controls.Add(pathRow);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        buttons.Controls.AddRange([start, stop, open]);
        layout.Controls.Add(buttons); layout.Controls.Add(state); layout.Controls.Add(details);
        Controls.Add(layout);
        trayMenu.Items.AddRange([trayStart, trayStop, new ToolStripSeparator(),
            new ToolStripMenuItem("Show settings / status", null, (_, _) => ShowStatus()),
            new ToolStripMenuItem("Open output folder", null, (_, _) => OpenOutput()),
            new ToolStripSeparator(), new ToolStripMenuItem("Exit", null, (_, _) => RequestExit())]);
        tray = new TrayStatus(trayMenu, showTrayIcon);
        tray.DoubleClick += (_, _) => ShowStatus();
        trayStart.Click += async (_, _) => await StartSession();
        trayStop.Click += (_, _) => RequestStop();
        browse.Click += (_, _) => { using var picker = new FolderBrowserDialog { InitialDirectory = output.Text };
            if (picker.ShowDialog(this) == DialogResult.OK) output.Text = picker.SelectedPath; };
        start.Click += async (_, _) => await StartSession();
        stop.Click += (_, _) => RequestStop();
        open.Click += (_, _) => { if (lastFolder is not null) Process.Start(new ProcessStartInfo(lastFolder) { UseShellExecute = true }); };
        timer.Tick += (_, _) =>
        {
            state.Text = $"RECORDING — {DateTimeOffset.UtcNow - started:hh\\:mm\\:ss} — PID {Environment.ProcessId}";
            UpdateTray(true, $"Scout.Scribe — recording {DateTimeOffset.UtcNow - started:hh\\:mm\\:ss}");
        };
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
    }

    private async Task StartSession()
    {
        start.Enabled = browse.Enabled = output.Enabled = false;
        state.Text = "Starting — not recording yet";
        UpdateTray(false, "Scout.Scribe — starting");
        try
        {
            var current = new CaptureSession(output.Text);
            session = current;
            lastFolder = current.DirectoryPath;
            open.Enabled = true;
            Append($"Session: {lastFolder}\r\nPID: {Environment.ProcessId}");
            current.Status += message => Post(() => Append(message));
            try
            {
                await current.Started;
                started = DateTimeOffset.UtcNow;
                stop.Enabled = true;
                state.Text = "RECORDING";
                Text = "Scout.Scribe — RECORDING";
                timer.Start();
                UpdateTray(true, "Scout.Scribe — recording");
            }
            catch (Exception error) { Append(error.Message); }
            var result = await current.Completion;
            state.Text = result.Completed ? "Completed — not recording" : "FAILED — not recording; retained output may be partial";
            if (result.Error is not null) Append(result.Error);
        }
        catch (Exception error) { state.Text = "Failed to start — not recording"; Append(error.ToString()); }
        finally
        {
            timer.Stop(); stop.Enabled = false; start.Enabled = browse.Enabled = output.Enabled = true;
            Text = "Scout.Scribe — capture experiment (not recording)";
            session = null;
            UpdateTray(false, "Scout.Scribe — not recording");
            if (closingAfterStop) Close();
        }
    }

    private void RequestStop()
    {
        timer.Stop(); stop.Enabled = false;
        state.Text = "Finalizing — capture stopping";
        Text = "Scout.Scribe — finalizing";
        UpdateTray(false, "Scout.Scribe — finalizing");
        session?.Stop();
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
        trayStart.Enabled = start.Enabled;
        trayStop.Enabled = stop.Enabled;
    }
    private void ShowStatus() { Show(); WindowState = FormWindowState.Normal; Activate(); }
    internal void RequestExit() { exitRequested = true; Close(); }
    private void OpenOutput()
    {
        var path = lastFolder ?? Path.GetFullPath(output.Text);
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    internal void RemoveTrayIcon() => tray.Remove();
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == InstanceSignal.ShowMessage) ShowStatus();
        base.WndProc(ref m);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); tray.Dispose(); trayMenu.Dispose(); }
        base.Dispose(disposing);
    }
}
