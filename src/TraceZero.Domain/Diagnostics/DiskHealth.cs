namespace TraceZero.Domain.Diagnostics;

/// <summary>État de santé d'un disque, tel que rapporté par Windows (Phase 28). Jamais un score inventé.</summary>
public enum DiskHealthStatus
{
    /// <summary>Windows ne rapporte pas d'état fiable.</summary>
    Unknown = 0,

    /// <summary>Sain.</summary>
    Healthy = 1,

    /// <summary>Avertissement : Windows signale un risque.</summary>
    Warning = 2,

    /// <summary>Défaillant.</summary>
    Unhealthy = 3,
}

/// <summary>Type de média d'un disque physique (Phase 28), réutilisable par l'effacement sécurisé (Phase 9).</summary>
public enum DiskMediaKind
{
    Unknown = 0,
    Hdd = 1,
    Ssd = 2,
}

/// <summary>
/// Santé d'un disque physique (Phase 28). Toutes les valeurs proviennent de Windows (WMI) ; aucune n'est
/// inventée. Un état <see cref="DiskHealthStatus.Warning"/> est présenté factuellement, sans alarmisme.
/// </summary>
public sealed record DiskHealth
{
    public required string Model { get; init; }

    public required DiskHealthStatus Status { get; init; }

    public required DiskMediaKind Media { get; init; }

    public long SizeBytes { get; init; }

    /// <summary>Détail brut rapporté par Windows (ex. « OK », « Pred Fail »), quand disponible.</summary>
    public string? StatusDetail { get; init; }
}
