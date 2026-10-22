using NAudio.Wave;

namespace Scout.Scribe;

public sealed record Mp3FrameReport(int Frames, double FrameSeconds, int[] Bitrates, int[] SampleRates,
    string[] ChannelModes, bool IncompleteTail);

public static class Mp3Inspection
{
    public static Mp3FrameReport ReadFrames(string path)
    {
        using var input = File.OpenRead(path);
        var rates = new HashSet<int>();
        var samples = new HashSet<int>();
        var modes = new HashSet<string>();
        int frames = 0;
        double seconds = 0;
        bool incompleteTail = false;
        while (true)
        {
            Mp3Frame? frame;
            try { frame = Mp3Frame.LoadFromStream(input); }
            catch (EndOfStreamException) { incompleteTail = true; break; }
            if (frame is null) break;
            rates.Add(frame.BitRate); samples.Add(frame.SampleRate); modes.Add(frame.ChannelMode.ToString());
            frames++; seconds += frame.SampleCount / (double)frame.SampleRate;
        }
        return new(frames, seconds, rates.Order().ToArray(), samples.Order().ToArray(), modes.Order().ToArray(), incompleteTail);
    }
}
