using System.Security.Cryptography;
using TraceZero.Application.Duplicates;
using TraceZero.Application.Scanning;
using TraceZero.Domain.Duplicates;
using TraceZero.Engine.IO;

namespace TraceZero.Engine.Duplicates;

/// <summary>
/// Recherche de doublons en trois passes pour rester performante et fiable (§21) :
/// <list type="number">
///   <item>regroupement par taille (rejette immédiatement les tailles uniques) ;</item>
///   <item>hachage partiel des premiers Ko (écarte la majorité des faux candidats) ;</item>
///   <item>hachage complet SHA-256 (confirme l'identité réelle du contenu).</item>
/// </list>
/// Un doublon n'est jamais conclu sur le nom, la date ou la taille seuls.
/// </summary>
public sealed class DuplicateFinder : IDuplicateFinder
{
    private const int PartialHashBytes = 4096;
    private const int ReportEveryFiles = 256;

    public Task<IReadOnlyList<DuplicateGroup>> FindAsync(
        string root,
        long minimumBytes,
        IScanProgressReporter reporter,
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<DuplicateGroup>>(() => Find(root, minimumBytes, reporter, cancellationToken), cancellationToken);
    }

    private static List<DuplicateGroup> Find(string root, long minimumBytes, IScanProgressReporter reporter, CancellationToken cancellationToken)
    {
        // Passe 1 — regrouper par taille.
        var bySize = new Dictionary<long, List<FileEntry>>();
        var examined = 0;

        foreach (var entry in SafeFileEnumerator.EnumerateEntries(root, recursive: true, cancellationToken))
        {
            if (entry.Length < minimumBytes)
            {
                continue;
            }

            if (++examined >= ReportEveryFiles)
            {
                reporter.ReportFiles(examined, root);
                examined = 0;
            }

            if (!bySize.TryGetValue(entry.Length, out var list))
            {
                list = [];
                bySize[entry.Length] = list;
            }

            list.Add(entry);
        }

        if (examined > 0)
        {
            reporter.ReportFiles(examined, root);
        }

        var groups = new List<DuplicateGroup>();

        foreach (var (size, sameSize) in bySize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sameSize.Count < 2)
            {
                continue;
            }

            // Passe 2 — hachage partiel.
            foreach (var partialGroup in GroupBy(sameSize, f => ComputeHash(f.FullPath, PartialHashBytes)))
            {
                if (partialGroup.Count < 2)
                {
                    continue;
                }

                // Passe 3 — hachage complet.
                foreach (var fullGroup in GroupByKey(partialGroup, f => ComputeHash(f.FullPath, wholeFile: true)))
                {
                    if (fullGroup.Value.Count < 2)
                    {
                        continue;
                    }

                    groups.Add(new DuplicateGroup
                    {
                        Hash = fullGroup.Key,
                        SizeBytes = size,
                        Files = fullGroup.Value.Select(ToDuplicateFile).ToList(),
                    });
                }
            }
        }

        return groups.OrderByDescending(g => g.ReclaimableBytes).ToList();
    }

    private static Dictionary<string, List<FileEntry>>.ValueCollection GroupBy(List<FileEntry> files, Func<FileEntry, string?> keySelector) =>
        GroupByKey(files, keySelector).Values;

    private static Dictionary<string, List<FileEntry>> GroupByKey(List<FileEntry> files, Func<FileEntry, string?> keySelector)
    {
        var map = new Dictionary<string, List<FileEntry>>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var key = keySelector(file);
            if (key is null)
            {
                continue; // fichier illisible : écarté du rapprochement
            }

            if (!map.TryGetValue(key, out var list))
            {
                list = [];
                map[key] = list;
            }

            list.Add(file);
        }

        return map;
    }

    private static string? ComputeHash(string path, int maxBytes = 0, bool wholeFile = false)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.SequentialScan);

            if (wholeFile)
            {
                return Convert.ToHexString(SHA256.HashData(stream));
            }

            Span<byte> buffer = stackalloc byte[PartialHashBytes];
            var read = stream.Read(buffer[..Math.Min(maxBytes, PartialHashBytes)]);
            return Convert.ToHexString(SHA256.HashData(buffer[..read]));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static DuplicateFile ToDuplicateFile(FileEntry entry) => new()
    {
        Path = entry.FullPath,
        FileName = entry.FileName,
        SizeBytes = entry.Length,
        LastWriteUtc = entry.LastWriteUtc,
    };
}
