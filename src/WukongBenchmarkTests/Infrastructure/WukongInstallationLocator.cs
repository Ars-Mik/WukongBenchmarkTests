using System.Text.RegularExpressions;
using Microsoft.Win32;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Infrastructure;

public sealed class WukongInstallationLocator
{
    private const string SteamAppId = "3132990";

    private const string ManifestFileName = "appmanifest_3132990.acf";

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public WukongInstallation Find()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Поиск установки Wukong поддерживается только на Windows.");
        }

        var steamRoot = FindSteamRoot();
        var libraries = FindSteamLibraries(steamRoot);

        foreach (var libraryPath in libraries)
        {
            var steamAppsPath = Path.Combine(libraryPath, "steamapps");

            var manifestPath = Path.Combine(
                steamAppsPath,
                ManifestFileName);

            if (!File.Exists(manifestPath))
            {
                continue;
            }

            var installDirectoryName = ReadInstallDirectoryName(manifestPath);

            var rootPath = Path.Combine(
                steamAppsPath,
                "common",
                installDirectoryName);

            var executablePath = Path.Combine(
                rootPath,
                "b1_benchmark.exe");

            var gameSettingsPath = Path.Combine(
                rootPath,
                "b1",
                "Saved",
                "Config",
                "Windows",
                "GameUserSettings.ini");

            if (!File.Exists(executablePath))
            {
                throw new FileNotFoundException(
                    "Steam-манифест Wukong найден, " +
                    "но b1_benchmark.exe отсутствует.",
                    executablePath);
            }

            if (!File.Exists(gameSettingsPath))
            {
                throw new FileNotFoundException(
                    "Wukong найден, но GameUserSettings.ini отсутствует. " +
                    "Возможно, benchmark ещё ни разу не запускался.",
                    gameSettingsPath);
            }

            return new WukongInstallation
            {
                RootPath = Path.GetFullPath(rootPath),
                ExecutablePath = Path.GetFullPath(executablePath),
                GameSettingsPath = Path.GetFullPath(gameSettingsPath)
            };
        }

        throw new DirectoryNotFoundException(
            $"Black Myth: Wukong Benchmark Tool " +
            $"(Steam AppID {SteamAppId}) не найден " +
            "ни в одной библиотеке Steam.");
    }

    private static string FindSteamRoot()
    {
        string? steamPath = ReadRegistryString(
            Registry.CurrentUser,
            @"Software\Valve\Steam",
            "SteamPath");

        steamPath ??= ReadRegistryString(
            Registry.LocalMachine,
            @"SOFTWARE\WOW6432Node\Valve\Steam",
            "InstallPath");

        steamPath ??= ReadRegistryString(
            Registry.LocalMachine,
            @"SOFTWARE\Valve\Steam",
            "InstallPath");

        if (string.IsNullOrWhiteSpace(steamPath))
        {
            throw new DirectoryNotFoundException(
                "Не удалось определить папку установки Steam.");
        }

        steamPath = Path.GetFullPath(
            steamPath.Replace(
                '/',
                Path.DirectorySeparatorChar));

        if (!Directory.Exists(steamPath))
        {
            throw new DirectoryNotFoundException(
                $"Папка Steam не существует: {steamPath}");
        }

        return steamPath;
    }

    private static IReadOnlyList<string> FindSteamLibraries(string steamRoot)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            steamRoot
        };

        var libraryFile = Path.Combine(
            steamRoot,
            "steamapps",
            "libraryfolders.vdf");

        if (!File.Exists(libraryFile))
        {
            return libraries.ToArray();
        }

        var text = File.ReadAllText(libraryFile);

        var matches = Regex.Matches(
            text,
            "\"path\"\\s*\"(?<path>[^\"]+)\"",
            RegexOptions.IgnoreCase,
            RegexTimeout);

        foreach (Match match in matches)
        {
            var value = match.Groups["path"].Value;

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // В VDF обратные слэши могут быть экранированы.
            value = value.Replace(@"\\", @"\");

            value = value.Replace(
                '/',
                Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(value);

            if (Directory.Exists(fullPath))
            {
                libraries.Add(fullPath);
            }
        }

        return libraries.ToArray();
    }

    private static string ReadInstallDirectoryName(string manifestPath)
    {
        var text = File.ReadAllText(manifestPath);

        var match = Regex.Match(text,
            "\"installdir\"\\s*\"(?<name>[^\"]+)\"",
            RegexOptions.IgnoreCase,
            RegexTimeout);

        if (!match.Success)
        {
            throw new InvalidDataException(
                $"В Steam-манифесте не найден installdir: {manifestPath}");
        }

        var installDirectoryName = match.Groups["name"].Value.Trim();

        if (string.IsNullOrWhiteSpace(installDirectoryName))
        {
            throw new InvalidDataException(
                "Steam-манифест содержит пустой installdir.");
        }

        return installDirectoryName;
    }

    private static string? ReadRegistryString(RegistryKey root, string subKeyPath, string valueName)
    {
        using var key = root.OpenSubKey(subKeyPath);

        return key?
            .GetValue(valueName)?
            .ToString();
    }
}