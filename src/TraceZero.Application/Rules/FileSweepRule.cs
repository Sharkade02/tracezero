using TraceZero.Domain;

namespace TraceZero.Application.Rules;

/// <summary>
/// Description déclarative d'un balayage de fichiers (§8, forme légère du futur moteur de règles).
/// Validable, testable, versionnable. Les chemins sont des racines absolues déjà résolues.
/// </summary>
public sealed record FileSweepRule
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    /// <summary>Clé de ressource pour le nom localisé (§31), repli sur <see cref="DisplayName"/>.</summary>
    public string? NameKey { get; init; }

    public string? Description { get; init; }

    /// <summary>Clé de ressource pour la description localisée (§31), repli sur <see cref="Description"/>.</summary>
    public string? DescriptionKey { get; init; }

    public required Category Category { get; init; }

    public required RiskLevel Risk { get; init; }

    /// <summary>Racines absolues à balayer (déjà résolues depuis les variables d'environnement).</summary>
    public required IReadOnlyList<string> Roots { get; init; }

    /// <summary>Balayage récursif des sous-dossiers (sans jamais suivre un point d'analyse).</summary>
    public bool Recursive { get; init; } = true;

    /// <summary>Motifs de noms de fichiers (glob simple * ?). Vide = tous les fichiers.</summary>
    public IReadOnlyList<string> IncludeGlobs { get; init; } = [];

    /// <summary>N'inclure que les fichiers modifiés il y a au moins cette durée.</summary>
    public TimeSpan? MinimumAge { get; init; }

    /// <summary>Conserver le dossier racine (supprimer seulement son contenu).</summary>
    public bool PreserveRoot { get; init; } = true;

    /// <summary>Sélectionné par défaut. Ignoré si <see cref="Risk"/> vaut Review (§3.2).</summary>
    public bool SelectedByDefault { get; init; }

    public Reversibility Reversibility { get; init; } = Reversibility.Irreversible;

    public bool NeedsElevation { get; init; }

    public string? HelpKey { get; init; }
}
