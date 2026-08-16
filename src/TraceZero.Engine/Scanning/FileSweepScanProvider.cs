using System.IO.Enumeration;
using System.Runtime.CompilerServices;
using TraceZero.Application.Rules;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Engine.IO;

namespace TraceZero.Engine.Scanning;

/// <summary>
/// Fournisseur de scan piloté par une <see cref="FileSweepRule"/>. Produit un <see cref="ScanItem"/>
/// agrégé par racine, avec des tailles et un nombre de fichiers réellement mesurés.
/// </summary>
public sealed class FileSweepScanProvider : IScanProvider
{
    private readonly FileSweepRule _rule;

    public FileSweepScanProvider(FileSweepRule rule) => _rule = rule ?? throw new ArgumentNullException(nameof(rule));

    public string Id => _rule.Id;

    public string DisplayName => _rule.DisplayName;

    public Category Category => _rule.Category;

    private const int ReportEveryFiles = 256;

    public async IAsyncEnumerable<ScanItem> ScanAsync(
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var root in _rule.Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = ScanRoot(root, reporter, cancellationToken);
            if (item is not null)
            {
                yield return item;
            }

            await Task.Yield();
        }
    }

    private ScanItem? ScanRoot(string root, IScanProgressReporter reporter, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var cutoffUtc = _rule.MinimumAge is { } age ? DateTime.UtcNow - age : (DateTime?)null;
        long totalBytes = 0;
        var fileCount = 0;
        var examinedSinceReport = 0;

        foreach (var file in SafeFileEnumerator.EnumerateEntries(root, _rule.Recursive, cancellationToken))
        {
            if (++examinedSinceReport >= ReportEveryFiles)
            {
                reporter.ReportFiles(examinedSinceReport, root);
                examinedSinceReport = 0;
            }

            if (!MatchesGlobs(file.FileName))
            {
                continue;
            }

            if (cutoffUtc is { } cutoff && file.LastWriteUtc > cutoff)
            {
                continue;
            }

            totalBytes += file.Length;
            fileCount++;
        }

        if (examinedSinceReport > 0)
        {
            reporter.ReportFiles(examinedSinceReport, root);
        }

        if (fileCount == 0)
        {
            return null;
        }

        // Aucun élément REVIEW n'est sélectionné par défaut (§3.2).
        var selected = _rule.SelectedByDefault && _rule.Risk != RiskLevel.Review;

        return new ScanItem
        {
            Id = $"{_rule.Id}::{root}",
            RuleId = _rule.Id,
            Category = _rule.Category,
            DisplayName = _rule.DisplayName,
            Description = _rule.Description,
            PathOrIdentifier = root,
            SizeBytes = totalBytes,
            ItemCount = fileCount,
            Risk = _rule.Risk,
            SelectedByDefault = selected,
            NeedsElevation = _rule.NeedsElevation,
            Reversibility = _rule.Reversibility,
            HelpKey = _rule.HelpKey,
            ActionKind = _rule.PreserveRoot ? FileActionKind.DeleteDirectoryContents : FileActionKind.DeleteDirectory,
            AllowedRoots = [root],
        };
    }

    private bool MatchesGlobs(string fileName)
    {
        if (_rule.IncludeGlobs.Count == 0)
        {
            return true;
        }

        foreach (var glob in _rule.IncludeGlobs)
        {
            if (FileSystemName.MatchesSimpleExpression(glob, fileName))
            {
                return true;
            }
        }

        return false;
    }
}
