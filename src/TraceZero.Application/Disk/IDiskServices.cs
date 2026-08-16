using TraceZero.Application.Scanning;
using TraceZero.Domain.Disk;

namespace TraceZero.Application.Disk;

/// <summary>Fournit l'état des lecteurs fixes (§20).</summary>
public interface IDriveQueryService
{
    IReadOnlyList<DriveInfoModel> GetFixedDrives();
}

/// <summary>Recherche les fichiers volumineux sous une racine, au-dessus d'un seuil (§20).</summary>
public interface ILargeFileScanner
{
    IAsyncEnumerable<LargeFileEntry> ScanAsync(
        string root,
        long minimumBytes,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken);
}

/// <summary>Envoie un fichier à la Corbeille (suppression réversible), jamais une suppression brute.</summary>
public interface IRecycleFileService
{
    /// <summary>Envoie le fichier à la Corbeille. Retourne vrai en cas de succès.</summary>
    bool SendToRecycleBin(string path);
}
