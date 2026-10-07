using System.Management;
using System.Runtime.InteropServices;
using WukongBenchmarkTests.Models;

namespace WukongBenchmarkTests.System;

public sealed class SystemInfoCollector
{

    public SystemInfo Collect()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Сбор характеристик системы поддерживается только на Windows.");
        }

        var cpu =
            ReadCpu();

        var gpus =
            ReadGpus();

        var totalMemoryBytes =
            ReadTotalMemory();

        var displayResolution =
            ReadPrimaryDisplayResolution();

        return new SystemInfo
        {
            CpuName = cpu.Name,

            CpuCores = cpu.Cores,

            CpuLogicalProcessors =
                cpu.LogicalProcessors,

            Gpus = gpus,

            TotalMemoryBytes =
                totalMemoryBytes,

            TotalMemoryGigabytes =
                Math.Round(
                    totalMemoryBytes /
                    1024d /
                    1024d /
                    1024d,
                    2),

            OperatingSystem =
                RuntimeInformation.OSDescription,

            OsArchitecture =
                RuntimeInformation.OSArchitecture.ToString(),

            ScreenWidth =
                displayResolution.Width,

            ScreenHeight =
                displayResolution.Height
        };
    }

    private static CpuData ReadCpu()
    {
        using var searcher =
            new ManagementObjectSearcher(
                "SELECT Name, NumberOfCores, " +
                "NumberOfLogicalProcessors " +
                "FROM Win32_Processor");

        using var results =
            searcher.Get();

        foreach (ManagementObject processor in results)
        {
            using (processor)
            {
                var name =
                    processor["Name"]?
                        .ToString()?
                        .Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var cores =
                    Convert.ToInt32(
                        processor["NumberOfCores"]);

                var logicalProcessors =
                    Convert.ToInt32(
                        processor["NumberOfLogicalProcessors"]);

                return new CpuData(
                    name,
                    cores,
                    logicalProcessors);
            }
        }

        throw new InvalidOperationException(
            "Не удалось получить информацию о процессоре.");
    }

    private static IReadOnlyList<GpuInfo> ReadGpus()
    {
        var gpus =
            new List<GpuInfo>();

        using var searcher =
            new ManagementObjectSearcher(
                "SELECT Name, DriverVersion " +
                "FROM Win32_VideoController");

        using var results =
            searcher.Get();

        foreach (ManagementObject adapter in results)
        {
            using (adapter)
            {
                var name =
                    adapter["Name"]?
                        .ToString()?
                        .Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var driverVersion =
                    adapter["DriverVersion"]?
                        .ToString()?
                        .Trim();

                gpus.Add(
                    new GpuInfo
                    {
                        Name = name,
                        DriverVersion =
                            string.IsNullOrWhiteSpace(
                                driverVersion)
                                ? null
                                : driverVersion
                    });
            }
        }

        if (gpus.Count == 0)
        {
            throw new InvalidOperationException(
                "Не удалось получить информацию о видеокарте.");
        }

        return gpus;
    }

    private static ulong ReadTotalMemory()
    {
        using var searcher =
            new ManagementObjectSearcher(
                "SELECT TotalPhysicalMemory " +
                "FROM Win32_ComputerSystem");

        using var results =
            searcher.Get();

        foreach (ManagementObject computer in results)
        {
            using (computer)
            {
                var value =
                    computer[
                        "TotalPhysicalMemory"];

                if (value is null)
                {
                    continue;
                }

                return Convert.ToUInt64(
                    value);
            }
        }

        throw new InvalidOperationException(
            "Не удалось определить объём оперативной памяти.");
    }

    private sealed record CpuData(
        string Name,
        int Cores,
        int LogicalProcessors);


    private static (int Width, int Height)
    ReadPrimaryDisplayResolution()
    {
        var mode =
            new DevMode
            {
                Size =
                    (ushort)Marshal.SizeOf<DevMode>()
            };

        var success =
            EnumDisplaySettings(
                null,
                EnumCurrentSettings,
                ref mode);

        if (!success)
        {
            throw new InvalidOperationException(
                "Не удалось определить физическое разрешение основного экрана.");
        }

        if (mode.PelsWidth == 0 ||
            mode.PelsHeight == 0)
        {
            throw new InvalidOperationException(
                "Windows вернула некорректное разрешение основного экрана.");
        }

        return (
            checked((int)mode.PelsWidth),
            checked((int)mode.PelsHeight));
    }

    private const int EnumCurrentSettings =
        -1;

    [DllImport(
        "user32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplaySettings(
        string? deviceName,
        int modeNumber,
        ref DevMode deviceMode);

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 32)]
        public string DeviceName;

        public ushort SpecVersion;
        public ushort DriverVersion;
        public ushort Size;
        public ushort DriverExtra;

        public uint Fields;

        public PointL Position;

        public uint DisplayOrientation;
        public uint DisplayFixedOutput;

        public short Color;
        public short Duplex;
        public short YResolution;
        public short TTOption;
        public short Collate;

        [MarshalAs(
            UnmanagedType.ByValTStr,
            SizeConst = 32)]
        public string FormName;

        public ushort LogPixels;

        public uint BitsPerPel;

        public uint PelsWidth;
        public uint PelsHeight;

        public uint DisplayFlags;
        public uint DisplayFrequency;

        public uint IcmMethod;
        public uint IcmIntent;
        public uint MediaType;
        public uint DitherType;

        public uint Reserved1;
        public uint Reserved2;

        public uint PanningWidth;
        public uint PanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointL
    {
        public int X;
        public int Y;
    }
}