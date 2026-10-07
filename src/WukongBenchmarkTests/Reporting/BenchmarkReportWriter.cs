using System.Text.Json;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Reporting;

public sealed class BenchmarkReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    public async Task<string> WriteAsync(
        BenchmarkReport report,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            report);

        outputDirectory =
            Path.GetFullPath(
                outputDirectory);

        Directory.CreateDirectory(
            outputDirectory);

        var fileName =
            $"wukong-benchmark-" +
            $"{report.TimestampUtc:yyyyMMdd-HHmmss-fff}.json";

        var outputPath =
            Path.Combine(
                outputDirectory,
                fileName);

        await using var stream =
            new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read);

        await JsonSerializer.SerializeAsync(
            stream,
            report,
            JsonOptions,
            cancellationToken);

        return outputPath;
    }
}