namespace WukongBenchmarkTests.Models;

/// <summary>
/// Представление результата benchmark для итогового JSON-отчёта.
/// Внутренний BenchmarkResult остаётся с полной точностью.
/// </summary>
public sealed class BenchmarkResultReport
{
    public required double AverageFps { get; init; }

    public required double MinimumFps { get; init; }

    public required double MaximumFps { get; init; }

    public required double Low5PercentFps { get; init; }

    public required int FrameCount { get; init; }

    public required double DurationSeconds { get; init; }

    public static BenchmarkResultReport From(BenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new BenchmarkResultReport
        {
            AverageFps = Math.Round(result.AverageFps,2, MidpointRounding.AwayFromZero),

            MinimumFps = Math.Round(result.MinimumFps, 2, MidpointRounding.AwayFromZero),

            MaximumFps = Math.Round(result.MaximumFps, 2, MidpointRounding.AwayFromZero),

            Low5PercentFps = Math.Round(result.Low5PercentFps, 2, MidpointRounding.AwayFromZero),

            FrameCount = result.FrameCount,

            DurationSeconds = Math.Round(result.DurationSeconds, 3, MidpointRounding.AwayFromZero)
        };
    }
}