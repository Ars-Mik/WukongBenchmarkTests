using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace WukongBenchmarkTests.Infrastructure;

public sealed class PreflightChecker
{
    private static readonly string[] BenchmarkProcessNames =
    [
        "b1_benchmark",
        "b1-Win64-Shipping",
        "presentmon"
    ];

    private static int _lastProgressLength;

    public async Task ValidateAsync(
        string gameSettingsPath,
        string resultsDirectory,
        string localArtifactsDirectory,
        CancellationToken cancellationToken = default)
    {
        CheckWindows();

        gameSettingsPath = Path.GetFullPath(gameSettingsPath);

        CheckGameSettings(gameSettingsPath);
        CheckBenchmarkExecutable(gameSettingsPath);
        CheckSteamProtocol();
        CheckProcesses();
        CheckPresentMonPermissions();

        await CheckPresentMonAsync(cancellationToken);

        CheckDirectoryWritable(resultsDirectory, "Папка результатов");

        CheckDirectoryWritable(localArtifactsDirectory,"Папка временных файлов");

        CompleteProgress();
    }

    private static void CheckWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Инструмент поддерживается только на Windows.");
        }

        PrintSuccess("Windows обнаружена.");
    }

    private static void CheckGameSettings(string gameSettingsPath)
    {
        if (!File.Exists(gameSettingsPath))
        {
            throw new FileNotFoundException("GameUserSettings.ini не найден.", gameSettingsPath);
        }

        // Проверяем возможность открыть реальный конфиг
        // одновременно на чтение и запись.
        using (
            File.Open(
                gameSettingsPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.Read))
        {
        }

        var directory = Path.GetDirectoryName(gameSettingsPath)
            ?? throw new InvalidOperationException(
                "Не удалось определить папку GameUserSettings.ini.");

        // Backup создаётся рядом с оригинальным конфигом,
        // поэтому отдельно проверяем доступ к этой папке.
        CheckDirectoryWritable(directory, "Папка GameUserSettings.ini");

        PrintSuccess("GameUserSettings.ini найден и доступен для изменения.");
    }

    private static void CheckBenchmarkExecutable(string gameSettingsPath)
    {
        var configDirectory = new DirectoryInfo(Path.GetDirectoryName(gameSettingsPath)
            ?? throw new InvalidOperationException(
                "Не удалось определить папку конфига."));

        // Config -> Saved -> b1 -> корень Benchmark Tool.
        var benchmarkRoot = configDirectory;

        for (var level = 0; level < 4; level++)
        {
            benchmarkRoot = benchmarkRoot.Parent ?? throw new InvalidOperationException(
                "Не удалось определить корневую папку Wukong Benchmark.");
        }

        var executablePath = Path.Combine(benchmarkRoot.FullName, "b1_benchmark.exe");

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("b1_benchmark.exe не найден.", executablePath);
        }

        PrintSuccess("Wukong Benchmark найден.");
    }

    private static void CheckSteamProtocol()
    {
        using var commandKey = Registry.ClassesRoot.OpenSubKey(@"steam\shell\open\command");

        var command = commandKey?
                .GetValue(null)?
                .ToString();

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidOperationException(
                "Протокол steam:// не зарегистрирован. " +
                "Убедись, что Steam установлен.");
        }

        PrintSuccess("Протокол steam:// зарегистрирован.");
    }

    private static void CheckProcesses()
    {
        var runningProcesses = new List<string>();

        foreach (var processName in BenchmarkProcessNames)
        {
            var processes = Process.GetProcessesByName(processName);

            try
            {
                foreach (var process in processes)
                {
                    runningProcesses.Add($"{process.ProcessName} (PID {process.Id})");
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
        }

        if (runningProcesses.Count > 0)
        {
            throw new InvalidOperationException(
                "Перед запуском необходимо закрыть процессы: " +
                string.Join(", ", runningProcesses));
        }

        PrintSuccess("Wukong и PresentMon сейчас не запущены.");
    }

    private static void CheckPresentMonPermissions()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var principal = new WindowsPrincipal(identity);

        var isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);

        // Встроенная группа Windows:
        // Performance Log Users.
        var performanceLogUsersSid = new SecurityIdentifier("S-1-5-32-559");

        var isPerformanceLogUser = principal.IsInRole(performanceLogUsersSid);

        if (!isAdministrator && !isPerformanceLogUser)
        {
            throw new UnauthorizedAccessException(
                "Недостаточно прав для PresentMon. " +
                "Запусти терминал от имени администратора " +
                "или добавь пользователя в группу " +
                "\"Performance Log Users\".");
        }

        var accessMode = isAdministrator
                ? "администратор"
                : "Performance Log Users";

        PrintSuccess($"Права PresentMon подтверждены ({accessMode}).");
    }

    private static async Task CheckPresentMonAsync(CancellationToken cancellationToken)
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = "presentmon.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        startInfo.ArgumentList.Add("--help");

        Process? process;

        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "PresentMon не найден в PATH. " +
                "Установи Intel PresentMon Console " +
                "или добавь presentmon.exe в PATH.",
                exception);
        }

        if (process is null)
        {
            throw new InvalidOperationException("Не удалось запустить PresentMon.");
        }

        using (process)
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);

            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var output = stdout + Environment.NewLine + stderr;

            var presentMonDetected = output.Contains("PresentMon", StringComparison.OrdinalIgnoreCase) 
                && output.Contains("Capture Target Options", StringComparison.OrdinalIgnoreCase);

            if (!presentMonDetected)
            {
                throw new InvalidOperationException(
                    "PresentMon удалось запустить, " +
                    "но его вывод не удалось распознать." +
                    Environment.NewLine +
                    $"Код выхода: {process.ExitCode}" +
                    Environment.NewLine +
                    output);
            }
        }

        PrintSuccess("PresentMon найден и запускается.");
    }

    private static void CheckDirectoryWritable(string directory, string description)
    {
        directory = Path.GetFullPath(directory);

        Directory.CreateDirectory(directory);

        var probePath = Path.Combine(directory, $".wbr-write-test-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(probePath,"write-test");

            if (!File.Exists(probePath))
            {
                throw new IOException("Проверочный файл не был создан.");
            }
        }
        catch (Exception exception)
        {
            throw new IOException(
                $"Нет доступа на запись: {description} " +
                $"'{directory}'.",
                exception);
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch
            {
                // Ошибка удаления тестового файла не должна
                // скрывать настоящую ошибку.
            }
        }

        PrintSuccess($"{description} доступна для записи.");
    }

    private static void PrintSuccess(string message)
    {
        var text = $"Проверка: {message}";

        var width = Math.Max(_lastProgressLength, text.Length);

        Console.Write('\r');
        Console.Write(text.PadRight(width));

        _lastProgressLength = text.Length;
    }

    private static void CompleteProgress()
    {
        const string text =
            "Предварительная проверка успешно завершена.";

        var width = Math.Max(_lastProgressLength, text.Length);

        Console.Write('\r');
        Console.Write(text.PadRight(width));
        Console.WriteLine();

        _lastProgressLength = 0;
    }
}