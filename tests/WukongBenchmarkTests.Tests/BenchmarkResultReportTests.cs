using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Tests;

public sealed class BenchmarkResultReportTests
{
    [Fact]
    public void From_RoundsBenchmarkValuesForReport()
    {
        var result = new BenchmarkResult
        {
            AverageFps = 157.81008292753924,
            MinimumFps = 139.99991,
            MaximumFps = 212.0049,
            Low5PercentFps = 146.9999,
            FrameCount = 23739,
            DurationSeconds = 150.4276505000007
        };

        var report = BenchmarkResultReport.From(result);

        Assert.Equal(157.81, report.AverageFps);
        Assert.Equal(140.00, report.MinimumFps);
        Assert.Equal(212.00, report.MaximumFps);
        Assert.Equal(147.00, report.Low5PercentFps);
        Assert.Equal(23739, report.FrameCount);
        Assert.Equal(150.428, report.DurationSeconds);
    }
}