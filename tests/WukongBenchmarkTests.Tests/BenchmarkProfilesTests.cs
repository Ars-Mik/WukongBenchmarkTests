using WukongBenchmarkTests.Benchmarking;

namespace WukongBenchmarkTests.Tests;

public sealed class BenchmarkProfilesTests
{
    [Fact]
    public void CreateCpuProfile_ReturnsCpuOrientedSettings()
    {
        var profile = BenchmarkProfiles.CreateCpuProfile(2560, 1600);

        Assert.Equal(2560, profile.ResolutionWidth);
        Assert.Equal(1600, profile.ResolutionHeight);
        Assert.Equal(50, profile.ResolutionScalePercent);
        Assert.Equal(0, profile.QualityLevel);
        Assert.Equal(0, profile.ScalabilityLevel);
        Assert.Equal(0, profile.MotionBlurLevel);
        Assert.Equal(0, profile.FrameRateLimit);

        Assert.False(profile.VSyncEnabled);
        Assert.False(profile.DynamicResolutionEnabled);
        Assert.False(profile.FrameGenerationEnabled);
        Assert.False(profile.RayTracingEnabled);
    }

    [Fact]
    public void CreateGpuProfile_ReturnsGpuOrientedSettings()
    {
        var profile = BenchmarkProfiles.CreateGpuProfile(2560, 1600);

        Assert.Equal(2560, profile.ResolutionWidth);
        Assert.Equal(1600, profile.ResolutionHeight);
        Assert.Equal(100, profile.ResolutionScalePercent);
        Assert.Equal(4, profile.QualityLevel);
        Assert.Equal(3, profile.ScalabilityLevel);
        Assert.Equal(2, profile.MotionBlurLevel);
        Assert.Equal(0, profile.FrameRateLimit);

        Assert.False(profile.VSyncEnabled);
        Assert.False(profile.DynamicResolutionEnabled);
        Assert.False(profile.FrameGenerationEnabled);
        Assert.False(profile.RayTracingEnabled);
    }
}