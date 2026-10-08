using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkCompletionDetector
{

    private const double StartTransitionThresholdMs = 200.0;
    private const double EndTransitionThresholdMs = 500.0;

    private static readonly TimeSpan MinimumStartDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MinimumBenchmarkDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public async Task WaitForCompletionAsync(
        string csvPath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTime.UtcNow;
        double? benchmarkStartTimestamp = null;

        Console.WriteLine("Ожидаем завершения benchmark...");

        while (DateTime.UtcNow - startedAt < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (File.Exists(csvPath))
            {
                var rows = TryReadSnapshot(csvPath);

                if (rows.Count > 0)
                {
                    var firstTimestamp = rows[0].Timestamp;

                    if (benchmarkStartTimestamp is null)
                    {
                        var startMarker = rows.FirstOrDefault(row =>
                            row.Timestamp - firstTimestamp >= MinimumStartDelay.TotalMilliseconds
                            && row.FrameTime >= StartTransitionThresholdMs);

                        if (startMarker is not null)
                        {
                            benchmarkStartTimestamp = startMarker.Timestamp;

                            var seconds =
                                (startMarker.Timestamp - firstTimestamp) / 1000.0;

                            Console.WriteLine(
                                $"Обнаружено начало теста benchmark: {seconds:F1} с.");
                        }
                    }

                    if (benchmarkStartTimestamp is not null)
                    {
                        var endMarker = rows.FirstOrDefault(row =>
                            row.Timestamp > benchmarkStartTimestamp.Value
                            && row.Timestamp - benchmarkStartTimestamp.Value
                            >= MinimumBenchmarkDuration.TotalMilliseconds
                            && row.FrameTime >= EndTransitionThresholdMs);

                        if (endMarker is not null)
                        {
                            var duration =
                                (endMarker.Timestamp - benchmarkStartTimestamp.Value) / 1000.0;

                            Console.WriteLine(
                                $"Обнаружено завершение теста benchmark. " +
                                $"Длительность прогона: {duration:F1} с.");

                            return;
                        }
                    }
                }
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new TimeoutException(
            $"Benchmark не завершился за {timeout.TotalMinutes:F0} минут.");
    }

    private static List<FrameRow> TryReadSnapshot(string csvPath)
    {
        try
        {
            using var stream = new FileStream(
                csvPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var parser = new TextFieldParser(stream);

            parser.TextFieldType = FieldType.Delimited;
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;

            var headers = parser.ReadFields();

            if (headers is null)
            {
                return [];
            }

            var timestampIndex = FindFirstColumn(
                headers,
                "CPUStartTime",
                "CPUStartTimeInMs",
                "TimeInMs");

            var frameTimeIndex = FindFirstColumn(
                headers,
                "FrameTime",
                "MsBetweenPresents");

            var rows = new List<FrameRow>();

            while (!parser.EndOfData)
            {
                var fields = parser.ReadFields();

                // PresentMon может в этот момент ещё дописывать
                // последнюю строку файла. Неполную строку просто
                // пропускаем до следующей проверки.
                if (fields is null || fields.Length != headers.Length)
                {
                    continue;
                }

                if (!TryParseDouble(fields[timestampIndex], out var timestamp))
                {
                    continue;
                }

                if (!TryParseDouble(fields[frameTimeIndex], out var frameTime))
                {
                    continue;
                }

                rows.Add(new FrameRow(timestamp, frameTime));
            }

            return rows;
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static int FindFirstColumn(
        string[] headers,
        params string[] possibleNames)
    {
        foreach (var name in possibleNames)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                if (string.Equals(
                        headers[i],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        throw new InvalidDataException(
            $"Не найден необходимый столбец PresentMon: " +
            $"{string.Join(" / ", possibleNames)}.");
    }

    private static bool TryParseDouble(string value,out double result)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private sealed record FrameRow(
        double Timestamp,
        double FrameTime);
}