using Microsoft.Extensions.DependencyInjection;
using TraceZero.Application.Browsers;
using TraceZero.Application.Scanning;

namespace TraceZero.Browsers.DependencyInjection;

public static class BrowsersServiceCollectionExtensions
{
    /// <summary>Enregistre la détection des navigateurs et le fournisseur de scan de leurs caches (§14).</summary>
    public static IServiceCollection AddTraceZeroBrowsers(this IServiceCollection services)
    {
        services.AddSingleton<IBrowserDetector, BrowserDetector>();
        services.AddSingleton<IScanProvider, BrowserCacheScanProvider>();
        return services;
    }
}
