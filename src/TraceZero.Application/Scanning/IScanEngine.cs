using TraceZero.Domain.Scanning;

namespace TraceZero.Application.Scanning;

/// <summary>
/// Moteur de scan asynchrone : orchestre les fournisseurs en parallèle borné, isole leurs erreurs,
/// reporte la progression et supporte l'annulation (§12).
/// </summary>
public interface IScanEngine
{
    Task<ScanReport> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
