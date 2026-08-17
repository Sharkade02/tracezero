using System.Text;
using Microsoft.Win32;
using TraceZero.Application.Protection;
using TraceZero.Domain.Protection;

namespace TraceZero.Windows.Protection;

/// <summary>
/// Sauvegarde/restauration de clés de registre sous HKCU (§17). Aucune élévation : HKEY_CURRENT_USER
/// est accessible en écriture par l'utilisateur courant. La sous-clé est fournie par l'appelant (issue
/// du catalogue de traces autorisées) — ce service ne décide jamais seul quoi sauvegarder.
/// </summary>
public sealed class RegistryBackupService : IRegistryBackupService
{
    // Séparateur interne pour encoder les REG_MULTI_SZ (une chaîne ne peut pas contenir un caractère nul).
    private const char MultiStringSeparator = '\0';

    public RegistryKeySnapshot? Capture(string hkcuSubKey)
    {
        var normalized = Normalize(hkcuSubKey);

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(normalized, writable: false);
            return key is null ? null : CaptureKey(key, name: string.Empty);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    public int Restore(string hkcuSubKey, RegistryKeySnapshot snapshot)
    {
        var normalized = Normalize(hkcuSubKey);

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(normalized, writable: true);
            return key is null ? 0 : RestoreKey(key, snapshot);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return 0;
        }
    }

    private static RegistryKeySnapshot CaptureKey(RegistryKey key, string name)
    {
        var values = new List<RegistryValueSnapshot>();
        foreach (var valueName in key.GetValueNames())
        {
            if (string.IsNullOrEmpty(valueName))
            {
                // On ignore la valeur par défaut, cohérent avec le nettoyeur de traces.
                continue;
            }

            var kind = key.GetValueKind(valueName);
            var raw = key.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (raw is null || !TryEncode(kind, raw, out var encoded))
            {
                continue;
            }

            values.Add(new RegistryValueSnapshot
            {
                Name = valueName,
                Kind = (int)kind,
                EncodedValue = encoded,
            });
        }

        var subKeys = new List<RegistryKeySnapshot>();
        foreach (var subKeyName in key.GetSubKeyNames())
        {
            try
            {
                using var subKey = key.OpenSubKey(subKeyName, writable: false);
                if (subKey is not null)
                {
                    subKeys.Add(CaptureKey(subKey, subKeyName));
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
            {
                // Sous-clé illisible : ignorée, jamais devinée.
            }
        }

        return new RegistryKeySnapshot
        {
            Name = name,
            Values = values,
            SubKeys = subKeys,
        };
    }

    private static int RestoreKey(RegistryKey key, RegistryKeySnapshot snapshot)
    {
        var restored = 0;

        foreach (var value in snapshot.Values)
        {
            if (TryDecode(value, out var data, out var kind))
            {
                key.SetValue(value.Name, data, kind);
                restored++;
            }
        }

        foreach (var subSnapshot in snapshot.SubKeys)
        {
            using var subKey = key.CreateSubKey(subSnapshot.Name, writable: true);
            if (subKey is not null)
            {
                restored += RestoreKey(subKey, subSnapshot);
            }
        }

        return restored;
    }

    private static bool TryEncode(RegistryValueKind kind, object raw, out string encoded)
    {
        switch (kind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                encoded = raw as string ?? string.Empty;
                return true;

            case RegistryValueKind.DWord:
                encoded = Convert.ToInt32(raw, System.Globalization.CultureInfo.InvariantCulture)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;

            case RegistryValueKind.QWord:
                encoded = Convert.ToInt64(raw, System.Globalization.CultureInfo.InvariantCulture)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                return true;

            case RegistryValueKind.Binary:
                encoded = Convert.ToBase64String((byte[])raw);
                return true;

            case RegistryValueKind.MultiString:
                var joined = string.Join(MultiStringSeparator, (string[])raw);
                encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(joined));
                return true;

            default:
                // Type non pris en charge (ex. None) : on ne l'invente pas.
                encoded = string.Empty;
                return false;
        }
    }

    private static bool TryDecode(RegistryValueSnapshot value, out object data, out RegistryValueKind kind)
    {
        kind = (RegistryValueKind)value.Kind;
        switch (kind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                data = value.EncodedValue;
                return true;

            case RegistryValueKind.DWord:
                data = int.Parse(value.EncodedValue, System.Globalization.CultureInfo.InvariantCulture);
                return true;

            case RegistryValueKind.QWord:
                data = long.Parse(value.EncodedValue, System.Globalization.CultureInfo.InvariantCulture);
                return true;

            case RegistryValueKind.Binary:
                data = Convert.FromBase64String(value.EncodedValue);
                return true;

            case RegistryValueKind.MultiString:
                var joined = Encoding.UTF8.GetString(Convert.FromBase64String(value.EncodedValue));
                data = joined.Length == 0 ? Array.Empty<string>() : joined.Split(MultiStringSeparator);
                return true;

            default:
                data = string.Empty;
                return false;
        }
    }

    private static string Normalize(string subKey) =>
        subKey.Trim().Replace('/', '\\').Trim('\\');
}
