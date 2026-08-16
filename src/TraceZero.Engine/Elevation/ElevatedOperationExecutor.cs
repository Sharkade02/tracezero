using TraceZero.Domain.Elevation;

namespace TraceZero.Engine.Elevation;

/// <summary>
/// Autorité côté helper élevé (Phase 20, §30) : traduit une <see cref="ElevatedRequest"/> structurée en
/// une action concrète, en résolvant <b>elle-même</b> la liste d'autorisation dédiée à chaque opération.
///
/// Le client UI ne fournit jamais de chemin : l'exécuteur résout les emplacements à partir de
/// l'environnement système (ex. <c>%SystemRoot%\Temp</c>). Une requête dont l'opération ou la version de
/// protocole est inconnue est refusée sans effet de bord.
/// </summary>
public sealed class ElevatedOperationExecutor
{
    private readonly ElevatedTempCleaner _tempCleaner;
    private readonly Func<string> _windowsDirectoryProvider;

    public ElevatedOperationExecutor(
        ElevatedTempCleaner? tempCleaner = null,
        Func<string>? windowsDirectoryProvider = null)
    {
        _tempCleaner = tempCleaner ?? new ElevatedTempCleaner();
        _windowsDirectoryProvider = windowsDirectoryProvider
            ?? (() => Environment.GetFolderPath(Environment.SpecialFolder.Windows));
    }

    public ElevatedResult Execute(ElevatedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProtocolVersion != ElevatedRequest.CurrentProtocolVersion)
        {
            return ElevatedResult.Fail(
                $"Version de protocole non supportée : {request.ProtocolVersion} " +
                $"(attendu {ElevatedRequest.CurrentProtocolVersion}).");
        }

        return request.Operation switch
        {
            ElevatedOperation.CleanWindowsTemp => CleanWindowsTemp(request, cancellationToken),
            _ => ElevatedResult.Fail($"Opération élevée inconnue : {request.Operation}."),
        };
    }

    private ElevatedResult CleanWindowsTemp(ElevatedRequest request, CancellationToken cancellationToken)
    {
        var windows = _windowsDirectoryProvider();
        if (string.IsNullOrWhiteSpace(windows))
        {
            return ElevatedResult.Fail("Répertoire Windows introuvable.");
        }

        // Liste d'autorisation dédiée, résolue côté helper — jamais issue du client.
        var windowsTemp = Path.Combine(windows, "Temp");
        return _tempCleaner.Clean(windowsTemp, request.MinimumAgeMinutes, cancellationToken);
    }
}
