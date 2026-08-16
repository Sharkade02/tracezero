using TraceZero.Domain.Elevation;

namespace TraceZero.Application.Elevation;

/// <summary>
/// Façade côté UI pour déclencher une opération nécessitant l'élévation (Phase 20, §30).
///
/// L'application ne démarre jamais élevée : cette façade lance à la demande le helper
/// <c>TraceZero.Elevated.exe</c> (déclenchant l'invite UAC), lui transmet une commande structurée,
/// attend son résultat, puis le helper s'arrête. L'UI ne réalise elle-même aucune opération élevée.
/// </summary>
public interface IElevatedOperationService
{
    /// <summary>Le processus courant dispose-t-il déjà des droits administrateur ?</summary>
    bool IsCurrentProcessElevated { get; }

    /// <summary>
    /// Exécute <paramref name="request"/> via le helper élevé. Retourne un résultat structuré ;
    /// un refus d'élévation (UAC annulé) ou une erreur d'invocation est renvoyé comme échec explicite,
    /// jamais une exception non gérée.
    /// </summary>
    Task<ElevatedResult> RunAsync(ElevatedRequest request, CancellationToken cancellationToken = default);
}
