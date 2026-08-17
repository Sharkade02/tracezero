namespace TraceZero.Domain.Protection;

/// <summary>Nature d'un élément restaurable conservé dans le coffre de protection (§17).</summary>
public enum RestoreItemKind
{
    /// <summary>Sauvegarde d'une clé de registre HKCU (trace de confidentialité).</summary>
    RegistryBackup = 0,
}

/// <summary>
/// Élément restaurable persisté par la couche de protection (§17). Enregistre ce qui a été sauvegardé
/// avant un nettoyage réversible, et permet de le restaurer plus tard. Les données restent locales
/// (jamais transmises) — c'est le principe même d'une sauvegarde.
/// </summary>
public sealed record RestoreRecord
{
    public long Id { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Libellé humain de l'élément (ex. « Documents récents »).</summary>
    public required string Description { get; init; }

    /// <summary>Origine du nettoyage (ex. « Confidentialité »).</summary>
    public required string Source { get; init; }

    public required RestoreItemKind Kind { get; init; }

    /// <summary>Réversibilité honnête de l'opération (§17).</summary>
    public required Reversibility Reversibility { get; init; }

    /// <summary>Cible de restauration (sous-clé HKCU pour <see cref="RestoreItemKind.RegistryBackup"/>).</summary>
    public required string Target { get; init; }

    /// <summary>Charge utile sérialisée de la sauvegarde (instantané de registre en JSON).</summary>
    public required string Payload { get; init; }

    /// <summary>Vrai si l'élément a déjà été restauré.</summary>
    public bool IsRestored { get; init; }
}
