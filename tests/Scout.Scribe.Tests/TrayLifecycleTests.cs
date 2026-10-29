using System.Runtime.ExceptionServices;

namespace Scout.Scribe.Tests;

public sealed class TrayLifecycleTests
{
    [Fact]
    public void ClosingWindowKeepsApplicationAliveAndExplicitExitDisposesIt()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                // Exercise the real FormClosing and disposal paths on an STA thread,
                // without visible windows, tray notifications, or capture devices.
                using var form = new ExperimentForm(showTrayIcon: false) { ShowInTaskbar = false, Opacity = 0 };
                form.Show();
                Application.DoEvents();
                Assert.False(form.Visible);
                Assert.True(form.IsHandleCreated);
                form.ShowStatus();
                Assert.True(form.Visible);
                form.Close();
                Assert.False(form.Visible);
                Assert.False(form.IsDisposed);
                form.RequestExit();
                Assert.True(form.IsDisposed);
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Tray lifecycle test did not finish.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Fact]
    public void MessageLoopStartsHiddenAndExitsWithoutEverOpeningPanel()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var form = new ExperimentForm(showTrayIcon: false);
                form.BeginInvoke(() =>
                {
                    try { Assert.False(form.Visible); Assert.False(form.ShowInTaskbar); }
                    catch (Exception error) { failure = error; }
                    finally { form.RequestExit(); }
                });
                Application.Run(form);
                Assert.True(form.IsDisposed);
            }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Hidden application did not exit.");
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    [Theory]
    [InlineData(0, "00:00:00")]
    [InlineData(3661, "01:01:01")]
    [InlineData(90061, "25:01:01")]
    public void DurationDoesNotWrapAtTwentyFourHours(int seconds, string expected) =>
        Assert.Equal(expected, ExperimentForm.FormatDuration(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void RecordingPanelSurvivesHidingAndFinalizesSyntheticSession()
    {
        Exception? failure = null;
        var root = Path.Combine(Path.GetTempPath(), "scribe-panel-" + Guid.NewGuid().ToString("N"));
        var thread = new Thread(() =>
        {
            try
            {
                var sessions = 0;
                using var form = new ExperimentForm(showTrayIcon: false, createSession: _ =>
                {
                    sessions++;
                    return new CaptureSession(root, synthetic: true);
                }) { Opacity = 0 };
                form.BeginInvoke(async () =>
                {
                    Task? completion = null;
                    try
                    {
                        form.ShowStatus();
                        completion = form.StartSession();
                        var deadline = DateTime.UtcNow.AddSeconds(10);
                        while (!Descendants(form).OfType<Button>().Any(button => button.Text == "Stop recording"))
                        {
                            if (completion.IsCompleted) await completion;
                            Assert.True(DateTime.UtcNow < deadline, "Synthetic recording did not start.");
                            await Task.Delay(25);
                        }
                        await form.StartSession();
                        Assert.Equal(1, sessions);
                        form.Close();
                        Assert.False(form.Visible);
                        Assert.False(form.IsDisposed);
                        form.ShowStatus();
                        Assert.True(form.Visible);
                        form.RequestStop();
                        Assert.Contains(Descendants(form).OfType<Label>(), label => label.Text == "Saving recording…");
                        await completion;
                        Assert.Contains(Descendants(form).OfType<Label>(), label => label.Text == "Recording saved");
                        Assert.Contains(Descendants(form).OfType<Button>(), button => button.Text == "Start recording" && button.Enabled);
                        Assert.Equal(2, Directory.GetFiles(root, "*.mp3", SearchOption.AllDirectories).Length);
                    }
                    catch (Exception error) { failure = error; }
                    finally
                    {
                        form.RequestStop();
                        if (completion is not null) await completion;
                        form.RequestExit();
                    }
                });
                Application.Run(form);
            }
            catch (Exception error) { failure = error; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        try
        {
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Synthetic UI lifecycle did not finish.");
            if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
        }
        finally { if (!thread.IsAlive && Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
