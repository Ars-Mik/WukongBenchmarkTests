using WukongBenchmarkTests.Configuration;
using WukongBenchmarkTests.Models;
using System.Diagnostics;
using System.Security.Cryptography;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkSuiteRunner
{
    private readonly BenchmarkRunner _benchmarkRunner =
        new();

    public async Task<BenchmarkSuiteResult> RunAsync(
        string gameSettingsPath,
        string cpuCsvPath,
        string gpuCsvPath,
        int nativeWidth,
        int nativeHeight,
        CancellationToken cancellationToken = default)
    {
        gameSettingsPath =
            Path.GetFullPath(gameSettingsPath);

        if (!File.Exists(gameSettingsPath))
        {
            throw new FileNotFoundException(
                "GameUserSettings.ini не найден.",
                gameSettingsPath);
        }

        var originalConfigHash = CalculateSha256(gameSettingsPath);

        Console.WriteLine(
            $"SHA-256 исходного конфига: {originalConfigHash}");


        var cpuProfile =
            BenchmarkProfiles.CreateCpuProfile(
                nativeWidth,
                nativeHeight);

        var gpuProfile =
            BenchmarkProfiles.CreateGpuProfile(
                nativeWidth,
                nativeHeight);

        // Создаём отдельный неизменяемый снимок оригинального
        // конфига. Он не зависит от того, изменит ли первый
        // профиль файл или окажется уже установленным.
        var originalSnapshotPath =
            $"{gameSettingsPath}.wbr-original-{Guid.NewGuid():N}.bak";

        File.Copy(
            gameSettingsPath,
            originalSnapshotPath,
            overwrite: false);

        string? cpuGeneratedBackup = null;
        string? gpuGeneratedBackup = null;

        try
        {
            Console.WriteLine();
            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "=== CPU BENCHMARK ===");

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "Применяем CPU-профиль...");

            cpuGeneratedBackup =
                GameSettingsFile.ApplyProfile(
                    gameSettingsPath,
                    cpuProfile);

            var cpuResult =
                await _benchmarkRunner.RunAsync(
                    cpuCsvPath,
                    cancellationToken);

            Console.WriteLine();
            Console.WriteLine(
                "CPU benchmark завершён.");

            Console.WriteLine(
                $"Средний FPS: {cpuResult.AverageFps:F2}");

            // После закрытия первого Wukong специально
            // выдерживаем 15 секунд.
            //
            // Steam и launcher получают время полностью завершить
            // предыдущую игровую сессию.
            //
            // Каждую секунду одновременно проверяем наличие
            // процессов Wukong и PresentMon.
            await WaitBeforeNextRunAsync(
                TimeSpan.FromSeconds(15),
                cancellationToken);

            Console.WriteLine();

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "=== GPU BENCHMARK ===");

            Console.WriteLine(
                "========================================");

            Console.WriteLine(
                "Применяем GPU-профиль...");

            gpuGeneratedBackup =
                GameSettingsFile.ApplyProfile(
                    gameSettingsPath,
                    gpuProfile);

            var gpuResult =
                await _benchmarkRunner.RunAsync(
                    gpuCsvPath,
                    cancellationToken);

            Console.WriteLine();
            Console.WriteLine(
                "GPU benchmark завершён.");

            Console.WriteLine(
                $"Средний FPS: {gpuResult.AverageFps:F2}");

            return new BenchmarkSuiteResult
            {
                CpuProfile = cpuProfile,
                Cpu = cpuResult,

                GpuProfile = gpuProfile,
                Gpu = gpuResult
            };
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine(
                "Восстанавливаем исходный GameUserSettings.ini...");

            try
            {
                // Restore должен выполняться даже если основная
                // операция была отменена или один из тестов упал.
                _ = GameSettingsFile.Restore(
                    gameSettingsPath,
                    originalSnapshotPath);

                Console.WriteLine(
                    "Исходный конфиг восстановлен.");

                var restoredConfigHash =
                    CalculateSha256(
                        gameSettingsPath);

                Console.WriteLine(
                    $"SHA-256 восстановленного конфига: {restoredConfigHash}");

                if (!string.Equals(
                        originalConfigHash,
                        restoredConfigHash,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "После завершения benchmark GameUserSettings.ini " +
                        "не совпадает с исходным файлом.");
                }

                Console.WriteLine(
                    "Целостность исходного конфига подтверждена.");
            }
            finally
            {
                TryDelete(
                    originalSnapshotPath);

                TryDelete(
                    cpuGeneratedBackup);

                TryDelete(
                    gpuGeneratedBackup);
            }
        }
    }

    private static void TryDelete(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Не удалось удалить временный backup " +
                $"'{path}': {exception.Message}");
        }
    }

    private static async Task WaitBeforeNextRunAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        var seconds =
            (int)Math.Ceiling(
                delay.TotalSeconds);

        Console.WriteLine();
        Console.WriteLine(
            "Ожидаем полного завершения предыдущей сессии...");

        Console.WriteLine(
            "Во время ожидания проверяем Wukong, launcher и PresentMon.");

        Console.WriteLine();

        for (var remaining = seconds;
            remaining > 0;
            remaining--)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var mainIds =
                GetProcessIds(
                    "b1-Win64-Shipping");

            var launcherIds =
                GetProcessIds(
                    "b1_benchmark");

            var presentMonIds =
                GetProcessIds(
                    "presentmon");

            Console.WriteLine(
                $"Следующий запуск через {remaining,2} с | " +
                $"Wukong: {FormatProcessState(mainIds)} | " +
                $"Launcher: {FormatProcessState(launcherIds)} | " +
                $"PresentMon: {FormatProcessState(presentMonIds)}");

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        // После полного обратного отсчёта выполняем
        // окончательную проверку среды.
        var finalMainIds =
            GetProcessIds(
                "b1-Win64-Shipping");

        var finalLauncherIds =
            GetProcessIds(
                "b1_benchmark");

        var finalPresentMonIds =
            GetProcessIds(
                "presentmon");

        if (finalMainIds.Length > 0 ||
            finalLauncherIds.Length > 0 ||
            finalPresentMonIds.Length > 0)
        {
            throw new InvalidOperationException(
                "Предыдущая benchmark-сессия не завершилась полностью. " +
                $"Wukong: {FormatProcessState(finalMainIds)}, " +
                $"Launcher: {FormatProcessState(finalLauncherIds)}, " +
                $"PresentMon: {FormatProcessState(finalPresentMonIds)}.");
        }

        Console.WriteLine();
        Console.WriteLine(
            "Проверка завершена: " +
            "Wukong, launcher и PresentMon не запущены.");

        Console.WriteLine(
            "Среда готова к следующему benchmark.");
    }

    private static int[] GetProcessIds(
        string processName)
    {
        var processes =
            Process.GetProcessesByName(
                processName);

        try
        {
            return processes
                .Where(process =>
                {
                    try
                    {
                        return !process.HasExited;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Select(process =>
                    process.Id)
                .ToArray();
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static string FormatProcessState(
        int[] processIds)
    {
        if (processIds.Length == 0)
        {
            return "нет";
        }

        return "PID " +
            string.Join(
                ", ",
                processIds);
    }

    private static string CalculateSha256(
        string path)
    {
        using var stream =
            File.OpenRead(path);

        var hash =
            SHA256.HashData(stream);

        return Convert.ToHexString(
            hash);
    }
}