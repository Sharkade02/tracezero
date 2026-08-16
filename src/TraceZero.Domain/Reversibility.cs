namespace TraceZero.Domain;

/// <summary>
/// Réversibilité d'une action de nettoyage (§17). Doit être honnête : ne jamais présenter comme
/// réversible ce qui ne l'est pas.
/// </summary>
public enum Reversibility
{
    /// <summary>Peut être restauré (backup, Corbeille, point de restauration…).</summary>
    Reversible = 0,

    /// <summary>Partiellement restaurable.</summary>
    PartiallyReversible = 1,

    /// <summary>Irréversible (effacement sécurisé, suppression définitive).</summary>
    Irreversible = 2,
}
