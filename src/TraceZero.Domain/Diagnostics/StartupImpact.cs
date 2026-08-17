namespace TraceZero.Domain.Diagnostics;

/// <summary>
/// Impact d'un programme au démarrage, mesuré par Windows (Phase 28). Provient du journal
/// « Diagnostics-Performance » ; ce n'est pas une estimation inventée. En l'absence de données récentes,
/// aucun impact n'est affiché plutôt qu'une valeur factice.
/// </summary>
public sealed record StartupImpact
{
    /// <summary>Nom du programme tel que rapporté par l'événement Windows.</summary>
    public required string Name { get; init; }

    /// <summary>Pénalité moyenne au démarrage (millisecondes), sur les démarrages mesurés.</summary>
    public double AverageMs { get; init; }

    /// <summary>Nombre de démarrages ayant fourni une mesure.</summary>
    public int SampleCount { get; init; }
}
