namespace WukongBenchmarkTests.Models;

public sealed record BenchmarkProfile
{
    public required string Name { get; init; }

    public required int ResolutionWidth { get; init; }

    public required int ResolutionHeight { get; init; }

    public required double ResolutionScalePercent { get; init; }

    // Значение общего качества из UI Wukong:
    // 0 — минимальное, 4 — ультра.
    public required int QualityLevel { get; init; }

    // Значение Unreal Scalability:
    // 0 — Low, 1 — Medium, 2 — High, 3 — Epic.
    public required int ScalabilityLevel { get; init; }

    public required bool VSyncEnabled { get; init; }

    public required bool DynamicResolutionEnabled { get; init; }

    public required bool FrameGenerationEnabled { get; init; }

    public required bool RayTracingEnabled { get; init; }

    public required double FrameRateLimit { get; init; }

    public required int MotionBlurLevel { get; init; }
}