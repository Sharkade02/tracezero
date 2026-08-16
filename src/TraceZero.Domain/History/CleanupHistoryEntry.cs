namespace TraceZero.Domain.History;

/// <summary>
/// Entrée d'historique d'un nettoyage (§16). Ne contient jamais de contenu de fichier ni de chemin
/// personnel : uniquement un résumé (privacy by design, §39).
/// </summary>
public sealed record CleanupHistoryEntry
{
    public long Id { get; init; }

    public required DateTimeOffset TimestampUtc { get; init; }

    /// <summary>Version de TraceZero au moment du nettoyage.</summary>
    public required string AppVersion { get; init; }

    /// <summary>Origine du nettoyage (ex. « Nettoyage », « Confidentialité »).</summary>
    public required string Source { get; init; }

    public long FreedBytes { get; init; }

    public int ItemsCleaned { get; init; }

    public int Failures { get; init; }

    public long DurationMs { get; init; }
}

/// <summary>Statistiques agrégées de l'historique local.</summary>
public sealed record CleanupHistoryStats(long TotalFreedBytes, int CleanupCount, DateTimeOffset? LastCleanupUtc)
{
    public static CleanupHistoryStats Empty { get; } = new(0, 0, null);
}
