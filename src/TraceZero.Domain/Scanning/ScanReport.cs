namespace TraceZero.Domain.Scanning;

/// <summary>Erreur isolée d'un fournisseur : un provider défaillant ne fait pas échouer le scan (§12).</summary>
public sealed record ProviderError(string ProviderId, string Message);

/// <summary>Agrégat par niveau de risque, pour l'affichage du Dashboard (§3.2).</summary>
public sealed record RiskSummary(RiskLevel Risk, int ItemCount, long Bytes);

/// <summary>
/// Résultat complet d'un scan (§12). Les tailles sont réelles, jamais inventées.
/// </summary>
public sealed record ScanReport
{
    public required IReadOnlyList<ScanItem> Items { get; init; }

    public required TimeSpan Elapsed { get; init; }

    public IReadOnlyList<ProviderError> Errors { get; init; } = [];

    public int TotalItems => Items.Count;

    public long TotalBytes => Items.Sum(i => i.SizeBytes);

    public IReadOnlyList<RiskSummary> ByRisk =>
        Items.GroupBy(i => i.Risk)
             .Select(g => new RiskSummary(g.Key, g.Count(), g.Sum(i => i.SizeBytes)))
             .OrderBy(s => s.Risk)
             .ToList();

    public long BytesFor(RiskLevel risk) => Items.Where(i => i.Risk == risk).Sum(i => i.SizeBytes);

    public static ScanReport Empty { get; } = new()
    {
        Items = [],
        Elapsed = TimeSpan.Zero,
    };
}
