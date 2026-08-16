namespace TraceZero.Application.Privacy;

/// <summary>
/// Efface des traces de confidentialité dans le registre, sous HKEY_CURRENT_USER uniquement, et
/// seulement pour des clés explicitement autorisées (§9, §43). Analogue registre de
/// <c>ISafePathValidator</c> : refus par défaut.
/// </summary>
public interface IRegistryTraceCleaner
{
    /// <summary>La clé (relative à HKCU) est-elle autorisée au nettoyage ?</summary>
    bool IsAllowed(string hkcuSubKey);

    /// <summary>Compte les entrées (valeurs + sous-clés) présentes sous la clé, ou 0 si absente/refusée.</summary>
    int CountEntries(string hkcuSubKey);

    /// <summary>
    /// Efface les valeurs et sous-clés sous la clé (la clé elle-même est conservée). Ne fait rien si
    /// la clé n'est pas autorisée. Retourne le nombre d'entrées supprimées.
    /// </summary>
    int ClearKey(string hkcuSubKey);
}
