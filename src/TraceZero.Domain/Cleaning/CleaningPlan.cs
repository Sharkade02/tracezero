namespace TraceZero.Domain.Cleaning;

/// <summary>
/// Une action concrète du plan de nettoyage (§6). Dérivée d'un <see cref="ScanItem"/> sélectionné.
/// </summary>
public sealed record CleaningAction
{
    public required string ItemId { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Chemin cible (vide pour les actions non liées à un chemin, ex. Corbeille).</summary>
    public required string TargetPath { get; init; }

    public required FileActionKind Kind { get; init; }

    /// <summary>Racines autorisées, revalidées avant chaque suppression.</summary>
    public required IReadOnlyList<string> AllowedRoots { get; init; }

    /// <summary>Dossiers à balayer si l'action regroupe plusieurs cibles (vide = <see cref="TargetPath"/>).</summary>
    public IReadOnlyList<string> SweepRoots { get; init; } = [];

    public required RiskLevel Risk { get; init; }

    public long EstimatedBytes { get; init; }

    public Reversibility Reversibility { get; init; } = Reversibility.Irreversible;

    public bool NeedsElevation { get; init; }
}

/// <summary>
/// Plan de nettoyage complet : ce qui va se passer, avant toute suppression (§3.3, §6).
/// </summary>
public sealed record CleaningPlan
{
    public required IReadOnlyList<CleaningAction> Actions { get; init; }

    public long EstimatedBytes => Actions.Sum(a => a.EstimatedBytes);

    public int ActionCount => Actions.Count;

    public bool ContainsIrreversible => Actions.Any(a => a.Reversibility == Reversibility.Irreversible);

    public bool ContainsReview => Actions.Any(a => a.Risk == RiskLevel.Review);

    public long BytesFor(RiskLevel risk) => Actions.Where(a => a.Risk == risk).Sum(a => a.EstimatedBytes);

    public static CleaningPlan Empty { get; } = new() { Actions = [] };
}
