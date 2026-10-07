namespace WukongBenchmarkTests.Models;

public sealed class BenchmarkSuiteResult
{
    public required BenchmarkProfile CpuProfile { get; init; }

    public required BenchmarkResult Cpu { get; init; }

    public required BenchmarkProfile GpuProfile { get; init; }

    public required BenchmarkResult Gpu { get; init; }
}