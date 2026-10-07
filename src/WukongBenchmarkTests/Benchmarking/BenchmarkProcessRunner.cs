using System.Diagnostics;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkProcessRunner
{
    private const string SteamUri =
        "steam://rungameid/3132990";

    private const string TargetProcessName =
        "b1-Win64-Shipping";

    public async Task<Process> LaunchAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        EnsureBenchmarkIsNotAlreadyRunning();

        var startInfo =
            new ProcessStartInfo
            {
                FileName = SteamUri,
                UseShellExecute = true
            };

        Process.Start(startInfo);

        Console.WriteLine(
            "Команда запуска Black Myth: Wukong Benchmark отправлена Steam.");

        return await WaitForProcessAsync(
            timeout,
            cancellationToken);
    }

    private static void EnsureBenchmarkIsNotAlreadyRunning()
    {
        var existingProcesses =
            Process.GetProcessesByName(
                TargetProcessName);

        try
        {
            if (existingProcesses.Length > 0)
            {
                throw new InvalidOperationException(
                    "Black Myth: Wukong Benchmark уже запущен. " +
                    "Закрой его перед новым запуском.");
            }
        }
        finally
        {
            foreach (var process in existingProcesses)
            {
                process.Dispose();
            }
        }
    }

    private static async Task<Process> WaitForProcessAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var stopwatch =
            Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var processes =
                Process.GetProcessesByName(
                    TargetProcessName);

            if (processes.Length > 0)
            {
                var targetProcess =
                    processes[0];

                // Освобождаем все найденные экземпляры,
                // кроме того, который возвращаем вызывающему коду.
                for (var i = 1;
                     i < processes.Length;
                     i++)
                {
                    processes[i].Dispose();
                }

                return targetProcess;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        throw new TimeoutException(
            $"Процесс {TargetProcessName}.exe " +
            $"не появился за {timeout.TotalSeconds:F0} секунд.");
    }
}