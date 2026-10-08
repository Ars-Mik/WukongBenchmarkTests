using System.Text;
using WukongBenchmarkTests.Configuration;

namespace WukongBenchmarkTests.Tests;

public sealed class GameSettingsFileTests
{
    [Fact]
    public void SetFrameGeneration_AndRestore_PreservesOriginalFile()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"wukong-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);

        var configPath = Path.Combine(
            directory,
            "GameUserSettings.ini");

        const string originalText =
            """
            [/Script/GSGameSettings.GSGameUserSettings]
            UISettingData=(("InsertFrame","0"))
            """;

        var originalBytes = Encoding.UTF8.GetBytes(originalText);

        File.WriteAllBytes(configPath, originalBytes);

        string? generatedBackup = null;
        string? restoreBackup = null;

        try
        {
            Assert.False(
                GameSettingsFile.ReadFrameGeneration(configPath));

            generatedBackup = GameSettingsFile.SetFrameGeneration(
                configPath,
                enabled: true);

            Assert.NotNull(generatedBackup);

            Assert.True(
                GameSettingsFile.ReadFrameGeneration(configPath));

            restoreBackup = GameSettingsFile.Restore(
                configPath,
                generatedBackup!);

            var restoredBytes = File.ReadAllBytes(configPath);

            Assert.True(
                originalBytes.AsSpan().SequenceEqual(restoredBytes));

            Assert.False(
                GameSettingsFile.ReadFrameGeneration(configPath));
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(restoreBackup) &&
                File.Exists(restoreBackup))
            {
                File.Delete(restoreBackup);
            }

            if (!string.IsNullOrWhiteSpace(generatedBackup) &&
                File.Exists(generatedBackup))
            {
                File.Delete(generatedBackup);
            }

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}