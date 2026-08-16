using TraceZero.Application.Disk;
using TraceZero.Domain.Disk;

namespace TraceZero.Storage;

/// <summary>Liste les lecteurs fixes prêts et leur occupation (§20).</summary>
public sealed class DriveQueryService : IDriveQueryService
{
    public IReadOnlyList<DriveInfoModel> GetFixedDrives()
    {
        var drives = new List<DriveInfoModel>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
                {
                    continue;
                }

                drives.Add(new DriveInfoModel
                {
                    Name = drive.Name,
                    Label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? null : drive.VolumeLabel,
                    Format = drive.DriveFormat,
                    TotalBytes = drive.TotalSize,
                    FreeBytes = drive.AvailableFreeSpace,
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Lecteur non prêt / inaccessible : ignoré.
            }
        }

        return drives;
    }
}
