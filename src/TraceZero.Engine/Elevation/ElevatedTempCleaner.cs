using TraceZero.Domain.Elevation;
using TraceZero.Engine.IO;
using TraceZero.Engine.Safety;

namespace TraceZero.Engine.Elevation;

/// <summary>
/// Nettoyage du contenu d'une racine temporaire nécessitant l'élévation (ex. <c>C:\Windows\Temp</c>).
///
/// Coeur testable, indépendant de tout privilège : la racine autorisée est fournie en paramètre
/// (les tests la font pointer sur un dossier jetable). Chaque fichier est :
/// <list type="bullet">
///   <item>revalidé par <see cref="ElevatedSafePathValidator"/> juste avant suppression (§9) ;</item>
///   <item>épargné s'il est plus récent que l'âge minimum (fichier possiblement en cours d'usage) ;</item>
///   <item>jamais forcé : un fichier verrouillé est compté en échec, pas supprimé brutalement.</item>
/// </list>
/// L'énumération ne suit jamais un point d'analyse (<see cref="SafeFileEnumerator"/>).
/// </summary>
public sealed class ElevatedTempCleaner
{
    private readonly TimeProvider _timeProvider;

    public ElevatedTempCleaner(TimeProvider? timeProvider = null) =>
        _timeProvider = timeProvider ?? TimeProvider.System;

    /// <param name="allowedRoot">Racine autorisée (son contenu est nettoyé ; la racine elle-même est préservée).</param>
    /// <param name="minimumAgeMinutes">Âge minimum d'un fichier pour être éligible (borné 0–1440).</param>
    public ElevatedResult Clean(string allowedRoot, int minimumAgeMinutes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(allowedRoot) || !Directory.Exists(allowedRoot))
        {
            // Rien à nettoyer : succès neutre (0 octet).
            return new ElevatedResult { Success = true };
        }

        var validator = new ElevatedSafePathValidator([allowedRoot]);
        var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime
            - TimeSpan.FromMinutes(Math.Clamp(minimumAgeMinutes, 0, 1440));

        long bytesFreed = 0;
        var succeeded = 0;
        var failed = 0;

        foreach (var entry in SafeFileEnumerator.EnumerateEntries(allowedRoot, recursive: true, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.LastWriteUtc > cutoffUtc)
            {
                continue; // Trop récent : possiblement en cours d'utilisation.
            }

            // Barrière de sécurité finale : le chemin doit toujours passer la validation élevée.
            if (!validator.Validate(entry.FullPath).IsAllowed)
            {
                failed++;
                continue;
            }

            try
            {
                File.Delete(entry.FullPath);
                bytesFreed += entry.Length;
                succeeded++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Fichier verrouillé / protégé : jamais forcé.
                failed++;
            }
        }

        return new ElevatedResult
        {
            Success = true,
            BytesFreed = bytesFreed,
            ActionsSucceeded = succeeded,
            ActionsFailed = failed,
        };
    }
}
