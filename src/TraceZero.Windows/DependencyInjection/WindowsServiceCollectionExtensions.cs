using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Apps;
using TraceZero.Application.Automation;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Disk;
using TraceZero.Application.Diagnostics;
using TraceZero.Application.Elevation;
using TraceZero.Application.Privacy;
using TraceZero.Application.Protection;
using TraceZero.Application.Software;
using TraceZero.Windows.Apps;
using TraceZero.Windows.Automation;
using TraceZero.Windows.Diagnostics;
using TraceZero.Windows.Disk;
using TraceZero.Windows.Elevation;
using TraceZero.Windows.Privacy;
using TraceZero.Windows.Protection;
using TraceZero.Windows.RecycleBin;

namespace TraceZero.Windows.DependencyInjection;

public static class WindowsServiceCollectionExtensions
{
    /// <summary>Enregistre les services spécifiques à Windows (Corbeille, traces de confidentialité…).</summary>
    public static IServiceCollection AddTraceZeroWindows(this IServiceCollection services)
    {
        services.AddSingleton<IRecycleBinService, RecycleBinService>();

        // Nettoyeur registre borné par la liste d'autorisation issue du catalogue de traces (§9, §15).
        services.AddSingleton<IRegistryTraceCleaner>(_ =>
            new RegistryTraceCleaner(WindowsPrivacyCatalog.RegistryAllowList()));
        services.AddSingleton<IPrivacyInspector, WindowsPrivacyInspector>();

        // Sauvegarde/restauration de traces registre HKCU avant nettoyage réversible (Phase 7, §17).
        services.AddSingleton<IRegistryBackupService, RegistryBackupService>();

        // Suppression réversible (Corbeille) pour le nettoyage manuel des gros fichiers (§20).
        services.AddSingleton<IRecycleFileService, RecycleFileService>();

        // Applications & démarrage (§22).
        services.AddSingleton<IInstalledAppService, InstalledAppService>();
        services.AddSingleton<IStartupService, StartupService>();

        // Automatisation via le Planificateur de tâches (§15).
        services.AddSingleton<IAutomationService, AutomationService>();

        // Impact au démarrage mesuré par Windows (Phase 28), lecture seule.
        services.AddSingleton<IStartupImpactService, StartupImpactService>();

        // Inventaire des pilotes (Driver Health, Phase 14), lecture seule — jamais d'installation.
        services.AddSingleton<IDriverHealthService, DriverHealthService>();

        // Mises à jour logicielles via winget (Software Updater, Phase 13) — source officielle signée.
        services.AddSingleton<ISoftwareUpdateService, Software.WingetUpdateService>();

        // Élévation à la demande via le helper séparé (Phase 20, §30) — jamais admin par défaut.
        services.AddSingleton<IElevatedOperationService, ElevatedOperationClient>();

        return services;
    }
}
