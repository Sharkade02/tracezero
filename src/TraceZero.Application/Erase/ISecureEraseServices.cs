using TraceZero.Domain.Diagnostics;
using TraceZero.Domain.Erasure;

namespace TraceZero.Application.Erasure;

/// <summary>
/// Effacement sécurisé d'un fichier explicitement choisi par l'utilisateur (§19). À la différence du
/// nettoyage (allow-list stricte), la cible est désignée par l'utilisateur ; le garde-fou refuse
/// néanmoins tout ce qui est dangereux : dossiers système, racines de volume, points d'analyse,
/// répertoires. Irréversible par nature — jamais présenté comme réversible.
/// </summary>
public interface ISecureFileEraser
{
    /// <summary>Retourne <c>null</c> si la cible est effaçable, sinon une raison de refus honnête.</summary>
    string? ValidateTarget(string path);

    Task<SecureEraseResult> EraseFileAsync(string path, SecureEraseMethod method, CancellationToken cancellationToken = default);
}

/// <summary>
/// Effacement de l'espace libre d'un lecteur (§19) : n'écrit qu'un fichier temporaire de remplissage,
/// ne touche <b>jamais</b> aux fichiers existants. Annulable, à priorité basse, avec estimation.
/// </summary>
public interface IFreeSpaceWiper
{
    /// <summary>
    /// Remplit l'espace libre sous <paramref name="workingDirectory"/> (un dossier accessible du lecteur
    /// cible) avec des données, dans la limite de <paramref name="maxBytes"/> (0 = jusqu'à disque plein),
    /// puis supprime le fichier de remplissage. Ne supprime aucun fichier existant.
    /// </summary>
    Task<FreeSpaceWipeResult> WipeAsync(
        string workingDirectory,
        long maxBytes,
        IProgress<FreeSpaceWipeProgress>? progress,
        CancellationToken cancellationToken = default);
}

/// <summary>Détecte le type de média (HDD/SSD) d'un chemin, pour adapter l'avertissement honnête (§19).</summary>
public interface IStorageMediaProbe
{
    DiskMediaKind GetMediaForPath(string path);
}
