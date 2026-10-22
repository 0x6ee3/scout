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
}
