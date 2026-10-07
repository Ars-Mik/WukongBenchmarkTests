namespace WukongBenchmarkTests.Models;

public sealed class BenchmarkReport
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required SystemInfo System { get; init; }

    public required BenchmarkProfile CpuProfile { get; init; }

    public required BenchmarkResult CpuResult { get; init; }

    public required BenchmarkProfile GpuProfile { get; init; }

    public required BenchmarkResult GpuResult { get; init; }
}