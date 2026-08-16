using System.Runtime.CompilerServices;
using TraceZero.Application.Disk;
using TraceZero.Application.Scanning;
using TraceZero.Domain.Disk;
using TraceZero.Engine.IO;

namespace TraceZero.Engine.Disk;

/// <summary>
/// Recherche les fichiers volumineux sous une racine (§20). Réutilise l'énumérateur sûr : ne suit
/// jamais un point d'analyse, ignore l'inaccessible, lit la taille sans syscall supplémentaire.
/// </summary>
public sealed class LargeFileScanner : ILargeFileScanner
{
    private const int ReportEveryFiles = 512;

    public async IAsyncEnumerable<LargeFileEntry> ScanAsync(
        string root,
        long minimumBytes,
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        var examined = 0;
        foreach (var entry in SafeFileEnumerator.EnumerateEntries(root, recursive: true, cancellationToken))
        {
            if (++examined >= ReportEveryFiles)
            {
                reporter.ReportFiles(examined, root);
                examined = 0;
            }

            if (entry.Length >= minimumBytes)
            {
                yield return new LargeFileEntry
                {
                    Path = entry.FullPath,
                    FileName = entry.FileName,
                    SizeBytes = entry.Length,
                    LastWriteUtc = entry.LastWriteUtc,
                };
            }
        }

        if (examined > 0)
        {
            reporter.ReportFiles(examined, root);
        }
    }
}
