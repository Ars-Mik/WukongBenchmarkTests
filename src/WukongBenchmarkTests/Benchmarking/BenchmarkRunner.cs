using System.Diagnostics;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkRunner
{
    private const string TargetProcessName =
        "b1-Win64-Shipping.exe";

    private readonly PresentMonRunner _presentMonRunner;
    private readonly BenchmarkProcessRunner _processRunner;
    private readonly BenchmarkUiController _uiController;
    private readonly BenchmarkCompletionDetector _completionDetector;
    private readonly PresentMonCsvAnalyzer _csvAnalyzer;

    public BenchmarkRunner()
    {
        _presentMonRunner =
            new PresentMonRunner();

        _processRunner =
            new BenchmarkProcessRunner();

        _uiController =
            new BenchmarkUiController();

        _completionDetector =
            new BenchmarkCompletionDetector();

        _csvAnalyzer =
            new PresentMonCsvAnalyzer();
    }

    public async Task<BenchmarkResult> RunAsync(
        string csvPath,
        CancellationToken cancellationToken = default)
    {
        Process? benchmarkProcess = null;
        bool presentMonStarted = false;

        try
        {
            Console.WriteLine(
                "Запускаем PresentMon...");

            _presentMonRunner.StartCapture(
                csvPath,
                TargetProcessName);

            presentMonStarted = true;

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);

            Console.WriteLine(
                "Запускаем Black Myth: Wukong Benchmark...");

            benchmarkProcess =
                await _processRunner.LaunchAsync(
                    TimeSpan.FromSeconds(60),
                    cancellationToken);

            Console.WriteLine(
                $"Основной процесс найден. PID: {benchmarkProcess.Id}");

            Console.WriteLine(
                "Запускаем benchmark через интерфейс...");

            await _uiController.StartBenchmarkAsync(
                benchmarkProcess,
                TimeSpan.FromSeconds(30),
                cancellationToken);

            Console.WriteLine();
            Console.WriteLine(
                "Benchmark запущен автоматически.");

            await _completionDetector.WaitForCompletionAsync(
                csvPath,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            Console.WriteLine(
                "Завершение benchmark обнаружено.");

            Console.WriteLine(
                "Останавливаем PresentMon...");

            await _presentMonRunner.StopCaptureAsync();

            presentMonStarted = false;

            await Task.Delay(
                TimeSpan.FromMilliseconds(500),
                cancellationToken);

            Console.WriteLine(
                "Анализируем результаты...");

            return _csvAnalyzer.Analyze(
                csvPath);
        }
        finally
        {
            // Cleanup выполняем даже при timeout,
            // ошибке UI или отмене операции.

            if (presentMonStarted)
            {
                try
                {
                    Console.WriteLine(
                        "Останавливаем PresentMon...");

                    await _presentMonRunner.StopCaptureAsync();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(
                        $"Не удалось корректно остановить PresentMon: " +
                        $"{exception.Message}");
                }
            }

            if (benchmarkProcess is not null)
            {
                try
                {
                    // Очистка не должна отменяться вместе
                    // с основной операцией.
                    await _processRunner.CloseAsync(
                        benchmarkProcess,
                        TimeSpan.FromSeconds(5),
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(
                        $"Не удалось корректно закрыть Wukong: " +
                        $"{exception.Message}");
                }
                finally
                {
                    benchmarkProcess.Dispose();
                }
            }
        }
    }
}