namespace WukongBenchmarkTests.Models;

public sealed class WukongInstallation
{
    public required string RootPath { get; init; }

    public required string ExecutablePath { get; init; }

    public required string GameSettingsPath { get; init; }
}