namespace WukongBenchmarkTests.Models;

public sealed class BenchmarkReport
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required SystemInfo System { get; init; }

    public required BenchmarkProfile CpuProfile { get; init; }

    public required BenchmarkResultReport CpuResult { get; init; }

    public required BenchmarkResultReport GpuResult { get; init; }

    public required BenchmarkProfile GpuProfile { get; init; }

}