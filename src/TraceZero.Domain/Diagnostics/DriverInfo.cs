namespace TraceZero.Domain.Diagnostics;

/// <summary>
/// Inventaire d'un pilote de périphérique (Phase 14, étape A — Driver Health, lecture seule). Toutes les
/// valeurs proviennent de Windows (WMI). TraceZero n'installe ni ne télécharge aucun pilote : la mise à
/// jour est déléguée à Windows Update (§24).
/// </summary>
public sealed record DriverInfo
{
    public required string DeviceName { get; init; }

    public string? DeviceClass { get; init; }

    public string? Version { get; init; }

    public string? Provider { get; init; }

    public string? Manufacturer { get; init; }

    public DateOnly? Date { get; init; }

    public bool IsSigned { get; init; }

    /// <summary>Le périphérique a un problème signalé par le Gestionnaire de périphériques.</summary>
    public bool HasProblem { get; init; }

    /// <summary>Code d'erreur ConfigManager (0 = aucun), tel que rapporté par Windows.</summary>
    public int ProblemCode { get; init; }
}
