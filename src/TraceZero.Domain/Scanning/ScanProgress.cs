namespace TraceZero.Domain.Scanning;

/// <summary>
/// Point de progression émis pendant un scan. Immuable, sûr à transmettre entre threads.
/// </summary>
public sealed record ScanProgress
{
    /// <summary>Nom lisible du fournisseur en cours (ou dernier terminé).</summary>
    public required string CurrentProvider { get; init; }

    /// <summary>Nombre de fournisseurs terminés.</summary>
    public int CompletedProviders { get; init; }

    /// <summary>Nombre total de fournisseurs.</summary>
    public int TotalProviders { get; init; }

    /// <summary>Éléments trouvés cumulés.</summary>
    public int ItemsFound { get; init; }

    /// <summary>Octets récupérables cumulés.</summary>
    public long BytesFound { get; init; }

    /// <summary>Nombre de fichiers examinés cumulés (progression fine pendant le balayage).</summary>
    public long FilesScanned { get; init; }

    public double Fraction => TotalProviders == 0
        ? 0
        : Math.Clamp((double)CompletedProviders / TotalProviders, 0, 1);
}
