using TraceZero.Domain.Protection;

namespace TraceZero.Application.Protection;

/// <summary>
/// Sauvegarde et restauration de clés de registre HKCU (§17). Opère uniquement sous HKEY_CURRENT_USER
/// (aucune élévation requise) et ne touche jamais une clé arbitraire : l'appelant fournit la sous-clé,
/// déjà validée par le catalogue de traces autorisées.
/// </summary>
public interface IRegistryBackupService
{
    /// <summary>
    /// Capture récursivement le contenu d'une sous-clé HKCU. Retourne <c>null</c> si la clé est absente,
    /// ou un instantané (éventuellement vide) sinon.
    /// </summary>
    RegistryKeySnapshot? Capture(string hkcuSubKey);

    /// <summary>
    /// Restaure un instantané vers sa sous-clé HKCU (recrée valeurs et sous-clés). Retourne le nombre
    /// d'entrées effectivement réécrites.
    /// </summary>
    int Restore(string hkcuSubKey, RegistryKeySnapshot snapshot);
}
