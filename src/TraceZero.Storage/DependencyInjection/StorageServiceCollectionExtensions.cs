using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Diagnostics;
using TraceZero.Application.Disk;
using TraceZero.Application.Erasure;

namespace TraceZero.Storage.DependencyInjection;

public static class StorageServiceCollectionExtensions
{
    /// <summary>Enregistre les services de stockage/disque (§20), la santé disque (Phase 28) et la détection de média (§19).</summary>
    public static IServiceCollection AddTraceZeroStorage(this IServiceCollection services)
    {
        services.AddSingleton<IDriveQueryService, DriveQueryService>();
        services.AddSingleton<IDiskHealthService, DiskHealthService>();
        services.AddSingleton<IMemoryInfoService, MemoryInfoService>();
        services.AddSingleton<IStorageMediaProbe, StorageMediaProbe>();
        return services;
    }
}
