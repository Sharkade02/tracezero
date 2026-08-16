using Microsoft.Win32;
using TraceZero.Application.Privacy;

namespace TraceZero.Windows.Privacy;

/// <summary>
/// Nettoyage de traces registre sous HKCU, borné par une liste d'autorisation (§9, §43).
/// Toute clé absente de la liste est refusée : le nettoyage ne peut jamais toucher une clé arbitraire.
/// </summary>
public sealed class RegistryTraceCleaner : IRegistryTraceCleaner
{
    private readonly HashSet<string> _allowList;

    public RegistryTraceCleaner(IEnumerable<string> allowedSubKeys) =>
        _allowList = new HashSet<string>(allowedSubKeys.Select(Normalize), StringComparer.OrdinalIgnoreCase);

    public bool IsAllowed(string hkcuSubKey) => _allowList.Contains(Normalize(hkcuSubKey));

    public int CountEntries(string hkcuSubKey)
    {
        if (!IsAllowed(hkcuSubKey))
        {
            return 0;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(hkcuSubKey, writable: false);
            if (key is null)
            {
                return 0;
            }

            // Valeurs nommées (on ignore la valeur par défaut vide) + sous-clés.
            var values = key.GetValueNames().Count(n => !string.IsNullOrEmpty(n));
            return values + key.SubKeyCount;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return 0;
        }
    }

    public int ClearKey(string hkcuSubKey)
    {
        if (!IsAllowed(hkcuSubKey))
        {
            return 0;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(hkcuSubKey, writable: true);
            if (key is null)
            {
                return 0;
            }

            var removed = 0;

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    key.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false);
                    removed++;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
                {
                    // Sous-clé verrouillée : ignorée, jamais forcée.
                }
            }

            foreach (var valueName in key.GetValueNames())
            {
                if (string.IsNullOrEmpty(valueName))
                {
                    continue;
                }

                try
                {
                    key.DeleteValue(valueName, throwOnMissingValue: false);
                    removed++;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
                {
                }
            }

            return removed;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return 0;
        }
    }

    private static string Normalize(string subKey) =>
        subKey.Trim().Replace('/', '\\').Trim('\\');
}
