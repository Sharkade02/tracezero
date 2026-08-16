using TraceZero.Domain;
using TraceZero.Domain.Cleaning;

namespace TraceZero.Application.Cleaning;

/// <summary>
/// Construit un plan de nettoyage à partir d'éléments sélectionnés puis l'exécute. Chaque suppression
/// est revalidée par <c>ISafePathValidator</c> (§6, §9).
/// </summary>
public interface ICleaningEngine
{
    /// <summary>Transforme les éléments sélectionnés en plan (§3.3 : « voici exactement ce qui va se passer »).</summary>
    CleaningPlan BuildPlan(IEnumerable<ScanItem> selectedItems);

    /// <summary>Exécute le plan. Les échecs sont collectés, ils n'interrompent pas le nettoyage.</summary>
    Task<CleaningResult> CleanAsync(
        CleaningPlan plan,
        IProgress<CleaningProgress>? progress,
        CancellationToken cancellationToken);
}
