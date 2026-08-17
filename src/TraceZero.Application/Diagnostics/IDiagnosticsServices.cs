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
/// Informations sur la mémoire installée (RAM), en lecture seule via WMI. N'expose que ce que Windows
/// rapporte ; les timings/latences ne sont pas disponibles sans accès matériel bas niveau.
/// </summary>
public interface IMemoryInfoService
{
    MemoryReport GetMemory();
}

/// <summary>
/// Charge système en direct (RAM utilisée + CPU), mesurée par Windows via WMI (read-only, sans admin).
/// Renvoie un instantané ; l'UI le rafraîchit périodiquement. Jamais de valeur inventée.
/// </summary>
public interface ISystemLoadService
{
    SystemLoadSnapshot GetSnapshot();
}

/// <summary>
/// Programmes qui consomment le plus de mémoire (« ce qui peut ralentir le PC »), en lecture seule.
/// Agrège par nom de programme et trie par working set décroissant. Les process inaccessibles sont
/// ignorés honnêtement (pas d'élévation).
/// </summary>
public interface IProcessUsageService
{
    IReadOnlyList<ProcessUsage> GetTopByMemory(int count = 8);
}

/// <summary>
/// Indice de performance Windows (WinSAT), en lecture seule via WMI. Expose les scores calculés par
/// Windows ; ne recalcule ni n'invente rien. Indisponible si aucune évaluation n'est en cache.
/// </summary>
public interface IPerformanceIndexService
{
    PerformanceIndex GetIndex();
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
