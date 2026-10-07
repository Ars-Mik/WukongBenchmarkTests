using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.Benchmarking;

public static class BenchmarkProfiles
{
    public static BenchmarkProfile CreateCpuProfile(
        int nativeWidth,
        int nativeHeight)
    {

        return new BenchmarkProfile
        {
            Name = "CPU",

            ResolutionWidth = nativeWidth,
            ResolutionHeight = nativeHeight,

            ResolutionScalePercent = 50.0,

            QualityLevel = 0,
            ScalabilityLevel = 0,

            VSyncEnabled = false,
            DynamicResolutionEnabled = false,
            FrameGenerationEnabled = false,
            RayTracingEnabled = false,

            FrameRateLimit = 0.0,

            MotionBlurLevel = 0
        };
    }

    public static BenchmarkProfile CreateGpuProfile(
        int nativeWidth,
        int nativeHeight)
    {
        // GPU-тест выполняется в родном разрешении
        // с максимальными обычными настройками графики.
        //
        // Frame Generation выключаем, чтобы в результат
        // не попадали синтезированные кадры.
        //
        // Ray Tracing оставляем выключенным:
        // профиль должен оставаться применимым не только
        // к видеокартам с аппаратной трассировкой лучей.
        return new BenchmarkProfile
        {
            Name = "GPU",

            ResolutionWidth = nativeWidth,
            ResolutionHeight = nativeHeight,

            ResolutionScalePercent = 100.0,

            QualityLevel = 4,
            ScalabilityLevel = 3,

            VSyncEnabled = false,
            DynamicResolutionEnabled = false,
            FrameGenerationEnabled = false,
            RayTracingEnabled = false,

            FrameRateLimit = 0.0,

            MotionBlurLevel = 2
        };
    }
}