using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TraceZero.Application.Update;
using TraceZero.Domain.Update;

namespace TraceZero.Updater;

/// <summary>
/// Vérifie et évalue un manifeste de mise à jour signé (§28). La signature RSA-SHA256 est vérifiée avec
/// une clé publique embarquée : un manifeste dont la validation échoue renvoie
/// <see cref="UpdateAvailability.ManifestInvalid"/> et n'est jamais accepté. Le téléchargement, la
/// vérification SHA-256/Authenticode et l'exécution sont des étapes ultérieures qui ne s'exécutent
/// jamais sans un manifeste validé.
/// </summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly RSA? _rsa;

    public UpdateChecker(string publicKeyPem)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            _rsa = rsa;
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            _rsa = null;
        }
    }

    public UpdateCheckResult Check(string manifestJson, Version currentVersion, UpdateChannel channel)
    {
        if (_rsa is null)
        {
            return Invalid("Clé de vérification de mise à jour indisponible.");
        }

        UpdateManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<UpdateManifest>(manifestJson, JsonOptions);
        }
        catch (JsonException)
        {
            return Invalid("Manifeste illisible.");
        }

        if (manifest is null
            || string.IsNullOrWhiteSpace(manifest.Version)
            || string.IsNullOrWhiteSpace(manifest.Signature))
        {
            return Invalid("Manifeste incomplet.");
        }

        if (!VerifySignature(manifest))
        {
            return Invalid("Signature du manifeste invalide.");
        }

        // Canal : stable n'accepte que stable ; beta accepte beta ou stable.
        if (!ChannelMatches(manifest.Channel, channel))
        {
            return new UpdateCheckResult { Availability = UpdateAvailability.ChannelMismatch, Manifest = manifest };
        }

        if (!Version.TryParse(manifest.Version, out var manifestVersion))
        {
            return Invalid("Version du manifeste invalide.");
        }

        // Version installée sous le minimum supporté → mise à jour requise.
        if (Version.TryParse(manifest.MinimumSupportedVersion, out var minimum) && currentVersion < minimum)
        {
            return new UpdateCheckResult { Availability = UpdateAvailability.BelowMinimum, Manifest = manifest };
        }

        return manifestVersion > currentVersion
            ? new UpdateCheckResult { Availability = UpdateAvailability.UpdateAvailable, Manifest = manifest }
            : new UpdateCheckResult { Availability = UpdateAvailability.UpToDate, Manifest = manifest };
    }

    /// <summary>
    /// Contenu signé (ordre fixe des champs). Le serveur signe exactement cette chaîne ; toute
    /// altération d'un champ invalide la signature.
    /// </summary>
    public static string SignedPayload(UpdateManifest manifest) => string.Join(
        "\n",
        manifest.Version,
        manifest.Channel,
        manifest.Url,
        manifest.Sha256,
        manifest.MinimumSupportedVersion ?? string.Empty);

    private bool VerifySignature(UpdateManifest manifest)
    {
        try
        {
            var payload = Encoding.UTF8.GetBytes(SignedPayload(manifest));
            var signature = Convert.FromBase64String(manifest.Signature);
            return _rsa!.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            return false;
        }
    }

    private static bool ChannelMatches(string manifestChannel, UpdateChannel requested)
    {
        var isBetaManifest = string.Equals(manifestChannel, "beta", StringComparison.OrdinalIgnoreCase);
        var isStableManifest = string.Equals(manifestChannel, "stable", StringComparison.OrdinalIgnoreCase);

        return requested switch
        {
            UpdateChannel.Stable => isStableManifest,
            UpdateChannel.Beta => isBetaManifest || isStableManifest,
            _ => false,
        };
    }

    private static UpdateCheckResult Invalid(string message) =>
        new() { Availability = UpdateAvailability.ManifestInvalid, Message = message };
}
