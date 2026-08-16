using Microsoft.Win32;
using TraceZero.Application.Apps;
using TraceZero.Domain.Apps;

namespace TraceZero.Windows.Apps;

/// <summary>
/// Gestionnaire des entrées de démarrage (§22). Les entrées « Run » de l'utilisateur peuvent être
/// activées/désactivées de façon réversible : désactiver déplace la valeur vers une sauvegarde
/// (jamais de suppression sèche). Les entrées machine (HKLM) et les dossiers de démarrage sont
/// affichés en lecture seule (nécessiteraient une élévation).
/// </summary>
public sealed class StartupService : IStartupService
{
    private const string HklmRunPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _runSubKey;
    private readonly string _backupSubKey;

    public StartupService()
        : this(@"Software\Microsoft\Windows\CurrentVersion\Run", @"Software\TraceZero\StartupBackup\Run")
    {
    }

    public StartupService(string runSubKey, string backupSubKey)
    {
        _runSubKey = runSubKey;
        _backupSubKey = backupSubKey;
    }

    public IReadOnlyList<StartupEntry> GetStartupEntries()
    {
        var entries = new List<StartupEntry>();

        // Entrées utilisateur actives (HKCU Run).
        ReadRun(Registry.CurrentUser, _runSubKey, StartupLocation.RunCurrentUser, enabled: true, canToggle: true, "hkcu", entries);

        // Entrées utilisateur désactivées (sauvegardées par TraceZero).
        ReadRun(Registry.CurrentUser, _backupSubKey, StartupLocation.RunCurrentUser, enabled: false, canToggle: true, "hkcu-off", entries);

        // Entrées machine (lecture seule).
        ReadRun(Registry.LocalMachine, HklmRunPath, StartupLocation.RunLocalMachine, enabled: true, canToggle: false, "hklm", entries);

        // Dossier de démarrage de l'utilisateur (lecture seule pour l'instant).
        ReadStartupFolder(entries);

        return entries;
    }

    private static void ReadRun(RegistryKey hive, string subKey, StartupLocation location, bool enabled, bool canToggle, string idPrefix, List<StartupEntry> entries)
    {
        try
        {
            using var key = hive.OpenSubKey(subKey);
            if (key is null)
            {
                return;
            }

            foreach (var name in key.GetValueNames())
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                entries.Add(new StartupEntry
                {
                    Id = $"{idPrefix}::{name}",
                    Name = name,
                    Command = key.GetValue(name)?.ToString() ?? string.Empty,
                    Location = location,
                    IsEnabled = enabled,
                    CanToggle = canToggle,
                });
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void ReadStartupFolder(List<StartupEntry> entries)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(folder, "*.lnk"))
            {
                entries.Add(new StartupEntry
                {
                    Id = $"folder::{Path.GetFileName(file)}",
                    Name = Path.GetFileNameWithoutExtension(file),
                    Command = file,
                    Location = StartupLocation.StartupFolder,
                    IsEnabled = true,
                    CanToggle = false,
                });
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    public bool SetEnabled(StartupEntry entry, bool enabled)
    {
        if (entry.Location != StartupLocation.RunCurrentUser || !entry.CanToggle)
        {
            return false;
        }

        // Désactiver = déplacer Run -> sauvegarde ; activer = déplacer sauvegarde -> Run.
        return enabled
            ? MoveValue(_backupSubKey, _runSubKey, entry.Name)
            : MoveValue(_runSubKey, _backupSubKey, entry.Name);
    }

    private static bool MoveValue(string fromSubKey, string toSubKey, string valueName)
    {
        try
        {
            using var from = Registry.CurrentUser.OpenSubKey(fromSubKey, writable: true);
            var value = from?.GetValue(valueName);
            if (from is null || value is null)
            {
                return false;
            }

            using var to = Registry.CurrentUser.CreateSubKey(toSubKey);
            to!.SetValue(valueName, value);   // sauvegarde avant modification
            from.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
