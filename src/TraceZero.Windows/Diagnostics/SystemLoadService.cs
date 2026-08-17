using System.Globalization;
using System.Management;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Windows.Diagnostics;

/// <summary>
/// Charge système en direct via WMI (read-only, sans admin). RAM depuis <c>Win32_OperatingSystem</c>
/// (FreePhysicalMemory / TotalVisibleMemorySize, en kilo-octets) ; CPU depuis
/// <c>Win32_PerfFormattedData_PerfOS_Processor</c> (instance <c>_Total</c>), valeur déjà formatée par
/// Windows (pas de calcul maison, pas de fenêtre d'échantillonnage à gérer). En cas d'échec WMI,
/// renvoie un instantané marqué indisponible plutôt qu'un zéro trompeur.
/// </summary>
public sealed class SystemLoadService : ISystemLoadService
{
    public SystemLoadSnapshot GetSnapshot()
    {
        var memory = ReadMemory(out var memoryOk);
        var cpu = ReadCpu(out var cpuOk);

        if (!memoryOk && !cpuOk)
        {
            return SystemLoadSnapshot.Unavailable;
        }

        return new SystemLoadSnapshot
        {
            Memory = memory,
            CpuPercent = cpu,
            Available = true,
        };
    }

    private static LiveMemoryUsage ReadMemory(out bool ok)
    {
        ok = false;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var os = (ManagementObject)raw;
                // Les deux valeurs sont en kilo-octets (doc WMI).
                var totalKb = AsLong(os["TotalVisibleMemorySize"]);
                var freeKb = AsLong(os["FreePhysicalMemory"]);
                if (totalKb <= 0)
                {
                    return LiveMemoryUsage.Empty;
                }

                ok = true;
                var totalBytes = totalKb * 1024;
                var usedBytes = Math.Max(0, (totalKb - freeKb) * 1024);
                return new LiveMemoryUsage { TotalBytes = totalBytes, UsedBytes = usedBytes };
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
        }

        return LiveMemoryUsage.Empty;
    }

    private static double ReadCpu(out bool ok)
    {
        ok = false;
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var perf = (ManagementObject)raw;
                ok = true;
                return Math.Clamp(AsLong(perf["PercentProcessorTime"]), 0, 100);
            }
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
        }

        return 0;
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
