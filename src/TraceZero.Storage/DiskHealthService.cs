using System.Management;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Storage;

/// <summary>
/// Santé des disques physiques via WMI (Phase 28), en lecture seule et sans élévation. Interroge
/// <c>MSFT_PhysicalDisk</c> (espace <c>root\Microsoft\Windows\Storage</c>) pour l'état de santé, le type
/// de média et la taille — tels que rapportés par Windows. Aucune valeur inventée ; en cas d'échec WMI,
/// la liste est simplement vide.
/// </summary>
public sealed class DiskHealthService : IDiskHealthService
{
    public IReadOnlyList<DiskHealth> GetDiskHealth()
    {
        var disks = new List<DiskHealth>();

        try
        {
            var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
            scope.Connect();

            var query = new ObjectQuery(
                "SELECT FriendlyName, HealthStatus, MediaType, Size FROM MSFT_PhysicalDisk");
            using var searcher = new ManagementObjectSearcher(scope, query);
            using var results = searcher.Get();

            foreach (var raw in results)
            {
                using var disk = (ManagementObject)raw;
                disks.Add(new DiskHealth
                {
                    Model = AsString(disk["FriendlyName"]) ?? "Disque",
                    Status = MapHealth(disk["HealthStatus"]),
                    Media = MapMedia(disk["MediaType"]),
                    SizeBytes = AsLong(disk["Size"]),
                    StatusDetail = HealthDetail(disk["HealthStatus"]),
                });
            }
        }
        catch (ManagementException)
        {
            // Espace/classe WMI indisponible : on ne rapporte rien plutôt que d'inventer.
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (System.Runtime.InteropServices.COMException)
        {
        }

        return disks;
    }

    // MSFT_PhysicalDisk.HealthStatus : 0=Healthy, 1=Warning, 2=Unhealthy.
    private static DiskHealthStatus MapHealth(object? value) => AsInt(value) switch
    {
        0 => DiskHealthStatus.Healthy,
        1 => DiskHealthStatus.Warning,
        2 => DiskHealthStatus.Unhealthy,
        _ => DiskHealthStatus.Unknown,
    };

    private static string? HealthDetail(object? value) => AsInt(value) switch
    {
        0 => "Sain",
        1 => "Avertissement",
        2 => "Défaillant",
        _ => null,
    };

    // MSFT_PhysicalDisk.MediaType : 3=HDD, 4=SSD, 5=SCM ; 0/autre = inconnu.
    private static DiskMediaKind MapMedia(object? value) => AsInt(value) switch
    {
        3 => DiskMediaKind.Hdd,
        4 => DiskMediaKind.Ssd,
        _ => DiskMediaKind.Unknown,
    };

    private static string? AsString(object? value) =>
        value is null ? null : value.ToString();

    private static int AsInt(object? value)
    {
        try
        {
            return value is null ? -1 : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return -1;
        }
    }

    private static long AsLong(object? value)
    {
        try
        {
            return value is null ? 0 : Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return 0;
        }
    }
}
