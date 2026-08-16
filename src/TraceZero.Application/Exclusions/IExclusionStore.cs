using TraceZero.Domain;
using TraceZero.Domain.Exclusions;

namespace TraceZero.Application.Exclusions;

/// <summary>
/// Gère les règles d'exclusion et détermine si un élément scanné doit être écarté (§16).
/// </summary>
public interface IExclusionStore
{
    IReadOnlyList<ExclusionRule> GetAll();

    void Add(ExclusionRule rule);

    void Remove(Guid id);

    /// <summary>Vrai si l'élément est couvert par au moins une règle d'exclusion.</summary>
    bool IsExcluded(ScanItem item);
}
