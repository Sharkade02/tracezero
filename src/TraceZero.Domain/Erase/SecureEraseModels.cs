namespace TraceZero.Domain.Erasure;

/// <summary>
/// Méthode d'effacement sécurisé d'un fichier (§19). On ne multiplie jamais artificiellement les passes
/// pour laisser croire à plus de sécurité. Sur SSD/NVMe, aucune de ces méthodes n'est présentée comme
/// garantie (wear leveling / TRIM).
/// </summary>
public enum SecureEraseMethod
{
    /// <summary>Un passage d'écrasement (données aléatoires) puis suppression.</summary>
    SingleOverwrite = 0,

    /// <summary>Mode renforcé (Expert) : trois passages (aléatoire, complément, aléatoire) puis suppression.</summary>
    ReinforcedOverwrite = 1,
}

/// <summary>Résultat d'un effacement sécurisé de fichier.</summary>
public sealed record SecureEraseResult
{
    public required bool Success { get; init; }

    public required string Path { get; init; }

    /// <summary>Nombre de passages d'écrasement réellement effectués.</summary>
    public int PassesApplied { get; init; }

    /// <summary>Message d'erreur honnête si l'opération a échoué (jamais silencieuse).</summary>
    public string? Error { get; init; }
}

/// <summary>Progression d'un effacement d'espace libre.</summary>
public sealed record FreeSpaceWipeProgress
{
    public required long BytesWritten { get; init; }

    public required long EstimatedTotalBytes { get; init; }

    public double Fraction => EstimatedTotalBytes <= 0
        ? 0
        : Math.Clamp((double)BytesWritten / EstimatedTotalBytes, 0, 1);
}

/// <summary>Résultat d'un effacement d'espace libre.</summary>
public sealed record FreeSpaceWipeResult
{
    public required bool Success { get; init; }

    public required long BytesWritten { get; init; }

    public bool Canceled { get; init; }

    public string? Error { get; init; }
}
