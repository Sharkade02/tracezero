namespace TraceZero.Domain.Update;

/// <summary>Canal de distribution (§28).</summary>
public enum UpdateChannel
{
    Stable = 0,
    Beta = 1,
}

/// <summary>Résultat de l'évaluation d'un manifeste de mise à jour.</summary>
public enum UpdateAvailability
{
    /// <summary>Déjà à jour (aucune version plus récente valide).</summary>
    UpToDate = 0,

    /// <summary>Une mise à jour signée et plus récente est disponible.</summary>
    UpdateAvailable = 1,

    /// <summary>La version installée est sous le minimum supporté (mise à jour requise).</summary>
    BelowMinimum = 2,

    /// <summary>Manifeste illisible ou signature invalide — jamais exécuté (§28).</summary>
    ManifestInvalid = 3,

    /// <summary>Le manifeste ne correspond pas au canal demandé.</summary>
    ChannelMismatch = 4,
}

/// <summary>
/// Manifeste de mise à jour publié par le serveur (§28). La <see cref="Signature"/> couvre les champs
/// signés ; une validation échouée interdit toute exécution.
/// </summary>
public sealed record UpdateManifest
{
    public required string Version { get; init; }

    public required string Channel { get; init; }

    /// <summary>URL HTTPS du binaire.</summary>
    public required string Url { get; init; }

    /// <summary>Empreinte SHA-256 attendue du binaire.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Signature RSA-SHA256 (base64) du contenu signé.</summary>
    public required string Signature { get; init; }

    /// <summary>Version minimale supportée : en dessous, une mise à jour est requise.</summary>
    public string? MinimumSupportedVersion { get; init; }
}

/// <summary>Résultat d'une vérification de mise à jour, avec le manifeste validé le cas échéant.</summary>
public sealed record UpdateCheckResult
{
    public required UpdateAvailability Availability { get; init; }

    /// <summary>Manifeste validé (présent seulement si la signature est vérifiée).</summary>
    public UpdateManifest? Manifest { get; init; }

    public string? Message { get; init; }
}
