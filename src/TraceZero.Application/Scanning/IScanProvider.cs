using TraceZero.Domain;

namespace TraceZero.Application.Scanning;

/// <summary>
/// Source d'éléments scannables (une catégorie, une règle, un navigateur…). Les fournisseurs sont
/// isolés : la défaillance de l'un ne doit pas interrompre le scan global (§12).
/// </summary>
public interface IScanProvider
{
    string Id { get; }

    string DisplayName { get; }

    Category Category { get; }

    /// <summary>Énumère les éléments trouvés, de façon asynchrone et annulable, avec des tailles réelles.</summary>
    /// <param name="reporter">Reçoit la progression fine (fichiers examinés) pendant le balayage.</param>
    IAsyncEnumerable<ScanItem> ScanAsync(IScanProgressReporter reporter, CancellationToken cancellationToken);
}
