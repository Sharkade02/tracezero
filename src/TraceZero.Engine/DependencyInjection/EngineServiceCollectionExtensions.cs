using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraceZero.Application.Browsers;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Disk;
using TraceZero.Application.Duplicates;
using TraceZero.Application.Erasure;
using TraceZero.Application.Ntfs;
using TraceZero.Application.Privacy;
using TraceZero.Application.Safety;
using TraceZero.Application.Scanning;
using TraceZero.Engine.Cleaning;
using TraceZero.Engine.Disk;
using TraceZero.Engine.Duplicates;
using TraceZero.Engine.Erasure;
using TraceZero.Engine.Ntfs;
using TraceZero.Engine.Rules;
using TraceZero.Engine.Safety;
using TraceZero.Engine.Scanning;

namespace TraceZero.Engine.DependencyInjection;

public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre la couche de sécurité, les fournisseurs de scan (règles Windows + Corbeille) et
    /// les moteurs de scan/nettoyage.
    /// </summary>
    public static IServiceCollection AddTraceZeroEngine(this IServiceCollection services)
    {
        // Sécurité (§9).
        services.AddSingleton<IKnownFolders, WindowsKnownFolders>();
        services.AddSingleton<ISafePathValidator>(sp =>
            new SafePathValidator(sp.GetRequiredService<IKnownFolders>()));

        // Fournisseurs de scan issus des règles Windows standard (Phase 3).
        foreach (var rule in WindowsCleaningRules.BuildDefaultRules())
        {
            services.AddSingleton<IScanProvider>(new FileSweepScanProvider(rule));
        }

        // Corbeille (nécessite IRecycleBinService, fourni par la couche Windows).
        services.AddSingleton<IScanProvider, RecycleBinScanProvider>();

        // Recherche de gros fichiers (§20) et de doublons (§21).
        services.AddSingleton<ILargeFileScanner, LargeFileScanner>();
        services.AddSingleton<IDuplicateFinder, DuplicateFinder>();

        // Effacement sécurisé (§19) : fichier (garde-fou dédié) + espace libre.
        services.AddSingleton<ISecureFileEraser>(sp =>
            new SecureEraser(sp.GetRequiredService<IKnownFolders>()));
        services.AddSingleton<IFreeSpaceWiper, FreeSpaceWiper>();

        // Analyse NTFS avancée en lecture seule (Phase 8, §18) — Mode Expert, jamais destructive.
        services.AddSingleton<INtfsAnalyzer, NtfsAnalyzer>();

        // Moteurs.
        services.AddSingleton<IScanEngine, ScanEngine>();
        services.AddSingleton<ICleaningEngine>(sp => new CleaningEngine(
            sp.GetRequiredService<ISafePathValidator>(),
            sp.GetService<IRecycleBinService>(),
            sp.GetService<IRegistryTraceCleaner>(),
            sp.GetService<IBrowserHistoryCleaner>(),
            sp.GetService<ILogger<CleaningEngine>>()));

        return services;
    }
}
