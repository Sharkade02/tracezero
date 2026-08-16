namespace TraceZero.Domain.Elevation;

/// <summary>
/// Opérations que le helper élevé (<c>TraceZero.Elevated.exe</c>) sait exécuter (Phase 20, §30).
///
/// L'ensemble est <b>fermé</b> : le helper ne fait jamais confiance à un chemin arbitraire fourni par
/// l'UI. Chaque opération est liée, côté helper, à une liste d'autorisation dédiée et revalidée.
/// </summary>
public enum ElevatedOperation
{
    /// <summary>Nettoie le contenu de <c>%SystemRoot%\Temp</c> (déféré depuis la Phase 3).</summary>
    CleanWindowsTemp = 1,
}

/// <summary>
/// Commande structurée envoyée au helper élevé. C'est le seul vocabulaire accepté :
/// aucune commande hors de ce contrat n'est exécutée (« ne jamais faire confiance au client UI », §30).
/// </summary>
public sealed record ElevatedRequest
{
    /// <summary>Version du protocole IPC. Le helper refuse toute version inconnue.</summary>
    public const int CurrentProtocolVersion = 1;

    public int ProtocolVersion { get; init; } = CurrentProtocolVersion;

    public required ElevatedOperation Operation { get; init; }

    /// <summary>
    /// Âge minimum (minutes) sous lequel un fichier n'est pas supprimé (fichiers en cours d'usage).
    /// Borné côté helper entre 0 et 1440 pour éviter toute valeur aberrante.
    /// </summary>
    public int MinimumAgeMinutes { get; init; } = 60;
}

/// <summary>
/// Résultat structuré renvoyé par le helper. Toujours renseigné, même en cas d'échec (§41 : jamais un
/// code d'erreur nu).
/// </summary>
public sealed record ElevatedResult
{
    public required bool Success { get; init; }

    public long BytesFreed { get; init; }

    public int ActionsSucceeded { get; init; }

    public int ActionsFailed { get; init; }

    /// <summary>Message lisible en cas d'échec global (élévation refusée, protocole invalide…).</summary>
    public string? ErrorMessage { get; init; }

    public static ElevatedResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
