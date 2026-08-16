using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Exclusions;
using TraceZero.Application.History;
using TraceZero.Application.Licensing;
using TraceZero.Persistence.Licensing;

namespace TraceZero.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Enregistre le stockage local (historique SQLite, exclusions JSON, licence).</summary>
    public static IServiceCollection AddTraceZeroPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ICleanupHistoryStore>(_ => new SqliteCleanupHistoryStore(TraceZeroPaths.HistoryDatabase));
        services.AddSingleton<IExclusionStore>(_ => new JsonExclusionStore(TraceZeroPaths.ExclusionsFile));
        services.AddSingleton<ILicenseService>(_ => new LicenseService(LicenseKeys.PublicKeyPem, TraceZeroPaths.LicenseFile));
        return services;
    }
}
