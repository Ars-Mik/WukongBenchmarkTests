using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Configuration;

public static class GameSettingsFile
{
    private const string SectionName = "/Script/GSGameSettings.GSGameUserSettings";

    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public static bool ReadFrameGeneration(string path)
    {
        var bytes = File.ReadAllBytes(path);

        return bytes[FindValueOffset(bytes)] == (byte)'1';
    }

    // null означает, что нужное значение уже установлено:
    // запись файла не требуется.
    public static string? SetFrameGeneration(string path, bool enabled)
    {
        path = Path.GetFullPath(path);

        var original = File.ReadAllBytes(path);
        var offset = FindValueOffset(original);

        var value = enabled
            ? (byte)'1'
            : (byte)'0';

        if (original[offset] == value)
        {
            return null;
        }

        var updated = (byte[])original.Clone();

        updated[offset] = value;

        return ReplaceWithBackup(
            path,
            original,
            updated);
    }

    public static string? ApplyProfile(
        string path,
        BenchmarkProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        ValidateProfile(profile);

        path = Path.GetFullPath(path);

        var original = File.ReadAllBytes(path);
        var hasBom = HasUtf8Bom(original);
        var text = DecodeUtf8(original, hasBom);

        // Внутреннее разрешение рендера.
        var desiredWidth = (int)Math.Round(
            profile.ResolutionWidth *
            profile.ResolutionScalePercent /
            100.0);

        var desiredHeight = (int)Math.Round(
            profile.ResolutionHeight *
            profile.ResolutionScalePercent /
            100.0);

        // В UISettingData значение ImageQuality хранится
        // как процент render scale, умноженный на 16.
        //
        // Проверенный пример из текущего конфига:
        // 67% - 1072.
        var imageQuality = (int)Math.Round(
            profile.ResolutionScalePercent * 16.0);

        var boolVSync = profile.VSyncEnabled
            ? "True"
            : "False";

        var boolDynamicResolution = profile.DynamicResolutionEnabled
            ? "True"
            : "False";

        var boolRayTracing = profile.RayTracingEnabled
            ? "True"
            : "False";

        var uiVSync = profile.VSyncEnabled
            ? "1"
            : "0";

        var uiFrameGeneration = profile.FrameGenerationEnabled
            ? "1"
            : "0";

        var uiRayTracing = profile.RayTracingEnabled
            ? "1"
            : "0";

        var uiFrameRateLock = profile.FrameRateLimit > 0
            ? "1"
            : "0";

        var frameRateLimit = profile.FrameRateLimit.ToString(
            "0.000000",
            CultureInfo.InvariantCulture);

        var resolutionScale = profile.ResolutionScalePercent.ToString(
            "0.0000000",
            CultureInfo.InvariantCulture);

        // Основные параметры GSGameUserSettings.
        text = ReplaceLineValue(
            text,
            "DesiredScreenWidth",
            desiredWidth.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "DesiredScreenHeight",
            desiredHeight.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(text, "bUseVSync", boolVSync);
        text = ReplaceLineValue(text, "bUseDynamicResolution", boolDynamicResolution);

        text = ReplaceLineValue(
            text,
            "ResolutionSizeX",
            profile.ResolutionWidth.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "ResolutionSizeY",
            profile.ResolutionHeight.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "LastUserConfirmedResolutionSizeX",
            profile.ResolutionWidth.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "LastUserConfirmedResolutionSizeY",
            profile.ResolutionHeight.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "LastUserConfirmedDesiredScreenWidth",
            desiredWidth.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(
            text,
            "LastUserConfirmedDesiredScreenHeight",
            desiredHeight.ToString(CultureInfo.InvariantCulture));

        text = ReplaceLineValue(text, "FrameRateLimit", frameRateLimit);

        // UISettingData.
        text = ReplaceUiSettingValue(text, "LockFrameRate", uiFrameRateLock);
        text = ReplaceUiSettingValue(text, "Vsync", uiVSync);

        text = ReplaceUiSettingValue(
            text,
            "MotionBlur",
            profile.MotionBlurLevel.ToString(CultureInfo.InvariantCulture));

        text = ReplaceUiSettingValue(
            text,
            "ImageQuality",
            imageQuality.ToString(CultureInfo.InvariantCulture));

        text = ReplaceUiSettingValue(text, "InsertFrame", uiFrameGeneration);
        text = ReplaceUiSettingValue(text, "Rtx", uiRayTracing);

        text = ReplaceUiSettingValue(
            text,
            "QualityLevel",
            profile.QualityLevel.ToString(CultureInfo.InvariantCulture));

        var uiQuality = profile.QualityLevel.ToString(CultureInfo.InvariantCulture);

        text = ReplaceUiSettingValue(text, "ViewDistance", uiQuality);
        text = ReplaceUiSettingValue(text, "AntiAliasing", uiQuality);
        text = ReplaceUiSettingValue(text, "PostProcessing", uiQuality);
        text = ReplaceUiSettingValue(text, "ShadowQuality", uiQuality);
        text = ReplaceUiSettingValue(text, "TextureQuality", uiQuality);
        text = ReplaceUiSettingValue(text, "FxQuality", uiQuality);
        text = ReplaceUiSettingValue(text, "MaterialQuality", uiQuality);
        text = ReplaceUiSettingValue(text, "VegetationQuality", uiQuality);
        text = ReplaceUiSettingValue(text, "GlobalIllumination", uiQuality);
        text = ReplaceUiSettingValue(text, "ReflectionQuality", uiQuality);

        // Unreal Scalability.
        var scalability = profile.ScalabilityLevel.ToString(CultureInfo.InvariantCulture);

        text = ReplaceLineValue(text, "sg.ResolutionQuality", resolutionScale);
        text = ReplaceLineValue(text, "sg.ViewDistanceQuality", scalability);
        text = ReplaceLineValue(text, "sg.AntiAliasingQuality", scalability);
        text = ReplaceLineValue(text, "sg.ShadowQuality", scalability);
        text = ReplaceLineValue(text, "sg.GlobalIlluminationQuality", scalability);
        text = ReplaceLineValue(text, "sg.ReflectionQuality", scalability);
        text = ReplaceLineValue(text, "sg.PostProcessQuality", scalability);
        text = ReplaceLineValue(text, "sg.TextureQuality", scalability);
        text = ReplaceLineValue(text, "sg.EffectsQuality", scalability);
        text = ReplaceLineValue(text, "sg.FoliageQuality", scalability);
        text = ReplaceLineValue(text, "sg.ShadingQuality", scalability);

        var rayTracingQuality = profile.RayTracingEnabled
            ? scalability
            : "0";

        text = ReplaceLineValue(text, "sg.RayTracingQuality", rayTracingQuality);
        text = ReplaceLineValue(text, "r.RayTracing.EnableInGame", boolRayTracing);

        var updated = EncodeUtf8(text, hasBom);

        if (updated.AsSpan().SequenceEqual(original))
        {
            return null;
        }

        return ReplaceWithBackup(
            path,
            original,
            updated);
    }

    public static string Restore(
        string path,
        string backupPath)
    {
        path = Path.GetFullPath(path);
        backupPath = Path.GetFullPath(backupPath);

        if (string.Equals(
                path,
                backupPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Конфиг и резервная копия должны быть разными файлами.");
        }

        var restored = File.ReadAllBytes(backupPath);

        // Проверяем, что backup всё ещё является
        // поддерживаемым GameUserSettings.ini.
        _ = FindValueOffset(restored);

        return ReplaceWithBackup(
            path,
            File.ReadAllBytes(path),
            restored);
    }

    private static void ValidateProfile(BenchmarkProfile profile)
    {
        if (profile.ResolutionWidth <= 0 || profile.ResolutionHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "Разрешение профиля должно быть больше нуля.");
        }

        if (profile.ResolutionScalePercent <= 0 ||
            profile.ResolutionScalePercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "Render scale должен находиться в диапазоне 0–100%.");
        }

        if (profile.QualityLevel is < 0 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "QualityLevel должен находиться в диапазоне 0–4.");
        }

        if (profile.ScalabilityLevel is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "ScalabilityLevel должен находиться в диапазоне 0–3.");
        }

