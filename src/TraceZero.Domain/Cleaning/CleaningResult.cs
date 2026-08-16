namespace TraceZero.Domain.Cleaning;

/// <summary>Échec de nettoyage d'une action, expliqué (§41 : jamais un code d'erreur nu).</summary>
public sealed record CleaningFailure
{
    public required string ItemId { get; init; }

    public required string Path { get; init; }

    /// <summary>Message lisible pour l'utilisateur.</summary>
    public required string Message { get; init; }

    /// <summary>Détail technique (mode Expert) : HRESULT / Win32.</summary>
    public string? TechnicalDetail { get; init; }
}

/// <summary>Progression d'un nettoyage.</summary>
public sealed record CleaningProgress
{
    public required string CurrentItem { get; init; }

    public int Processed { get; init; }

    public int Total { get; init; }

    public long BytesFreed { get; init; }

    public double Fraction => Total == 0 ? 0 : Math.Clamp((double)Processed / Total, 0, 1);
}

/// <summary>
/// Résultat d'un nettoyage : ce qui a réellement été libéré, et les échecs (§13, §41).
/// </summary>
public sealed record CleaningResult
{
    public required long BytesFreed { get; init; }

    public required int ActionsSucceeded { get; init; }

    public IReadOnlyList<CleaningFailure> Failures { get; init; } = [];

    public required TimeSpan Elapsed { get; init; }

    public bool HasFailures => Failures.Count > 0;
}
