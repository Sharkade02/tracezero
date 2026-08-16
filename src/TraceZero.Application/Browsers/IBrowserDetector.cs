using TraceZero.Domain.Browsers;

namespace TraceZero.Application.Browsers;

/// <summary>Détecte les navigateurs installés, leurs profils et leur état d'exécution (§14).</summary>
public interface IBrowserDetector
{
    IReadOnlyList<DetectedBrowser> DetectInstalledBrowsers();
}
