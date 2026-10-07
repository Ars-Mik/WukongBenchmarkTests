using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkRunner
{
    private const string TargetProcessName =
        "b1-Win64-Shipping.exe";

    private readonly BenchmarkProcessRunner _processRunner;
    private readonly PresentMonRunner _presentMonRunner;
    private readonly PresentMonCsvAnalyzer _csvAnalyzer;

    public BenchmarkRunner()
    {
        _processRunner =
            new BenchmarkProcessRunner();

        _presentMonRunner =
            new PresentMonRunner();

        _csvAnalyzer =
            new PresentMonCsvAnalyzer();
    }

    public async Task<BenchmarkResult> RunInteractiveAsync(
        string csvPath,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(
            "Запускаем сбор кадров PresentMon...");

        _presentMonRunner.StartCapture(
            csvPath,
            TargetProcessName);

        try
        {
            // Даём PresentMon время создать ETW-сессию
            // до запуска самого benchmark-приложения.
            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);

            Console.WriteLine(
                "Запускаем Black Myth: Wukong Benchmark...");

            using var benchmarkProcess =
                await _processRunner.LaunchAsync(
                    TimeSpan.FromSeconds(60),
                    cancellationToken);

            Console.WriteLine(
                $"Основной процесс найден. PID: {benchmarkProcess.Id}");

            Console.WriteLine();
            Console.WriteLine(
                "Временно требуется ручной шаг:");

            Console.WriteLine(
                "1. В приложении запусти «Тест быстродействия».");

            Console.WriteLine(
                "2. Дождись экрана результатов.");

            Console.WriteLine(
                "3. После этого закрой приложение обычным способом.");

            Console.WriteLine();
            Console.WriteLine(
                "Программа автоматически продолжит работу после закрытия Wukong.");

            await benchmarkProcess.WaitForExitAsync(
                cancellationToken);

            Console.WriteLine(
                "Основной процесс Wukong завершён.");
        }
        finally
        {
            Console.WriteLine(
                "Останавливаем сбор PresentMon...");

            await _presentMonRunner.StopCaptureAsync();
        }

        // Даём системе небольшой момент на завершение
        // записи и закрытие CSV-файла.
        await Task.Delay(
            TimeSpan.FromMilliseconds(500),
            cancellationToken);

        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException(
                "PresentMon не создал CSV-файл.",
                csvPath);
        }

        Console.WriteLine(
            "Анализируем полученные данные...");

        return _csvAnalyzer.Analyze(
            csvPath);
    }
}