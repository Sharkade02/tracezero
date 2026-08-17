using System.Globalization;
using System.Management;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Storage;

/// <summary>
/// Informations mémoire via WMI (read-only, sans admin). Interroge <c>Win32_PhysicalMemory</c> (modules)
/// et <c>Win32_PhysicalMemoryArray</c> (slots / capacité max). Aucune valeur inventée ; en cas d'échec
/// WMI, renvoie un rapport vide. Les timings (CL, tRCD…) ne sont pas accessibles ici (SPD/SMBus).
/// </summary>
public sealed class MemoryInfoService : IMemoryInfoService
{
    public MemoryReport GetMemory()
    {
        var modules = new List<MemoryModule>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceLocator, Manufacturer, PartNumber, Capacity, Speed, ConfiguredClockSpeed, ConfiguredVoltage, SMBIOSMemoryType FROM Win32_PhysicalMemory");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var mem = (ManagementObject)raw;
                modules.Add(new MemoryModule
                {
                    Slot = AsString(mem["DeviceLocator"]) ?? "?",
                    Manufacturer = Clean(AsString(mem["Manufacturer"])),
                    PartNumber = Clean(AsString(mem["PartNumber"])),
                    CapacityBytes = AsLong(mem["Capacity"]),
                    RatedSpeedMhz = AsInt(mem["Speed"]),
                    ConfiguredSpeedMhz = AsInt(mem["ConfiguredClockSpeed"]),
                    VoltageMillivolts = AsInt(mem["ConfiguredVoltage"]),
                    Type = MapType(AsInt(mem["SMBIOSMemoryType"])),
                });
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            return MemoryReport.Empty;
        }

        var (slots, maxBytes) = ReadArray();

        return new MemoryReport
        {
            Modules = modules,
            TotalSlots = slots > 0 ? slots : modules.Count,
            MaxCapacityBytes = maxBytes,
        };
    }

    private static (int Slots, long MaxBytes) ReadArray()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT MemoryDevices, MaxCapacityEx, MaxCapacity FROM Win32_PhysicalMemoryArray");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var array = (ManagementObject)raw;
                var slots = AsInt(array["MemoryDevices"]);
                // MaxCapacityEx (récent) et MaxCapacity (déprécié) sont tous deux en kilo-octets (doc WMI).
                var maxKb = AsLong(array["MaxCapacityEx"]);
                if (maxKb <= 0)
                {
                    maxKb = AsLong(array["MaxCapacity"]);
                }
                return (slots, maxKb * 1024);
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
        }

        return (0, 0);
    }

    // SMBIOSMemoryType : 24=DDR3, 26=DDR4, 34=DDR5.
    private static MemoryType MapType(int smbios) => smbios switch
    {
        24 => MemoryType.Ddr3,
        26 => MemoryType.Ddr4,
        34 => MemoryType.Ddr5,
        _ => MemoryType.Unknown,
    };

    private static string? AsString(object? value) => value?.ToString();

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static int AsInt(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return 0;
        }
    }

    private static long AsLong(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return 0;
        }
    }
}
