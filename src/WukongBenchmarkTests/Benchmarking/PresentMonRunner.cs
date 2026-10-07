using System.Diagnostics;
using System.Text;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class PresentMonRunner
{
    private const string ExecutableName =
        "presentmon.exe";

    private const string SessionName =
        "WukongBenchmarkTests";

    private Process? _captureProcess;
    private StreamWriter? _csvWriter;

    private Task? _stdoutPumpTask;
    private Task? _stderrPumpTask;

    public void StartCapture(
        string outputPath,
        string? processName = null)
    {
        if (_captureProcess is not null &&
            !_captureProcess.HasExited)
        {
            throw new InvalidOperationException(
                "PresentMon уже выполняет захват.");
        }

        var fullOutputPath =
            Path.GetFullPath(outputPath);

        var directory =
            Path.GetDirectoryName(fullOutputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // После аварийного завершения предыдущего запуска
        // могла остаться активная ETW-сессия PresentMon.
        StopExistingCaptureIfAny();

        // Старый CSV мог ещё некоторое время удерживаться
        // предыдущим экземпляром PresentMon.
        DeleteFileWithRetry(
            fullOutputPath);

        var fileStream =
            new FileStream(
                fullOutputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite);

        _csvWriter =
            new StreamWriter(
                fileStream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false))
            {
                // Каждая полученная строка сразу становится
                // доступна BenchmarkCompletionDetector.
                AutoFlush = true
            };

        var startInfo =
            new ProcessStartInfo
            {
                FileName = ExecutableName,
                UseShellExecute = false,
                CreateNoWindow = true,

                // CSV получаем не через --output_file,
                // а как живой поток из stdout.
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        startInfo.ArgumentList.Add(
            "--session_name");

        startInfo.ArgumentList.Add(
            SessionName);

        if (!string.IsNullOrWhiteSpace(processName))
        {
            startInfo.ArgumentList.Add(
                "--process_name");

            startInfo.ArgumentList.Add(
                processName);
        }

        startInfo.ArgumentList.Add(
            "--exclude_dropped");

        startInfo.ArgumentList.Add(
            "--v2_metrics");

        startInfo.ArgumentList.Add(
            "--output_stdout");

        _captureProcess =
            Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Не удалось запустить PresentMon.");

        // Читаем stdout в фоне, чтобы StartCapture()
        // сразу вернул управление основной программе.
        _stdoutPumpTask =
            PumpStdoutAsync(
                _captureProcess,
                _csvWriter);

        // stderr обязательно тоже читаем:
        // иначе заполненный pipe теоретически может
        // заблокировать дочерний процесс.
        _stderrPumpTask =
            DrainStderrAsync(
                _captureProcess);
    }

    public async Task StopCaptureAsync()
    {
        StopSession();

        if (_captureProcess is not null)
        {
            try
            {
                await _captureProcess
                    .WaitForExitAsync();
            }
            catch (InvalidOperationException)
            {
                // Процесс уже мог завершиться самостоятельно.
            }
        }

        if (_stdoutPumpTask is not null)
        {
            await _stdoutPumpTask;
        }

        if (_stderrPumpTask is not null)
        {
            await _stderrPumpTask;
        }

        if (_csvWriter is not null)
        {
            await _csvWriter.FlushAsync();
            _csvWriter.Dispose();
        }

        _captureProcess?.Dispose();

        _captureProcess = null;
        _csvWriter = null;
        _stdoutPumpTask = null;
        _stderrPumpTask = null;
    }

    private static async Task PumpStdoutAsync(
        Process process,
        StreamWriter writer)
    {
        while (true)
        {
            var line =
                await process.StandardOutput
                    .ReadLineAsync();

            if (line is null)
            {
                break;
            }

            await writer.WriteLineAsync(
                line);

            // AutoFlush уже включён, но здесь намеренно
            // оставляем явный flush: live-детектор должен
            // видеть строку максимально быстро.
            await writer.FlushAsync();
        }
    }

    private static async Task DrainStderrAsync(
        Process process)
    {
        while (true)
        {
            var line =
                await process.StandardError
                    .ReadLineAsync();

            if (line is null)
            {
                break;
            }

            // Ошибки PresentMon пока выводим в нашу консоль,
            // чтобы они не терялись при диагностике.
            Console.Error.WriteLine(
                $"PresentMon: {line}");
        }
    }

    private static void StopSession()
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = ExecutableName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        startInfo.ArgumentList.Add(
            "--session_name");

        startInfo.ArgumentList.Add(
            SessionName);

        startInfo.ArgumentList.Add(
            "--terminate_existing_session");

        using var process =
            Process.Start(startInfo);

        if (process is null)
        {
            return;
        }

        var standardOutput =
            process.StandardOutput.ReadToEnd();

        var standardError =
            process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0 &&
            !standardError.Contains(
                "no existing sessions found",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Не удалось остановить PresentMon." +
                $"{Environment.NewLine}" +
                $"Код выхода: {process.ExitCode}" +
                $"{Environment.NewLine}" +
                standardOutput +
                Environment.NewLine +
                standardError);
        }
    }

    private static void StopExistingCaptureIfAny()
    {
        var startInfo =
            new ProcessStartInfo
            {
                FileName = ExecutableName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

        startInfo.ArgumentList.Add(
            "--session_name");

        startInfo.ArgumentList.Add(
            SessionName);

        startInfo.ArgumentList.Add(
            "--terminate_existing_session");

        using var process =
            Process.Start(startInfo);

        if (process is null)
        {
            return;
        }

        // Обязательно считываем оба pipe до ожидания завершения.
        _ = process.StandardOutput.ReadToEnd();
        _ = process.StandardError.ReadToEnd();

        process.WaitForExit();

        // Ненулевой код здесь допустим:
        // чаще всего он означает, что старой сессии не было.
    }

    private static void DeleteFileWithRetry(
        string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        const int attempts = 15;

        for (var attempt = 1;
             attempt <= attempts;
             attempt++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException)
                when (attempt < attempts)
            {
                Thread.Sleep(200);
            }
        }

        throw new IOException(
            $"Не удалось удалить старый файл PresentMon: {path}");
    }
}