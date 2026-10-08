using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class PresentMonCsvAnalyzer
{
    private const string TargetProcessName = "b1-Win64-Shipping.exe";

    public BenchmarkResult Analyze(string csvPath)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException(
                "PresentMon CSV file was not found.",
                csvPath);
        }

        var rows = ReadRows(csvPath);

        var wukongRows = rows
            .Where(row =>
                string.Equals(
                    row.Application,
                    TargetProcessName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (wukongRows.Count == 0)
        {
            throw new InvalidOperationException(
                $"No frames from {TargetProcessName} were found.");
        }

        var mainRows = wukongRows
            .GroupBy(row => row.SwapChainAddress)
            .OrderByDescending(group => group.Count())
            .First()
            .OrderBy(row => row.CpuStartTime)
            .ToList();

        var benchmarkRows = ExtractBenchmarkSegment(mainRows);

        var frameTimes = benchmarkRows
            .Select(row => row.FrameTime)
            .Where(frameTime => frameTime > 0)
            .ToList();

        if (frameTimes.Count == 0)
        {
            throw new InvalidOperationException(
                "Benchmark segment does not contain valid frame times.");
        }

        var durationSeconds = frameTimes.Sum() / 1000.0;
        var averageFps = frameTimes.Count / durationSeconds;

        // Для минимального, максимального FPS и 5-го перцентиля
        // используем устойчивые секундные интервалы, а не отдельные кадры.
        // Это не позволяет одиночному микрофризу исказить весь результат.
        var oneSecondFpsSamples = CalculateOneSecondFpsSamples(benchmarkRows);

        if (oneSecondFpsSamples.Count == 0)
        {
            throw new InvalidOperationException(
                "Не удалось получить секундные значения FPS.");
        }

        var minimumFps = oneSecondFpsSamples.Min();
        var maximumFps = oneSecondFpsSamples.Max();

        var low5PercentFps = CalculateLowerPercentile(
            oneSecondFpsSamples,
            0.05);

        return new BenchmarkResult
        {
            AverageFps = averageFps,
            MinimumFps = minimumFps,
            MaximumFps = maximumFps,
            Low5PercentFps = low5PercentFps,
            FrameCount = frameTimes.Count,
            DurationSeconds = durationSeconds
        };
    }

    private static List<PresentMonRow> ExtractBenchmarkSegment(
        List<PresentMonRow> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        const double startTransitionThresholdMs = 200.0;
        const double endTransitionThresholdMs = 500.0;

        var firstTimestamp = rows[0].CpuStartTime;
        var benchmarkStartIndex = -1;

        // Сначала ищем переход из меню непосредственно в benchmark.
        for (var i = 0; i < rows.Count; i++)
        {
            var elapsedSeconds =
                (rows[i].CpuStartTime - firstTimestamp) / 1000.0;

            if (elapsedSeconds < 10)
            {
                continue;
            }

            if (rows[i].FrameTime >= startTransitionThresholdMs)
            {
                benchmarkStartIndex = i;
                break;
            }
        }

        if (benchmarkStartIndex < 0)
        {
            throw new InvalidOperationException(
                "Не удалось определить начало benchmark.");
        }

        var benchmarkEndIndex = -1;

        // После обнаружения начала ищем крупный переход
        // на экран результатов. Чтобы случайный микрофриз
        // внутри benchmark не считался концом, требуем,
        // чтобы прошло минимум 60 секунд.
        for (var i = benchmarkStartIndex + 1; i < rows.Count; i++)
        {
            var benchmarkDurationSeconds =
                (rows[i].CpuStartTime -
                 rows[benchmarkStartIndex].CpuStartTime) / 1000.0;

            if (benchmarkDurationSeconds < 60)
            {
                continue;
            }

            if (rows[i].FrameTime >= endTransitionThresholdMs)
            {
                benchmarkEndIndex = i;
                break;
            }
        }

        if (benchmarkEndIndex < 0)
        {
            throw new InvalidOperationException(
                "Не удалось определить конец benchmark.");
        }

        var startSeconds =
            (rows[benchmarkStartIndex].CpuStartTime - firstTimestamp) / 1000.0;

        var endSeconds =
            (rows[benchmarkEndIndex].CpuStartTime - firstTimestamp) / 1000.0;

        Console.WriteLine(
            $"Начало benchmark обнаружено на {startSeconds:F3} с.");

        Console.WriteLine(
            $"Конец benchmark обнаружен на {endSeconds:F3} с.");

        return rows
            .Skip(benchmarkStartIndex + 1)
            .Take(benchmarkEndIndex - benchmarkStartIndex - 1)
            .ToList();
    }

    private static List<double> CalculateOneSecondFpsSamples(
        IReadOnlyList<PresentMonRow> rows)
    {
        if (rows.Count < 2)
        {
            return [];
        }

        var firstTimestamp = rows[0].CpuStartTime;
        var lastTimestamp = rows[^1].CpuStartTime;

        var durationSeconds = (lastTimestamp - firstTimestamp) / 1000.0;
        var fullSecondCount = (int)Math.Floor(durationSeconds);

        var samples = new List<double>();

        // Первый секундный интервал пропускаем:
        // после перехода в benchmark там ещё может присутствовать
        // инициализация сцены и остаточная загрузка.
        //
        // Последний неполный интервал тоже не используем.
        for (var second = 1; second < fullSecondCount; second++)
        {
            var windowStart = firstTimestamp + second * 1000.0;
            var windowEnd = windowStart + 1000.0;

            var frameCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var timestamp = rows[i].CpuStartTime;

                if (timestamp >= windowStart && timestamp < windowEnd)
                {
                    frameCount++;
                }
            }

            if (frameCount > 0)
            {
                // Интервал длится ровно одну секунду,
                // поэтому количество кадров равно FPS.
                samples.Add(frameCount);
            }
        }

        return samples;
    }

    private static double CalculateLowerPercentile(
        IReadOnlyList<double> values,
        double percentile)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException(
                "Коллекция значений не может быть пустой.",
                nameof(values));
        }

        var sorted = values
            .OrderBy(value => value)
            .ToArray();

        var position = (sorted.Length - 1) * percentile;
        var index = (int)Math.Floor(position);

        return sorted[index];
    }

    private static List<PresentMonRow> ReadRows(string csvPath)
    {
        var rows = new List<PresentMonRow>();

        using var parser = new TextFieldParser(csvPath);

        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields()
            ?? throw new InvalidDataException(
                "PresentMon CSV does not contain a header.");

        var applicationIndex = FindColumn(headers, "Application");
        var swapChainIndex = FindColumn(headers, "SwapChainAddress");
        var cpuStartTimeIndex = FindColumn(headers, "CPUStartTime");
        var frameTimeIndex = FindColumn(headers, "FrameTime");

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();

            if (fields is null || fields.Length != headers.Length)
            {
                continue;
            }

            if (!TryParseDouble(fields[cpuStartTimeIndex], out var cpuStartTime))
            {
                continue;
            }

            if (!TryParseDouble(fields[frameTimeIndex], out var frameTime))
            {
                continue;
            }

            rows.Add(
                new PresentMonRow(
                    fields[applicationIndex],
                    fields[swapChainIndex],
                    cpuStartTime,
                    frameTime));
        }

        return rows;
    }

    private static bool TryParseDouble(
        string value,
        out double result)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result);
    }

    private static int FindColumn(
        string[] headers,
        string columnName)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            if (string.Equals(
                    headers[i],
                    columnName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidDataException(
            $"Required PresentMon column '{columnName}' was not found.");
    }

    private sealed record PresentMonRow(
        string Application,
        string SwapChainAddress,
        double CpuStartTime,
        double FrameTime);
}