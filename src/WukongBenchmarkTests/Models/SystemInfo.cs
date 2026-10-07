namespace WukongBenchmarkTests.Models;

public sealed class SystemInfo
{
    public required string CpuName { get; init; }

    public required int CpuCores { get; init; }

    public required int CpuLogicalProcessors { get; init; }

    public required IReadOnlyList<GpuInfo> Gpus { get; init; }

    public required ulong TotalMemoryBytes { get; init; }

    public required double TotalMemoryGigabytes { get; init; }

    public required string OperatingSystem { get; init; }

    public required string OsArchitecture { get; init; }

    public required int ScreenWidth { get; init; }

    public required int ScreenHeight { get; init; }
}