using TraceZero.Domain.Diagnostics;

namespace TraceZero.Application.Diagnostics;

/// <summary>
/// Santé des disques physiques, en lecture seule (Phase 28). N'expose que ce que Windows rapporte
/// (WMI) : jamais de score inventé ni de « booster ».
/// </summary>
public interface IDiskHealthService
{
    IReadOnlyList<DiskHealth> GetDiskHealth();
}

/// <summary>
/// Inventaire des pilotes (Phase 14, étape A), en lecture seule. N'installe ni ne télécharge aucun
/// pilote — la mise à jour reste déléguée à Windows Update (§24).
/// </summary>
public interface IDriverHealthService
{
    IReadOnlyList<DriverInfo> GetDrivers();
}

/// <summary>
/// Impact des programmes au démarrage, mesuré par Windows (Phase 28). Lit le journal
/// « Diagnostics-Performance » (lecture seule) ; peut être vide si la donnée est indisponible
/// (droits insuffisants ou aucun démarrage récent mesuré).
/// </summary>
public interface IStartupImpactService
{
    /// <summary>Impacts agrégés par programme sur les derniers démarrages mesurés.</summary>
    StartupImpactReport GetRecentImpacts(int maxBoots = 10);
}

/// <summary>
/// Résultat de lecture des impacts au démarrage : les mesures et un indicateur honnête de disponibilité.
/// </summary>
public sealed record StartupImpactReport
{
    public required IReadOnlyList<StartupImpact> Impacts { get; init; }

    /// <summary>Faux si la donnée n'a pas pu être lue (ex. droits requis) — l'UI l'explique honnêtement.</summary>
    public required bool DataAvailable { get; init; }

    public static StartupImpactReport Unavailable { get; } =
        new() { Impacts = [], DataAvailable = false };
}
