namespace WukongBenchmarkTests.Models;

public sealed class BenchmarkResult
{
    public double AverageFps { get; init; }

    public double MinimumFps { get; init; }

    public double MaximumFps { get; init; }

    public double Low5PercentFps { get; init; }

    public int FrameCount { get; init; }

    public double DurationSeconds { get; init; }
}