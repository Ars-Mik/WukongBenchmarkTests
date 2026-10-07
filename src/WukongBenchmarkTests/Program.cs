using WukongBenchmarkTests.Benchmarking;
using WukongBenchmarkTests.Models;
using WukongBenchmarkTests.Reporting;
using WukongBenchmarkTests.System;
using WukongBenchmarkTests.Infrastructure;

var gameSettingsPath =
    @"C:\Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Saved\Config\Windows\GameUserSettings.ini";

var projectRoot =
    Path.GetFullPath(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            ".."));

var resultsDirectory =
    Path.Combine(
        projectRoot,
        "results");

var localArtifactsDirectory =
    Path.Combine(
        projectRoot,
        "local-artifacts");

var cpuCsvPath =
    Path.Combine(
        localArtifactsDirectory,
        "cpu-benchmark.csv");

var gpuCsvPath =
    Path.Combine(
        localArtifactsDirectory,
        "gpu-benchmark.csv");

Directory.CreateDirectory(
    localArtifactsDirectory);

var preflightOnly =
    args.Any(argument =>
        argument.Equals(
            "--preflight-only",
            StringComparison.OrdinalIgnoreCase));

using var cancellationSource =
    new CancellationTokenSource();

// Ctrl+C не обрывает процесс мгновенно.
// Вместо этого запускается штатная отмена:
// PresentMon и Wukong будут закрыты,
// а оригинальный конфиг восстановлен через finally.
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;

    if (cancellationSource.IsCancellationRequested)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine(
        "Получен запрос на остановку.");

    Console.WriteLine(
        "Завершаем текущие процессы и восстанавливаем настройки...");

    cancellationSource.Cancel();
};

Console.WriteLine(
    "=== Black Myth: Wukong Benchmark Tool ===");

Console.WriteLine();

try
{
    Console.WriteLine(
        "Проверяем окружение...");

    var preflightChecker =
        new PreflightChecker();

    await preflightChecker.ValidateAsync(
        gameSettingsPath,
        resultsDirectory,
        localArtifactsDirectory,
        cancellationSource.Token);

    if (preflightOnly)
    {
        Console.WriteLine();
        Console.WriteLine(
            "Режим --preflight-only: benchmark запускаться не будет.");

        return;
    }

    Console.WriteLine();

    Console.WriteLine(
        "Собираем информацию о системе...");

    var systemInfoCollector =
        new SystemInfoCollector();

    var systemInfo =
        systemInfoCollector.Collect();

    PrintSystemInfo(
        systemInfo);

    Console.WriteLine();
    Console.WriteLine(
        "Запускаем полный CPU + GPU benchmark...");

    var suiteRunner =
        new BenchmarkSuiteRunner();

    var suiteResult =
        await suiteRunner.RunAsync(
            gameSettingsPath,
            cpuCsvPath,
            gpuCsvPath,
            nativeWidth:
                systemInfo.ScreenWidth,
            nativeHeight:
                systemInfo.ScreenHeight,
            cancellationToken:
                cancellationSource.Token);

    var report =
        new BenchmarkReport
        {
            TimestampUtc =
                DateTimeOffset.UtcNow,

            System =
                systemInfo,

            CpuProfile =
                suiteResult.CpuProfile,

            CpuResult =
                suiteResult.Cpu,

            GpuProfile =
                suiteResult.GpuProfile,

            GpuResult =
                suiteResult.Gpu
        };

    var reportWriter =
        new BenchmarkReportWriter();

    var reportPath =
        await reportWriter.WriteAsync(
            report,
            resultsDirectory,
            cancellationSource.Token);

    Console.WriteLine();
    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        "=== ИТОГОВЫЕ РЕЗУЛЬТАТЫ ===");

    Console.WriteLine(
        "========================================");

    Console.WriteLine();

    PrintBenchmarkResult(
        "CPU",
        suiteResult.Cpu);

    Console.WriteLine();

    PrintBenchmarkResult(
        "GPU",
        suiteResult.Gpu);

    Console.WriteLine();

    Console.WriteLine(
        "JSON-отчёт сохранён:");

    Console.WriteLine(
        reportPath);

    Console.WriteLine();

    Console.WriteLine(
        "Полный цикл benchmark успешно завершён.");
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine(
        "Benchmark был отменён пользователем.");

    Environment.ExitCode = 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Benchmark завершился с ошибкой:");

    Console.Error.WriteLine(
        exception);

    Environment.ExitCode = 1;
}

static void PrintSystemInfo(
    SystemInfo system)
{
    Console.WriteLine();
    Console.WriteLine(
        "=== СИСТЕМА ===");

    Console.WriteLine(
        $"CPU: {system.CpuName}");

    Console.WriteLine(
        $"Ядра / потоки: " +
        $"{system.CpuCores} / " +
        $"{system.CpuLogicalProcessors}");

    Console.WriteLine();

    for (var index = 0;
         index < system.Gpus.Count;
         index++)
    {
        var gpu =
            system.Gpus[index];

        Console.WriteLine(
            $"GPU {index + 1}: {gpu.Name}");

        Console.WriteLine(
            $"  Драйвер: " +
            $"{gpu.DriverVersion ?? "не определён"}");
    }

    Console.WriteLine();

    Console.WriteLine(
        $"RAM: " +
        $"{system.TotalMemoryGigabytes:F2} ГБ");

    Console.WriteLine(
        $"OS: {system.OperatingSystem}");

    Console.WriteLine(
        $"Архитектура: " +
        $"{system.OsArchitecture}");

    Console.WriteLine(
        $"Основной экран: " +
        $"{system.ScreenWidth}x" +
        $"{system.ScreenHeight}");
}

static void PrintBenchmarkResult(
    string name,
    BenchmarkResult result)
{
    Console.WriteLine(
        $"{name}:");

    Console.WriteLine(
        $"  Средний FPS: " +
        $"{result.AverageFps:F2}");

    Console.WriteLine(
        $"  Минимальный FPS: " +
        $"{result.MinimumFps:F2}");

    Console.WriteLine(
        $"  Максимальный FPS: " +
        $"{result.MaximumFps:F2}");

    Console.WriteLine(
        $"  5-й перцентиль: " +
        $"{result.Low5PercentFps:F2}");

    Console.WriteLine(
        $"  Количество кадров: " +
        $"{result.FrameCount}");

    Console.WriteLine(
        $"  Длительность: " +
        $"{result.DurationSeconds:F3} с");
}