using TraceZero.Domain.Software;

namespace TraceZero.Application.Software;

/// <summary>
/// Détecte les logiciels installés obsolètes via le Windows Package Manager (winget), source officielle
/// et signée (§23). N'installe rien en propre : la mise à jour est lancée par winget, visible par
/// l'utilisateur. Jamais de scraping de sources douteuses.
/// </summary>
public interface ISoftwareUpdateService
{
    /// <summary>Liste les mises à jour disponibles, ou un rapport « indisponible » si winget est absent.</summary>
    Task<SoftwareUpdateReport> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lance la mise à jour d'un package via winget (fenêtre visible). Retourne vrai si lancé.</summary>
    bool LaunchUpdate(string packageId);

    /// <summary>Lance la mise à jour de tous les packages via winget. Retourne vrai si lancé.</summary>
    bool LaunchUpdateAll();
}
