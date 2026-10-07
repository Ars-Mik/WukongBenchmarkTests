namespace WukongBenchmarkTests.Models;

public sealed class BenchmarkSuiteResult
{
    public required BenchmarkResult Cpu { get; init; }

    public required BenchmarkResult Gpu { get; init; }
}