using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WukongBenchmarkTests.Benchmarking;

public sealed class BenchmarkUiController
{
    // Координаты центра кнопки «Тест быстродействия»
    // были измерены при разрешении 2560x1600:
    // X = 217, Y = 482.
    //
    // Храним не абсолютные пиксели, а положение
    // относительно клиентской области окна.
    private const double BenchmarkButtonXRatio =
        437.0 / 2560.0;

    private const double BenchmarkButtonYRatio =
        700.0 / 1600.0;

    public async Task StartBenchmarkAsync(
        Process benchmarkProcess,
        TimeSpan windowTimeout,
        CancellationToken cancellationToken = default)
    {
        var windowHandle =
            await WaitForMainWindowAsync(
                benchmarkProcess,
                windowTimeout,
                cancellationToken);

        if (!GetCursorPos(out var originalCursorPosition))
        {
            throw new InvalidOperationException(
                "Не удалось получить текущую позицию курсора.");
        }

        try
        {
            // Восстанавливаем окно на случай,
            // если оно оказалось свёрнутым.
            ShowWindow(
                windowHandle,
                SW_RESTORE);

            if (!SetForegroundWindow(windowHandle))
            {
                throw new InvalidOperationException(
                    "Не удалось перевести окно Wukong на передний план.");
            }
            if (GetForegroundWindow() != windowHandle)
            {
                throw new InvalidOperationException(
                    "Wukong не стал активным окном. " +
                    "Отправлять ввод небезопасно.");
            }

            Console.WriteLine(
                "Ожидаем полной загрузки стартового экрана...");

            await WaitForInterfaceReadyAsync(
                windowHandle,
                TimeSpan.FromSeconds(21),
                cancellationToken);

            Console.WriteLine(
                "Стартовый экран должен быть готов.");

            Console.WriteLine(
                "Открываем главное меню...");

            SendEnter();

            Console.WriteLine(
                "Ждём загрузки главного меню...");

            await Task.Delay(
                TimeSpan.FromSeconds(2),
                cancellationToken);

            var benchmarkButtonPosition =
                GetBenchmarkButtonPosition(
                    windowHandle);

            Console.WriteLine(
                "Наводим курсор на кнопку «Тест быстродействия»...");

            if (!SetCursorPos(
                    benchmarkButtonPosition.X,
                    benchmarkButtonPosition.Y))
            {
                throw new InvalidOperationException(
                    "Не удалось переместить курсор на кнопку benchmark.");
            }

            // Даём интерфейсу время обработать hover-состояние кнопки.
            await Task.Delay(
                TimeSpan.FromMilliseconds(1500),
                cancellationToken);

            Console.WriteLine(
                "Открываем окно подтверждения запуска...");

            SendEnter();

            await Task.Delay(
                TimeSpan.FromSeconds(2),
                cancellationToken);

            Console.WriteLine(
                "Подтверждаем запуск benchmark...");

            SendEnter();

            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);

            Console.WriteLine(
                "Команда запуска benchmark отправлена.");
        }
        finally
        {
            // Возвращаем курсор туда, где он находился
            // до автоматизации интерфейса.
            SetCursorPos(
                originalCursorPosition.X,
                originalCursorPosition.Y);
        }
    }

    private static async Task WaitForInterfaceReadyAsync(
        IntPtr windowHandle,
        TimeSpan minimumWait,
        CancellationToken cancellationToken)
    {
        // Сам факт появления MainWindowHandle ещё не означает,
        // что Unreal Engine закончил загрузку интерфейса.
        //
        // Поэтому сначала ждём минимальное время загрузки,
        // а затем дополнительно убеждаемся, что окно существует
        // и имеет нормальную клиентскую область.

        var stopwatch =
            Stopwatch.StartNew();

        while (stopwatch.Elapsed < minimumWait)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var remaining =
                minimumWait - stopwatch.Elapsed;

            Console.Write(
                $"\rЗагрузка интерфейса: " +
                $"{Math.Ceiling(remaining.TotalSeconds),2:F0} с ");

            await Task.Delay(
                TimeSpan.FromMilliseconds(500),
                cancellationToken);
        }

        Console.WriteLine();

        if (!GetClientRect(
                windowHandle,
                out var clientRect))
        {
            throw new InvalidOperationException(
                "После ожидания не удалось получить размер окна Wukong.");
        }

        var width =
            clientRect.Right - clientRect.Left;

        var height =
            clientRect.Bottom - clientRect.Top;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException(
                "После ожидания окно Wukong имеет некорректный размер.");
        }
    }

    private static async Task<IntPtr> WaitForMainWindowAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
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
                throw new InvalidOperationException(
                    "Процесс Wukong завершился до появления главного окна.");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(250),
                cancellationToken);
        }

        throw new TimeoutException(
            $"Главное окно Wukong не появилось за " +
            $"{timeout.TotalSeconds:F0} секунд.");
    }

    private static POINT GetBenchmarkButtonPosition(
        IntPtr windowHandle)
    {
        if (!GetClientRect(
                windowHandle,
                out var clientRect))
        {
            throw new InvalidOperationException(
                "Не удалось получить размер клиентской области Wukong.");
        }

        var width =
            clientRect.Right - clientRect.Left;

        var height =
            clientRect.Bottom - clientRect.Top;

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException(
                "Окно Wukong имеет некорректный размер.");
        }

        var point =
            new POINT
            {
                X = (int)Math.Round(
                    width * BenchmarkButtonXRatio),

                Y = (int)Math.Round(
                    height * BenchmarkButtonYRatio)
            };

        if (!ClientToScreen(
                windowHandle,
                ref point))
        {
            throw new InvalidOperationException(
                "Не удалось преобразовать координаты кнопки в экранные.");
        }

        return point;
    }

    private static void SendEnter()
    {
        var scanCode =
            (ushort)MapVirtualKey(
                VK_RETURN,
                MAPVK_VK_TO_VSC);

        var inputs =
            new[]
            {
                new INPUT
                {
                    Type = INPUT_KEYBOARD,
                    Data = new INPUTUNION
                    {
                        Keyboard = new KEYBDINPUT
                        {
                            ScanCode = scanCode,
                            Flags = KEYEVENTF_SCANCODE
                        }
                    }
                },

                new INPUT
                {
                    Type = INPUT_KEYBOARD,
                    Data = new INPUTUNION
                    {
                        Keyboard = new KEYBDINPUT
                        {
                            ScanCode = scanCode,
                            Flags =
                                KEYEVENTF_SCANCODE |
                                KEYEVENTF_KEYUP
                        }
                    }
                }
            };

        var sent =
            SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<INPUT>());

        if (sent != inputs.Length)
        {
            var error =
                Marshal.GetLastWin32Error();

            throw new InvalidOperationException(
                $"Windows не удалось отправить Enter. " +
                $"Отправлено событий: {sent} из {inputs.Length}. " +
                $"Win32 error: {error}.");
        }
    }

    private const int SW_RESTORE = 9;

    private const uint INPUT_KEYBOARD = 1;

    private const ushort VK_RETURN = 0x0D;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private const uint MAPVK_VK_TO_VSC = 0;


    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }


    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public INPUTUNION Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;

        [FieldOffset(0)]
        public HARDWAREINPUT Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint Message;
        public ushort ParameterLow;
        public ushort ParameterHigh;
    }
    

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(
        out POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(
        int x,
        int y);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(
        IntPtr windowHandle,
        out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(
        IntPtr windowHandle,
        ref POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(
        IntPtr windowHandle,
        int command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint inputCount,
        INPUT[] inputs,
        int inputSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(
        uint code,
        uint mapType);

}