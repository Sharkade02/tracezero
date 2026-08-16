namespace TraceZero.Domain.Exclusions;

public enum ExclusionKind
{
    /// <summary>Exclut tout élément situé sous un dossier.</summary>
    Folder = 0,

    /// <summary>Exclut toute une catégorie d'éléments.</summary>
    Category = 1,
}

/// <summary>
/// Une règle d'exclusion : ce que TraceZero ne doit jamais proposer de nettoyer (§16).
/// </summary>
public sealed record ExclusionRule
{
    public required Guid Id { get; init; }

    public required ExclusionKind Kind { get; init; }

    /// <summary>Chemin de dossier, ou nom de catégorie (selon <see cref="Kind"/>).</summary>
    public required string Value { get; init; }

    public required string DisplayName { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }
}
