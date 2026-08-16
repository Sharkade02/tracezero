namespace TraceZero.Domain;

/// <summary>
/// Classification de risque d'un élément scanné. Exposée par le moteur, jamais calculée dans l'UI (§3.1).
/// </summary>
public enum RiskLevel
{
    /// <summary>Suppression sans impact fonctionnel normalement attendu (caches, fichiers temporaires…).</summary>
    Safe = 0,

    /// <summary>Suppression généralement sûre mais efface une trace ou un historique (RecentDocs, historiques…).</summary>
    Privacy = 1,

    /// <summary>Peut supprimer une information souhaitée par l'utilisateur (Corbeille, cookies, sessions…).</summary>
    Review = 2,
}
