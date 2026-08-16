using System.Runtime.CompilerServices;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Scanning;
using TraceZero.Domain;

namespace TraceZero.Engine.Scanning;

/// <summary>
/// Fournisseur pour la Corbeille. Classée REVIEW (§3.1) : jamais sélectionnée par défaut, car elle
/// peut contenir des fichiers que l'utilisateur souhaite récupérer.
/// </summary>
public sealed class RecycleBinScanProvider : IScanProvider
{
    private readonly IRecycleBinService _recycleBin;

    public RecycleBinScanProvider(IRecycleBinService recycleBin) =>
        _recycleBin = recycleBin ?? throw new ArgumentNullException(nameof(recycleBin));

    public string Id => "windows.recycle-bin";

    public string DisplayName => "Corbeille";

    public Category Category => Category.RecycleBin;

    public async IAsyncEnumerable<ScanItem> ScanAsync(
        IScanProgressReporter reporter,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var bytes = _recycleBin.GetUsedBytes();
        var count = _recycleBin.GetItemCount();

        if (bytes <= 0 && count <= 0)
        {
            yield break;
        }

        yield return new ScanItem
        {
            Id = "windows.recycle-bin",
            RuleId = "windows.recycle-bin",
            Category = Category.RecycleBin,
            DisplayName = "Corbeille",
            Description = "Éléments présents dans la Corbeille. À vérifier : vider la Corbeille est irréversible.",
            PathOrIdentifier = "shell:RecycleBinFolder",
            SizeBytes = bytes,
            ItemCount = (int)Math.Min(count, int.MaxValue),
            Risk = RiskLevel.Review,
            SelectedByDefault = false,
            Reversibility = Reversibility.Irreversible,
            ActionKind = FileActionKind.EmptyRecycleBin,
            AllowedRoots = [],
        };
    }
}
