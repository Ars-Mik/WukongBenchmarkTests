using System.Text;
using System.Text.RegularExpressions;

namespace WukongBenchmarkTests.Configuration;

/// <summary>Изменяет только проверенный параметр InsertFrame в конфиге бенчмарка.</summary>
public static class GameSettingsFile
{
    private const string SectionName = "/Script/GSGameSettings.GSGameUserSettings";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static bool ReadFrameGeneration(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return bytes[FindValueOffset(bytes)] == (byte)'1';
    }

    // null означает, что нужное значение уже установлено: запись не требуется.
    public static string? SetFrameGeneration(string path, bool enabled)
    {
        path = Path.GetFullPath(path);
        byte[] original = File.ReadAllBytes(path);
        int offset = FindValueOffset(original);
        byte value = enabled ? (byte)'1' : (byte)'0';

        if (original[offset] == value)
            return null;

        byte[] updated = (byte[])original.Clone();
        updated[offset] = value;
        return ReplaceWithBackup(path, original, updated);
    }

    public static string Restore(string path, string backupPath)
    {
        path = Path.GetFullPath(path);
        backupPath = Path.GetFullPath(backupPath);
        if (string.Equals(path, backupPath, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Конфиг и резервная копия должны быть разными файлами.");

        byte[] restored = File.ReadAllBytes(backupPath);
        _ = FindValueOffset(restored); // Проверяем, что это поддерживаемый конфиг.
        return ReplaceWithBackup(path, File.ReadAllBytes(path), restored);
    }

    private static int FindValueOffset(byte[] bytes)
    {
        int bomLength = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        string text = StrictUtf8.GetString(bytes, bomLength, bytes.Length - bomLength);
        MatchCollection headers = Regex.Matches(text,
            @"(?m)^[ \t]*\[(?<name>[^\]\r\n]+)\][ \t]*\r?$", RegexOptions.None,
            TimeSpan.FromSeconds(1));

        var sections = headers.Cast<Match>()
            .Where(m => m.Groups["name"].Value == SectionName).ToArray();
        if (sections.Length != 1)
            throw new InvalidDataException("Ожидалась ровно одна секция настроек GSGameUserSettings.");

        int start = sections[0].Index + sections[0].Length;
        int end = headers.Cast<Match>().FirstOrDefault(m => m.Index >= start)?.Index ?? text.Length;
        string section = text[start..end];

        MatchCollection lines = Regex.Matches(section,
            @"(?m)^[ \t]*UISettingData[ \t]*=[^\r\n]*", RegexOptions.None,
            TimeSpan.FromSeconds(1));
        if (lines.Count != 1)
            throw new InvalidDataException("Ожидалась ровно одна строка UISettingData.");

        MatchCollection entries = Regex.Matches(lines[0].Value,
            "\\(\"InsertFrame\",[ \\t]*\"(?<value>[^\"]*)\"\\)", RegexOptions.None,
            TimeSpan.FromSeconds(1));
        if (entries.Count != 1 || entries[0].Groups["value"].Value is not ("0" or "1"))
            throw new InvalidDataException("Ожидался один InsertFrame со значением 0 или 1.");

        int charOffset = start + lines[0].Index + entries[0].Groups["value"].Index;
        return bomLength + StrictUtf8.GetByteCount(text.AsSpan(0, charOffset));
    }

    private static string ReplaceWithBackup(string path, byte[] original, byte[] updated)
    {
        string suffix = $"wbr-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
        string temporaryPath = $"{path}.{suffix}.tmp";
        string backupPath = $"{path}.{suffix}.bak";

        try
        {
            File.WriteAllBytes(temporaryPath, updated);
            // Бенчмарк должен быть закрыт. Дополнительно обнаруживаем изменение
            // файла между чтением и этой проверкой; это не блокировка процесса игры.
            if (!File.ReadAllBytes(path).AsSpan().SequenceEqual(original))
                throw new IOException("Конфиг изменился во время операции. Закрой бенчмарк и повтори.");

            File.Replace(temporaryPath, path, backupPath);
            return backupPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
