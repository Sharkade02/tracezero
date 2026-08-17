using System.Net.Http;

namespace TraceZero.App.Services;

/// <summary>Récupère le manifeste de mise à jour depuis une source HTTPS (§28).</summary>
public interface IManifestSource
{
    /// <summary>Vrai si une URL HTTPS est configurée (sinon l'updater est désactivé).</summary>
    bool IsConfigured { get; }

    /// <summary>Télécharge le manifeste JSON, ou <c>null</c> si non configuré / échec réseau.</summary>
    Task<string?> FetchAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Source HTTPS du manifeste. Impose HTTPS ; échoue silencieusement (retourne null) hors ligne ou si non
/// configurée. Aucune donnée n'est envoyée : simple GET du manifeste public.
/// </summary>
public sealed class HttpManifestSource(string manifestUrl) : IManifestSource
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(manifestUrl)
        && manifestUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> FetchAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            return await http.GetStringAsync(manifestUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // Hors ligne / proxy indisponible / URL invalide : pas de mise à jour signalée, sans planter.
            return null;
        }
    }
}
