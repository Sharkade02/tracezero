namespace TraceZero.Domain.Ntfs;

/// <summary>
/// Statut honnête d'un artefact NTFS (Phase 8, §18). On n'affiche jamais « Nettoyable » ni ne simule de
/// suppression pour ce qui ne peut être retiré de façon fiable.
/// </summary>
public enum NtfsArtifactStatus
{
    /// <summary>Visible/expliqué mais non supprimable de façon fiable sans risque — détecté uniquement.</summary>
    DetectedOnly = 0,

    /// <summary>Géré par Windows ; le retirer manuellement corromprait le système de fichiers — détecté uniquement.</summary>
    ManagedByWindows = 1,

    /// <summary>Atténuable en toute sécurité par l'effacement de l'espace libre (Phase 9).</summary>
    MitigableByFreeSpaceWipe = 2,
}

/// <summary>
/// Un artefact de confidentialité NTFS expliqué (Phase 8, §18). Analyse en lecture seule : jamais de
/// modification de structures NTFS brutes, jamais d'écriture MFT, jamais de contournement du FS.
/// </summary>
public sealed record NtfsArtifact
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// <summary>Ce que c'est / ce qu'il révèle, en langage humain.</summary>
    public required string Explanation { get; init; }

    /// <summary>Pourquoi il existe.</summary>
    public required string Why { get; init; }

    public required NtfsArtifactStatus Status { get; init; }

    /// <summary>Détail factuel additionnel (ex. espace libre mesuré), quand disponible.</summary>
    public string? Detail { get; init; }
}
