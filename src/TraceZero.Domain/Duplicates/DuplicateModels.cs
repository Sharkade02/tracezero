namespace TraceZero.Domain.Duplicates;

/// <summary>Un fichier membre d'un groupe de doublons.</summary>
public sealed record DuplicateFile
{
    public required string Path { get; init; }

    public required string FileName { get; init; }

    public long SizeBytes { get; init; }

    public DateTime LastWriteUtc { get; init; }
}

/// <summary>
/// Un groupe de fichiers au contenu identique, confirmé par hachage cryptographique complet (§21).
/// Jamais conclu sur le nom, la date ou la taille seuls.
/// </summary>
public sealed record DuplicateGroup
{
    /// <summary>Hachage complet (SHA-256) partagé par tous les fichiers du groupe.</summary>
    public required string Hash { get; init; }

    public long SizeBytes { get; init; }

    public required IReadOnlyList<DuplicateFile> Files { get; init; }

    /// <summary>Espace récupérable = taille × (nombre de copies − 1).</summary>
    public long ReclaimableBytes => SizeBytes * Math.Max(0, Files.Count - 1);
}