        if (profile.MotionBlurLevel is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "MotionBlurLevel должен находиться в диапазоне 0–2.");
        }

        if (profile.FrameRateLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(profile),
                "Ограничение FPS не может быть отрицательным.");
        }
    }

    private static string ReplaceLineValue(
        string text,
        string key,
        string newValue)
    {
        var pattern =
            $@"(?m)^(?<prefix>[ \t]*{Regex.Escape(key)}[ \t]*=[ \t]*)(?<value>[^\r\n]*)(?<ending>\r?)$";

        var matches = Regex.Matches(
            text,
            pattern,
            RegexOptions.None,
            RegexTimeout);

        if (matches.Count != 1)
        {
            throw new InvalidDataException(
                $"Ожидалась ровно одна строка '{key}', " +
                $"найдено: {matches.Count}.");
        }

        var valueGroup = matches[0].Groups["value"];

        return ReplaceRange(
            text,
            valueGroup.Index,
            valueGroup.Length,
            newValue);
    }

    private static string ReplaceUiSettingValue(
        string text,
        string key,
        string newValue)
    {
        var pattern =
            $"\\(\"{Regex.Escape(key)}\",[ \\t]*\"(?<value>[^\"]*)\"\\)";

        var matches = Regex.Matches(
            text,
            pattern,
            RegexOptions.None,
            RegexTimeout);

        if (matches.Count != 1)
        {
            throw new InvalidDataException(
                $"Ожидался ровно один параметр UISettingData '{key}', " +
                $"найдено: {matches.Count}.");
        }

        var valueGroup = matches[0].Groups["value"];

        return ReplaceRange(
            text,
            valueGroup.Index,
            valueGroup.Length,
            newValue);
    }

    private static string ReplaceRange(
        string text,
        int index,
        int length,
        string replacement)
    {
        return string.Concat(
            text.AsSpan(0, index),
            replacement,
            text.AsSpan(index + length));
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3 &&
               bytes[0] == 0xEF &&
               bytes[1] == 0xBB &&
               bytes[2] == 0xBF;
    }

    private static string DecodeUtf8(
        byte[] bytes,
        bool hasBom)
    {
        var offset = hasBom
            ? 3
            : 0;

        return StrictUtf8.GetString(
            bytes,
            offset,
            bytes.Length - offset);
    }

    private static byte[] EncodeUtf8(
        string text,
        bool includeBom)
    {
        var content = StrictUtf8.GetBytes(text);

        if (!includeBom)
        {
            return content;
        }

        var result = new byte[3 + content.Length];

        result[0] = 0xEF;
        result[1] = 0xBB;
        result[2] = 0xBF;

        content.CopyTo(result, 3);

        return result;
    }

    private static int FindValueOffset(byte[] bytes)
    {
        var bomLength = HasUtf8Bom(bytes)
            ? 3
            : 0;

        var text = StrictUtf8.GetString(
            bytes,
            bomLength,
            bytes.Length - bomLength);

        var headers = Regex.Matches(
            text,
            @"(?m)^[ \t]*\[(?<name>[^\]\r\n]+)\][ \t]*\r?$",
            RegexOptions.None,
            RegexTimeout);

        var sections = headers
            .Cast<Match>()
            .Where(match => match.Groups["name"].Value == SectionName)
            .ToArray();

        if (sections.Length != 1)
        {
            throw new InvalidDataException(
                "Ожидалась ровно одна секция настроек GSGameUserSettings.");
        }

        var start = sections[0].Index + sections[0].Length;

        var end = headers
            .Cast<Match>()
            .FirstOrDefault(match => match.Index >= start)
            ?.Index
            ?? text.Length;

        var section = text[start..end];

        var lines = Regex.Matches(
            section,
            @"(?m)^[ \t]*UISettingData[ \t]*=[^\r\n]*",
            RegexOptions.None,
            RegexTimeout);

        if (lines.Count != 1)
        {
            throw new InvalidDataException(
                "Ожидалась ровно одна строка UISettingData.");
        }

        var entries = Regex.Matches(
            lines[0].Value,
            "\\(\"InsertFrame\",[ \\t]*\"(?<value>[^\"]*)\"\\)",
            RegexOptions.None,
            RegexTimeout);

        if (entries.Count != 1 ||
            entries[0].Groups["value"].Value is not ("0" or "1"))
        {
            throw new InvalidDataException(
                "Ожидался один InsertFrame со значением 0 или 1.");
        }

        var charOffset =
            start +
            lines[0].Index +
            entries[0].Groups["value"].Index;

        return bomLength +
               StrictUtf8.GetByteCount(
                   text.AsSpan(0, charOffset));
    }

    private static string ReplaceWithBackup(
        string path,
        byte[] original,
        byte[] updated)
    {
        var suffix =
            $"wbr-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";

        var temporaryPath = $"{path}.{suffix}.tmp";
        var backupPath = $"{path}.{suffix}.bak";

        try
        {
            File.WriteAllBytes(temporaryPath, updated);

            // Бенчмарк должен быть закрыт.
            // Дополнительно обнаруживаем изменение файла
            // между чтением и заменой.
            if (!File.ReadAllBytes(path)
                    .AsSpan()
                    .SequenceEqual(original))
            {
                throw new IOException(
                    "Конфиг изменился во время операции. " +
                    "Закрой бенчмарк и повтори.");
            }

            File.Replace(
                temporaryPath,
                path,
                backupPath);

            return backupPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}