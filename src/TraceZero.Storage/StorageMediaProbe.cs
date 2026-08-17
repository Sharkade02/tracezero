using System.Globalization;
using System.Management;
using TraceZero.Application.Erasure;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Storage;

/// <summary>
/// Détecte le type de média (HDD/SSD) du lecteur contenant un chemin (§19), via WMI :
/// lettre de lecteur → <c>MSFT_Partition.DiskNumber</c> → <c>MSFT_PhysicalDisk.MediaType</c>.
/// En cas d'incertitude, retourne <see cref="DiskMediaKind.Unknown"/> — l'UI présente alors un
/// avertissement couvrant les deux cas plutôt qu'un type erroné.
/// </summary>
public sealed class StorageMediaProbe : IStorageMediaProbe
{
    public DiskMediaKind GetMediaForPath(string path)
    {
        var letter = GetDriveLetter(path);
        if (letter is null)
        {
            return DiskMediaKind.Unknown;
        }

        try
        {
            var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
            scope.Connect();

            var diskNumber = QueryDiskNumber(scope, letter.Value);
            if (diskNumber is null)
            {
                return DiskMediaKind.Unknown;
            }

            return QueryMediaType(scope, diskNumber.Value);
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            return DiskMediaKind.Unknown;
        }
    }

    private static int? QueryDiskNumber(ManagementScope scope, char letter)
    {
        var query = new ObjectQuery(
            $"SELECT DiskNumber FROM MSFT_Partition WHERE DriveLetter='{letter}'");
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        foreach (var raw in results)
        {
            using var partition = (ManagementObject)raw;
            if (partition["DiskNumber"] is { } value)
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    private static DiskMediaKind QueryMediaType(ManagementScope scope, int diskNumber)
    {
        var query = new ObjectQuery(
            $"SELECT MediaType FROM MSFT_PhysicalDisk WHERE DeviceId='{diskNumber}'");
        using var searcher = new ManagementObjectSearcher(scope, query);
        using var results = searcher.Get();

        foreach (var raw in results)
        {
            using var disk = (ManagementObject)raw;
            // MediaType : 3=HDD, 4=SSD, 5=SCM.
            var mediaType = disk["MediaType"] is { } value
                ? Convert.ToInt32(value, CultureInfo.InvariantCulture)
                : 0;

            return mediaType switch
            {
                3 => DiskMediaKind.Hdd,
                4 => DiskMediaKind.Ssd,
                _ => DiskMediaKind.Unknown,
            };
        }

        return DiskMediaKind.Unknown;
    }

    private static char? GetDriveLetter(string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (!string.IsNullOrEmpty(root) && char.IsLetter(root[0]))
            {
                return char.ToUpperInvariant(root[0]);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
        }

        return null;
    }
}
