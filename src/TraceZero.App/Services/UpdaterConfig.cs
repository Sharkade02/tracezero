using TraceZero.Domain.Update;

namespace TraceZero.App.Services;

/// <summary>
/// Configuration de l'updater (§28). Vide par défaut → **désactivé** : aucune vérification réseau tant
/// qu'un endpoint et une clé publique de production ne sont pas renseignés. On ne simule jamais de
/// mise à jour.
/// </summary>
public static class UpdaterConfig
{
    /// <summary>URL HTTPS du manifeste signé. Vide = updater désactivé.</summary>
    public const string ManifestUrl = "";

    /// <summary>Clé publique (PEM) de vérification de signature du manifeste. Vide = updater désactivé.</summary>
    public const string PublicKeyPem = "";

    /// <summary>Canal de mise à jour de cette build.</summary>
    public const UpdateChannel Channel = UpdateChannel.Stable;
}
