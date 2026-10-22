using System.Diagnostics;
using System.Text.Json;
using NAudio.Wave;
using Scout.Scribe;

namespace Scout.Scribe.Tests;

// Real Windows codecs, synthetic input only. These tests never enumerate capture devices.
public sealed class EncodingTests
{
    [Fact]
    public async Task NormalStopFinalizesExactMono256FormatAndReleasesFiles()
    {
        var root = TemporaryRoot();
        try
        {
            var session = new CaptureSession(root, synthetic: true);
            await session.Started.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await Task.Delay(1300, TestContext.Current.CancellationToken);
            session.Stop();
            var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            Assert.True(result.Completed, result.Error);
            foreach (var file in new[] { "microphone.mp3", "playback.mp3" })
                AssertFormatAndDecode(Path.Combine(result.Directory, file), 1, 3);
            AssertTone(Path.Combine(result.Directory, "microphone.mp3"), 440, 880);
            AssertTone(Path.Combine(result.Directory, "playback.mp3"), 880, 440);
            Assert.Contains("\"kind\":\"completed\"", File.ReadAllText(Path.Combine(result.Directory, "events.jsonl")));
            var second = new CaptureSession(root, synthetic: true);
            await second.Started.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            second.Stop();
            Assert.True((await second.Completion.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken)).Completed);
            Assert.NotEqual(session.DirectoryPath, second.DirectoryPath);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ForcedProcessDeathLeavesCheckpointedMp3DirectlyDecodable()
    {
        var root = TemporaryRoot();
        try
        {
            var app = typeof(CaptureSession).Assembly.Location;
            // The project reference copies the Scribe runtimeconfig/deps alongside its assembly.
            var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var arg in new[] { app, "--synthetic-seconds", "20", "--output", root, "--terminate-after", "12" })
                info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            try { await process.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken); }
            catch { process.Kill(entireProcessTree: true); throw; }
            Assert.NotEqual(0, process.ExitCode);
            var directories = Directory.GetDirectories(root);
            Assert.True(directories.Length == 1, await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken));
            var journal = File.ReadAllText(Path.Combine(directories[0], "events.jsonl"));
            Assert.Contains("\"kind\":\"checkpoint\"", journal);
            Assert.DoesNotContain("\"kind\":\"completed\"", journal);
            Assert.DoesNotContain("\"kind\":\"failed\"", journal); // no crash cleanup/finalization ran
            var checkpoints = journal.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => JsonDocument.Parse(line).RootElement.Clone())
                .Where(item => item.GetProperty("kind").GetString() == "checkpoint")
                .Select(item => item.GetProperty("detail")).ToArray();
            foreach (var file in new[] { "microphone.mp3", "playback.mp3" })
            {
                var checkpoint = checkpoints.Last(item => item.GetProperty("file").GetString() == file).GetProperty("Bytes").GetInt64();
                Assert.True(checkpoint > 0);
                var path = Path.Combine(directories[0], file);
                Assert.True(new FileInfo(path).Length >= checkpoint);
                AssertFormatAndDecode(path, 7, 13);
            }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task EverySessionResolvesSourcesAgainAndRecordsTheirNativeFormats()
    {
        var root = TemporaryRoot();
        var calls = 0;
        ICaptureSource[] Open()
        {
            calls++;
            return [new SyntheticCaptureSource($"microphone {calls}", new WaveFormat(calls == 1 ? 44_100 : 48_000, 16, 1), 440),
                new SyntheticCaptureSource($"playback {calls}", WaveFormat.CreateIeeeFloatWaveFormat(48_000, 2), 880)];
        }
        try
        {
            for (var index = 1; index <= 2; index++)
            {
                var session = new CaptureSession(root, Open, synthetic: true);
                await session.Started.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
                await Task.Delay(500, TestContext.Current.CancellationToken);
                session.Stop();
                var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
                Assert.True(result.Completed, result.Error);
                var journal = File.ReadAllText(Path.Combine(result.Directory, "events.jsonl"));
                Assert.Contains($"microphone {index}", journal);
                Assert.Contains($"playback {index}", journal);
                Assert.Contains(index == 1 ? "44100" : "48000", journal);
            }
            Assert.Equal(2, calls);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    private static void AssertTone(string path, int expected, int other)
    {
        using var decoder = new MediaFoundationReader(path);
        var samples = decoder.ToSampleProvider();
        var buffer = new float[decoder.WaveFormat.SampleRate];
        var count = samples.Read(buffer);
        double Power(int frequency)
        {
            double real = 0, imaginary = 0;
            for (var index = 0; index < count; index++)
            {
                var phase = 2 * Math.PI * frequency * index / decoder.WaveFormat.SampleRate;
                real += buffer[index] * Math.Cos(phase);
                imaginary += buffer[index] * Math.Sin(phase);
            }
            return real * real + imaginary * imaginary;
        }
        Assert.True(Power(expected) > 1, "Expected source tone is missing.");
        Assert.True(Power(expected) > Power(other) * 100, "The other source leaked into this track.");
    }

    private static string TemporaryRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "Scout.Scribe.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void AssertFormatAndDecode(string path, double minimumSeconds, double maximumSeconds)
    {
        var frames = Mp3Inspection.ReadFrames(path);
        Assert.Equal(new[] { 256_000 }, frames.Bitrates);
        Assert.Equal(new[] { 48_000 }, frames.SampleRates);
        Assert.Equal(new[] { "Mono" }, frames.ChannelModes);
        using var decoder = new MediaFoundationReader(path);
        var buffer = new byte[96_000];
        long total = 0;
        int read;
        while ((read = decoder.Read(buffer)) > 0) total += read;
        Assert.InRange(total / (double)decoder.WaveFormat.AverageBytesPerSecond, minimumSeconds, maximumSeconds);
    }
}
