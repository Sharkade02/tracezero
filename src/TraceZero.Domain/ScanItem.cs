namespace TraceZero.Domain;

/// <summary>
/// Un élément trouvé par un scan (§7). Porte tout ce qui est nécessaire pour l'affichage,
/// la classification du risque, la prévisualisation et la suppression sécurisée.
/// </summary>
public sealed record ScanItem
{
    /// <summary>Identifiant stable de l'élément (dérivé de la règle + chemin/identifiant système).</summary>
    public required string Id { get; init; }

    /// <summary>Identifiant de la règle qui a produit cet élément.</summary>
    public required string RuleId { get; init; }

    public required Category Category { get; init; }

    public string? SubCategory { get; init; }

    /// <summary>Nom lisible affiché à l'utilisateur (repli si aucune clé localisée).</summary>
    public required string DisplayName { get; init; }

    /// <summary>Clé de ressource pour le nom localisé (§31). Si présente, l'UI l'utilise avant <see cref="DisplayName"/>.</summary>
    public string? NameKey { get; init; }

    /// <summary>Arguments de format pour <see cref="NameKey"/> (ex. nom du navigateur, profil). Vide = clé simple.</summary>
    public IReadOnlyList<string> NameArgs { get; init; } = [];

    /// <summary>Explication en langage humain de ce que représente l'élément (repli).</summary>
    public string? Description { get; init; }

    /// <summary>Clé de ressource pour la description localisée (§31).</summary>
    public string? DescriptionKey { get; init; }

    /// <summary>Arguments de format pour <see cref="DescriptionKey"/>. Vide = clé simple.</summary>
    public IReadOnlyList<string> DescriptionArgs { get; init; } = [];

    /// <summary>Chemin de fichier ou identifiant système (clé de registre, etc.).</summary>
    public required string PathOrIdentifier { get; init; }

    /// <summary>Taille en octets réellement mesurée. Jamais inventée.</summary>
    public long SizeBytes { get; init; }

    /// <summary>Nombre d'éléments regroupés (fichiers dans un dossier, entrées, etc.).</summary>
    public int ItemCount { get; init; } = 1;

    public required RiskLevel Risk { get; init; }

    /// <summary>Sélectionné par défaut dans le nettoyage recommandé. Aucun REVIEW ne l'est (§3.2).</summary>
    public bool SelectedByDefault { get; init; }

    /// <summary>Nécessite une élévation de privilèges pour être supprimé.</summary>
    public bool NeedsElevation { get; init; }

    /// <summary>Navigateur ou application concerné, le cas échéant.</summary>
    public string? AssociatedApp { get; init; }

    /// <summary>L'élément est verrouillé (fichier en cours d'utilisation).</summary>
    public bool IsLocked { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public Reversibility Reversibility { get; init; } = Reversibility.Irreversible;

    /// <summary>Raison / justification de la présence de l'élément dans les résultats.</summary>
    public string? Reason { get; init; }

    /// <summary>Clé de ressource d'aide/explication (localisée dans l'UI).</summary>
    public string? HelpKey { get; init; }

    // --- Informations de nettoyage : rendent l'élément auto-suffisant pour une suppression sûre. ---

    /// <summary>Nature de l'opération de suppression à appliquer.</summary>
    public FileActionKind ActionKind { get; init; } = FileActionKind.DeleteDirectoryContents;

    /// <summary>
    /// Racines dans lesquelles l'opération a le droit d'agir. Passées à <c>ISafePathValidator</c>
    /// avant toute suppression. Vide pour les actions non liées à un chemin (Corbeille).
    /// </summary>
    public IReadOnlyList<string> AllowedRoots { get; init; } = [];

    /// <summary>
    /// Dossiers dont le contenu doit être balayé lorsque l'élément regroupe plusieurs cibles
    /// (ex. les multiples dossiers de cache d'un profil navigateur). Vide = utiliser
    /// <see cref="PathOrIdentifier"/>. Chaque racine est revalidée avant suppression.
    /// </summary>
    public IReadOnlyList<string> SweepRoots { get; init; } = [];
}
