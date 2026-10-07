using WukongBenchmarkTests.Benchmarking;

var csvPath =
    @"C:\Users\ars04\Desktop\wukong-benchmark-tests\research\automatic-benchmark.csv";

var presentMonRunner =
    new PresentMonRunner();

var processRunner =
    new BenchmarkProcessRunner();

var uiController =
    new BenchmarkUiController();

var completionDetector =
    new BenchmarkCompletionDetector();

var analyzer =
    new PresentMonCsvAnalyzer();

Console.WriteLine(
    "=== Автоматический Black Myth: Wukong Benchmark ===");

Console.WriteLine();

Console.WriteLine(
    "Запускаем PresentMon...");

presentMonRunner.StartCapture(
    csvPath,
    "b1-Win64-Shipping.exe");

try
{
    await Task.Delay(
        TimeSpan.FromSeconds(1));

    Console.WriteLine(
        "Запускаем Black Myth: Wukong Benchmark...");

    using var benchmarkProcess =
        await processRunner.LaunchAsync(
            TimeSpan.FromSeconds(60));

    Console.WriteLine(
        $"Основной процесс найден. PID: {benchmarkProcess.Id}");

    Console.WriteLine(
        "Запускаем benchmark через интерфейс...");

    await uiController.StartBenchmarkAsync(
        benchmarkProcess,
        TimeSpan.FromSeconds(30));

    Console.WriteLine();
    Console.WriteLine(
        "Benchmark запущен автоматически.");

    await completionDetector.WaitForCompletionAsync(
        csvPath,
        TimeSpan.FromMinutes(5));

    Console.WriteLine(
        "Завершение benchmark обнаружено.");
}
finally
{
    Console.WriteLine(
        "Останавливаем PresentMon...");

    await presentMonRunner.StopCaptureAsync();
}

// Даём файловой системе небольшой момент после
// окончательного завершения live-записи.
await Task.Delay(
    TimeSpan.FromMilliseconds(500));

Console.WriteLine(
    "Анализируем результаты...");

var result =
    analyzer.Analyze(csvPath);

Console.WriteLine();
Console.WriteLine(
    "=== Результат ===");

Console.WriteLine(
    $"Средний FPS: {result.AverageFps:F2}");

Console.WriteLine(
    $"Минимальный FPS: {result.MinimumFps:F2}");

Console.WriteLine(
    $"Максимальный FPS: {result.MaximumFps:F2}");

Console.WriteLine(
    $"5-й перцентиль FPS: {result.Low5PercentFps:F2}");

Console.WriteLine(
    $"Количество кадров: {result.FrameCount}");

Console.WriteLine(
    $"Длительность benchmark: {result.DurationSeconds:F3} с");

Console.WriteLine();
Console.WriteLine(
    "Wukong оставлен открытым на экране результатов.");