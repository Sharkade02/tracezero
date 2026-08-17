namespace TraceZero.Domain.Software;

/// <summary>
/// Une mise à jour logicielle disponible, détectée via une source fiable (§23). Priorité au
/// Windows Package Manager (winget) ; jamais de scraping de sites de téléchargement douteux.
/// L'installation est déléguée à la source officielle (winget), visible par l'utilisateur.
/// </summary>
public sealed record SoftwareUpdate
{
    public required string Name { get; init; }

    /// <summary>Identifiant de package (ex. identifiant winget), utilisé pour lancer la mise à jour.</summary>
    public required string Id { get; init; }

    public required string InstalledVersion { get; init; }

    public required string AvailableVersion { get; init; }

    /// <summary>Source de la mise à jour (ex. « winget »).</summary>
    public required string Source { get; init; }
}

/// <summary>Résultat d'une recherche de mises à jour logicielles, avec un état de disponibilité honnête.</summary>
public sealed record SoftwareUpdateReport
{
    public required IReadOnlyList<SoftwareUpdate> Updates { get; init; }

    /// <summary>Faux si le gestionnaire de paquets (winget) est indisponible sur cette machine.</summary>
    public required bool SourceAvailable { get; init; }

    public static SoftwareUpdateReport Unavailable { get; } =
        new() { Updates = [], SourceAvailable = false };
}
