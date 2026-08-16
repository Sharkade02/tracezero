using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Apps;
using TraceZero.Application.Automation;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Disk;
using TraceZero.Application.Privacy;
using TraceZero.Windows.Apps;
using TraceZero.Windows.Automation;
using TraceZero.Windows.Disk;
using TraceZero.Windows.Privacy;
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

        // Suppression réversible (Corbeille) pour le nettoyage manuel des gros fichiers (§20).
        services.AddSingleton<IRecycleFileService, RecycleFileService>();

        // Applications & démarrage (§22).
        services.AddSingleton<IInstalledAppService, InstalledAppService>();
        services.AddSingleton<IStartupService, StartupService>();

        // Automatisation via le Planificateur de tâches (§15).
        services.AddSingleton<IAutomationService, AutomationService>();

        return services;
    }
}
