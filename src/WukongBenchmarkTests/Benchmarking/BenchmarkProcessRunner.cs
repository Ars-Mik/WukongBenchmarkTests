using System.Diagnostics;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkProcessRunner
{
    private const string SteamUri = "steam://rungameid/3132990";
    private const string TargetProcessName = "b1-Win64-Shipping";

    public async Task<Process> LaunchAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        EnsureBenchmarkIsNotAlreadyRunning();

        var startInfo = new ProcessStartInfo
        {
            FileName = SteamUri,
            UseShellExecute = true
        };

        Process.Start(startInfo);

        Console.WriteLine(
            "Команда запуска Black Myth: Wukong Benchmark отправлена в Steam.");

        return await WaitForProcessAsync(timeout, cancellationToken);
    }

    private static void EnsureBenchmarkIsNotAlreadyRunning()
    {
        var existingProcesses = Process.GetProcessesByName(TargetProcessName);

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
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var processes = Process.GetProcessesByName(TargetProcessName);

            if (processes.Length > 0)
            {
                var targetProcess = processes[0];

                for (var i = 1; i < processes.Length; i++)
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

    public async Task CloseAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        process.Refresh();

        Console.WriteLine("Закрываем Black Myth: Wukong Benchmark...");

        if (!process.HasExited)
        {
            // закрыть приложение
            var closeRequested = process.CloseMainWindow();

            if (closeRequested)
            {
                var stopwatch = Stopwatch.StartNew();

                while (stopwatch.Elapsed < timeout)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    process.Refresh();

                    if (process.HasExited)
                    {
                        Console.WriteLine("Основной процесс Wukong завершён.");
                        break;
                    }

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(250),
                        cancellationToken);
                }
            }

            process.Refresh();

            // Если закрытие не сработало,
            // завершаем процесс принудительно.
            if (!process.HasExited)
            {
                Console.WriteLine(
                    "Wukong не закрылся штатно. " +
                    "Завершаем процесс принудительно...");

                process.Kill(entireProcessTree: true);

                await process.WaitForExitAsync(cancellationToken);
            }
        }

        // Даже если основной процесс уже исчез,
        // launcher тоже должен быть завершён.
        await CloseLauncherProcessesAsync(cancellationToken);

        Console.WriteLine("Wukong успешно закрыт.");
    }

    private static async Task CloseLauncherProcessesAsync(
        CancellationToken cancellationToken)
    {
        var launcherProcesses = Process.GetProcessesByName("b1_benchmark");

        foreach (var launcherProcess in launcherProcesses)
        {
            using (launcherProcess)
            {
                launcherProcess.Refresh();

                if (launcherProcess.HasExited)
                {
                    continue;
                }

                // launcher завершится сам после основного процесса.
                for (var attempt = 0; attempt < 12; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    launcherProcess.Refresh();

                    if (launcherProcess.HasExited)
                    {
                        break;
                    }

                    await Task.Delay(
                        TimeSpan.FromMilliseconds(250),
                        cancellationToken);
                }

                launcherProcess.Refresh();

                if (!launcherProcess.HasExited)
                {
                    launcherProcess.Kill();

                    await launcherProcess.WaitForExitAsync(cancellationToken);
                }
            }
        }
    }

    public async Task WaitUntilFullyStoppedAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        const int requiredStableChecks = 8;

        var stableChecks = 0;

        while (stopwatch.Elapsed < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var mainRunning = Process.GetProcessesByName("b1-Win64-Shipping").Any();
            var launcherRunning = Process.GetProcessesByName("b1_benchmark").Any();

            if (!mainRunning && !launcherRunning)
            {
                stableChecks++;

                if (stableChecks >= requiredStableChecks)
                {
                    Console.WriteLine("Все процессы Wukong полностью завершены.");
                    return;
                }
            }
            else
            {
                stableChecks = 0;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        var remainingProcesses = Process.GetProcesses()
            .Where(process =>
                process.ProcessName.Equals("b1-Win64-Shipping", StringComparison.OrdinalIgnoreCase) || 
                process.ProcessName.Equals("b1_benchmark", StringComparison.OrdinalIgnoreCase))
            .Select(process =>
            {
                using (process)
                {
                    return $"{process.ProcessName} (PID {process.Id})";
                }
            })
            .ToArray();

        var details = remainingProcesses.Length == 0
            ? "процессы не обнаружены"
            : string.Join(", ", remainingProcesses);

        throw new TimeoutException(
            $"Wukong не завершился полностью за " +
            $"{timeout.TotalSeconds:F0} с. Остались: {details}.");
    }
}