using TraceZero.Domain.Apps;

namespace TraceZero.Application.Apps;

/// <summary>Liste les applications installées et lance leur désinstallateur déclaré (§22).</summary>
public interface IInstalledAppService
{
    IReadOnlyList<AppInstallation> GetInstalledApps();

    /// <summary>Lance le mécanisme de désinstallation déclaré par l'application. Ne supprime jamais manuellement.</summary>
    bool LaunchUninstaller(AppInstallation app);
}

/// <summary>
/// Gestionnaire des entrées de démarrage (§22). Les modifications sont réversibles : une sauvegarde
/// est effectuée avant toute désactivation.
/// </summary>
public interface IStartupService
{
    IReadOnlyList<StartupEntry> GetStartupEntries();

    /// <summary>Active ou désactive une entrée (réversible). Retourne vrai en cas de succès.</summary>
    bool SetEnabled(StartupEntry entry, bool enabled);
}
