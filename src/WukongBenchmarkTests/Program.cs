using WukongBenchmarkTests.Benchmarking;

var gameSettingsPath =
    @"C:\Program Files (x86)\Steam\steamapps\common\Black Myth Wukong Benchmark Tool\b1\Saved\Config\Windows\GameUserSettings.ini";

var cpuCsvPath =
    @"C:\Users\ars04\Desktop\wukong-benchmark-tests\research\cpu-benchmark.csv";

var gpuCsvPath =
    @"C:\Users\ars04\Desktop\wukong-benchmark-tests\research\gpu-benchmark.csv";

var runner =
    new BenchmarkSuiteRunner();

Console.WriteLine(
    "=== Black Myth: Wukong CPU + GPU Benchmark ===");

try
{
    var result =
        await runner.RunAsync(
            gameSettingsPath,
            cpuCsvPath,
            gpuCsvPath,
            nativeWidth: 2560,
            nativeHeight: 1600);

    Console.WriteLine();
    Console.WriteLine(
        "========================================");

    Console.WriteLine(
        "=== ИТОГОВЫЕ РЕЗУЛЬТАТЫ ===");

    Console.WriteLine(
        "========================================");

    Console.WriteLine();
    Console.WriteLine(
        "CPU:");

    Console.WriteLine(
        $"  Средний FPS: {result.Cpu.AverageFps:F2}");

    Console.WriteLine(
        $"  Минимальный FPS: {result.Cpu.MinimumFps:F2}");

    Console.WriteLine(
        $"  Максимальный FPS: {result.Cpu.MaximumFps:F2}");

    Console.WriteLine(
        $"  5-й перцентиль: {result.Cpu.Low5PercentFps:F2}");

    Console.WriteLine();

    Console.WriteLine(
        "GPU:");

    Console.WriteLine(
        $"  Средний FPS: {result.Gpu.AverageFps:F2}");

    Console.WriteLine(
        $"  Минимальный FPS: {result.Gpu.MinimumFps:F2}");

    Console.WriteLine(
        $"  Максимальный FPS: {result.Gpu.MaximumFps:F2}");

    Console.WriteLine(
        $"  5-й перцентиль: {result.Gpu.Low5PercentFps:F2}");

    Console.WriteLine();
    Console.WriteLine(
        "Полный цикл benchmark успешно завершён.");
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