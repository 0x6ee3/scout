namespace Scout.Scribe;

internal sealed class SettingsForm : Form
{
    private readonly TextBox recordingPath;
    private readonly CheckBox login;
    private readonly bool initialLogin;
    internal ScribePreferences Preferences { get; private set; }

    internal SettingsForm(ScribePreferences preferences, bool recording)
    {
        Preferences = preferences;
        ScribeTheme.Apply(this);
        Text = "Scout.Scribe · Settings";
        ClientSize = new(640, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new(24), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(ScribeTheme.Label("General", 22, true), 0, 0);

        var startup = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
            BackColor = ScribeTheme.Surface, Padding = new(16), Margin = new(0, 4, 0, 16), WrapContents = false };
        login = new CheckBox { Text = "Launch at sign-in", AutoSize = true, Margin = new(0, 0, 0, 6) };
        startup.Controls.Add(login);
        var loginHelp = ScribeTheme.Label("Start quietly in the tray when you sign in to Windows.");
        loginHelp.ForeColor = ScribeTheme.Muted;
        loginHelp.MaximumSize = new(520, 0);
        try { initialLogin = login.Checked = LoginStartup.Enabled; }
        catch (Exception error) when (error is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            login.Enabled = false;
            loginHelp.Text = "Launch at sign-in is unavailable. Windows denied access to startup settings.";
        }
        startup.Controls.Add(loginHelp);
        layout.Controls.Add(startup, 0, 1);

        var folder = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
            Padding = new(16), BackColor = ScribeTheme.Surface, Margin = Padding.Empty };
        folder.ColumnStyles.Add(new(SizeType.Percent, 100));
        folder.ColumnStyles.Add(new(SizeType.Absolute, 112));
        var title = ScribeTheme.Label("Recording folder", 12, true);
        folder.Controls.Add(title, 0, 0); folder.SetColumnSpan(title, 2);
        var description = ScribeTheme.Label(recording ? "Stop recording to change the folder." : "New sessions are saved in their own folder here.");
        description.ForeColor = ScribeTheme.Muted;
        folder.Controls.Add(description, 0, 1); folder.SetColumnSpan(description, 2);
        recordingPath = new TextBox { Text = preferences.RecordingPath, Dock = DockStyle.Fill,
            BackColor = ScribeTheme.Background, ForeColor = ScribeTheme.Foreground, BorderStyle = BorderStyle.FixedSingle,
            Enabled = !recording, AccessibleName = "Recording folder", Margin = new(0, 8, 10, 0) };
        var browse = ScribeTheme.Button("Browse…");
        browse.Enabled = !recording; browse.Dock = DockStyle.Fill;
        browse.Click += (_, _) =>
        {
            using var picker = new FolderBrowserDialog { InitialDirectory = recordingPath.Text };
            if (picker.ShowDialog(this) == DialogResult.OK) recordingPath.Text = picker.SelectedPath;
        };
        folder.Controls.Add(recordingPath, 0, 2); folder.Controls.Add(browse, 1, 2);
        layout.Controls.Add(folder, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Margin = Padding.Empty };
        var save = ScribeTheme.Button("Save", primary: true);
        var cancel = ScribeTheme.Button("Cancel"); cancel.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) => Save();
        actions.Controls.AddRange([save, cancel]); layout.Controls.Add(actions, 0, 4);
        Controls.Add(layout); AcceptButton = save; CancelButton = cancel;
        ActiveControl = save;
    }

    private void Save()
    {
        try
        {
            var next = new ScribePreferences(ScribePreferences.ValidatePath(recordingPath.Text));
            var loginChanged = login.Enabled && initialLogin != login.Checked;
            if (loginChanged) LoginStartup.SetEnabled(login.Checked);
            try { next.Save(); }
            catch
            {
                if (loginChanged) LoginStartup.SetEnabled(initialLogin);
                throw;
            }
            Preferences = next;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception error) { MessageBox.Show(this, error.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
