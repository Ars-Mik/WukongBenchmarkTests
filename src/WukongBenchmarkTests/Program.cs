using WukongBenchmarkTests.Benchmarking;

var csvPath =
    @"C:\Users\ars04\Desktop\wukong-benchmark-tests\research\single-benchmark.csv";

var runner =
    new BenchmarkRunner();

Console.WriteLine(
    "=== Black Myth: Wukong Benchmark ===");

Console.WriteLine();

try
{
    var result =
        await runner.RunAsync(
            csvPath);

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
        $"Длительность benchmark: " +
        $"{result.DurationSeconds:F3} с");

    Console.WriteLine();
    Console.WriteLine(
        "Benchmark успешно завершён.");
}
catch (Exception exception)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(
        "Benchmark завершился с ошибкой:");

    Console.Error.WriteLine(
        exception.Message);

    Environment.ExitCode = 1;
}