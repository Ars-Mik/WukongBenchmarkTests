namespace WukongBenchmarkTests.Models;

public sealed class GpuInfo
{
    public required string Name { get; init; }

    public string? DriverVersion { get; init; }
}