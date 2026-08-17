using TraceZero.Domain.Protection;

namespace TraceZero.Application.Protection;

/// <summary>
/// Coffre local des éléments restaurables (§17). Persiste les sauvegardes créées avant les nettoyages
/// réversibles et permet de les lister/restaurer. Local uniquement, aucune télémétrie (§39).
/// </summary>
public interface IProtectionVault
{
    /// <summary>Enregistre un élément restaurable. Retourne son identifiant.</summary>
    Task<long> AddAsync(RestoreRecord record, CancellationToken cancellationToken = default);

    /// <summary>Éléments encore restaurables (non déjà restaurés), du plus récent au plus ancien.</summary>
    Task<IReadOnlyList<RestoreRecord>> GetRestorableAsync(int max, CancellationToken cancellationToken = default);

    /// <summary>Charge un élément par identifiant, ou <c>null</c> s'il n'existe pas.</summary>
    Task<RestoreRecord?> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Marque un élément comme restauré (il n'apparaît plus dans la liste restaurable).</summary>
    Task MarkRestoredAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Vide le coffre (supprime toutes les sauvegardes).</summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
