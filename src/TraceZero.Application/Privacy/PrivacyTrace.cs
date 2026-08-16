using TraceZero.Domain;

namespace TraceZero.Application.Privacy;

public enum PrivacyTraceKind
{
    Registry = 0,
    File = 1,
}

/// <summary>
/// Description explicative d'une trace d'activité Windows (§15). Chaque trace doit être expliquée :
/// ce qu'elle révèle et pourquoi elle existe — jamais un chemin de registre nu.
/// </summary>
public sealed record PrivacyTraceDefinition
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Ce que Windows retient (langage humain).</summary>
    public required string Explanation { get; init; }

    /// <summary>Pourquoi cette trace existe.</summary>
    public required string Why { get; init; }

    public required PrivacyTraceKind Kind { get; init; }

    /// <summary>Sous-clé HKCU (traces registre).</summary>
    public string? RegistrySubKey { get; init; }

    /// <summary>Dossier racine (traces fichier).</summary>
    public string? FileRoot { get; init; }
}

/// <summary>Résultat d'inspection d'une trace : présence, nombre d'entrées et cible de nettoyage.</summary>
public sealed record PrivacyTraceResult
{
    public required PrivacyTraceDefinition Definition { get; init; }

    public required bool IsPresent { get; init; }

    public required int EntryCount { get; init; }

    /// <summary>Taille sur disque pour les traces fichier (0 pour le registre).</summary>
    public long SizeBytes { get; init; }

    /// <summary>Élément prêt à nettoyer (via <c>ICleaningEngine</c>).</summary>
    public required ScanItem CleanTarget { get; init; }
}

/// <summary>Inspecte les traces d'activité Windows connues (§15).</summary>
public interface IPrivacyInspector
{
    IReadOnlyList<PrivacyTraceResult> Inspect();
}
