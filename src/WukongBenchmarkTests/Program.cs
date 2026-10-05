using System.Text;
using System.Text.RegularExpressions;
using WukongBenchmarkTests.Configuration;

Console.OutputEncoding = Encoding.UTF8;

try
{
    if (args.Length == 2 && args[0] == "inspect")
    {
        bool enabled = GameSettingsFile.ReadFrameGeneration(args[1]);
        Console.WriteLine($"Генерация кадров в конфиге: {(enabled ? "включена" : "выключена")}");
        return 0;
    }

    if (args.Length == 3 && args[0] == "framegen" && args[2] is "on" or "off")
    {
        string? backup = GameSettingsFile.SetFrameGeneration(args[1], args[2] == "on");
        if (backup is null)
            Console.WriteLine("Нужное значение уже установлено. Файл не изменён.");
        else
        {
            Console.WriteLine($"InsertFrame записан: {(args[2] == "on" ? "1" : "0")}");
            Console.WriteLine($"Резервная копия: {backup}");
            Console.WriteLine("Применение настройки проверь при следующем запуске бенчмарка.");
        }
        return 0;
    }

    if (args.Length == 3 && args[0] == "restore")
    {
        string backup = GameSettingsFile.Restore(args[1], args[2]);
        Console.WriteLine("Конфиг восстановлен из указанной копии.");
        Console.WriteLine($"Состояние до восстановления сохранено: {backup}");
        return 0;
    }

    Console.WriteLine("Команды:");
    Console.WriteLine("  inspect <путь к GameUserSettings.ini>");
    Console.WriteLine("  framegen <путь к GameUserSettings.ini> on|off");
    Console.WriteLine("  restore <путь к GameUserSettings.ini> <путь к резервной копии>");
    Console.WriteLine("Перед изменением или восстановлением закрой бенчмарк.");
    return args.Length == 0 ? 0 : 2;
}
catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException
    or ArgumentException or RegexMatchTimeoutException)
{
    Console.Error.WriteLine($"Ошибка: {ex.Message}");
    return 1;
}
