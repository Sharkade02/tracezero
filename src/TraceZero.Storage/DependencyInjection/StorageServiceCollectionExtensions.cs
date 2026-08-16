using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Disk;

namespace TraceZero.Storage.DependencyInjection;

public static class StorageServiceCollectionExtensions
{
    /// <summary>Enregistre les services de stockage/disque (§20).</summary>
    public static IServiceCollection AddTraceZeroStorage(this IServiceCollection services)
    {
        services.AddSingleton<IDriveQueryService, DriveQueryService>();
        return services;
    }
}
