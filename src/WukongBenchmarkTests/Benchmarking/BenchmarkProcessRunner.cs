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

    public async Task CloseAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        process.Refresh();

        if (process.HasExited)
        {
            return;
        }

        Console.WriteLine(
            "Закрываем Black Myth: Wukong Benchmark...");

        // Сначала пытаемся закрыть приложение обычным способом,
        // как если бы пользователь нажал кнопку закрытия окна.
        var closeRequested =
            process.CloseMainWindow();

        if (closeRequested)
        {
            var stopwatch =
                Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                process.Refresh();

                if (process.HasExited)
                {
                    Console.WriteLine(
                        "Wukong успешно закрыт.");

                    await CloseLauncherProcessesAsync(
                        cancellationToken);

                    return;
                }

                await Task.Delay(
                    TimeSpan.FromMilliseconds(250),
                    cancellationToken);
            }
        }

        // Если приложение не отреагировало на обычное закрытие,
        // завершаем процесс принудительно.
        process.Refresh();

        if (!process.HasExited)
        {
            Console.WriteLine(
                "Wukong не закрылся штатно. Завершаем процесс принудительно...");

            process.Kill(
                entireProcessTree: true);

            await process.WaitForExitAsync(
                cancellationToken);
        }

        await CloseLauncherProcessesAsync(
            cancellationToken);

        Console.WriteLine(
            "Wukong закрыт.");
    }

    private static async Task CloseLauncherProcessesAsync(
        CancellationToken cancellationToken)
    {
        var launcherProcesses =
            Process.GetProcessesByName(
                "b1_benchmark");

        foreach (var launcherProcess in launcherProcesses)
        {
            using (launcherProcess)
            {
                launcherProcess.Refresh();

                if (launcherProcess.HasExited)
                {
                    continue;
                }

                // Обычно launcher завершается сам после основного процесса.
                // Даём ему немного времени.
                for (var attempt = 0;
                    attempt < 12;
                    attempt++)
                {
                    cancellationToken
                        .ThrowIfCancellationRequested();

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
                    await launcherProcess.WaitForExitAsync(
                        cancellationToken);
                }
            }
        }
    }


}
