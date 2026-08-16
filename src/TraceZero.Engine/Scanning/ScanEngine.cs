using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Scanning;

namespace TraceZero.Engine.Scanning;

/// <summary>
/// Moteur de scan (§12) : exécute les fournisseurs en parallèle borné, isole leurs erreurs,
/// reporte une progression fluide (fichiers examinés, §23) et respecte l'annulation.
/// </summary>
public sealed class ScanEngine : IScanEngine
{
    private readonly List<IScanProvider> _providers;
    private readonly ILogger<ScanEngine>? _logger;
    private readonly int _maxParallelism;

    public ScanEngine(IEnumerable<IScanProvider> providers, ILogger<ScanEngine>? logger = null)
    {
        _providers = providers.ToList();
        _logger = logger;
        _maxParallelism = Math.Max(2, Environment.ProcessorCount / 2);
    }

    public async Task<ScanReport> ScanAsync(IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var items = new ConcurrentBag<ScanItem>();
        var errors = new ConcurrentBag<ProviderError>();
        var aggregator = new ProgressAggregator(progress, _providers.Count);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxParallelism,
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(_providers, options, async (provider, token) =>
        {
            try
            {
                await foreach (var item in provider.ScanAsync(aggregator, token).WithCancellation(token))
                {
                    items.Add(item);
                    aggregator.OnItemFound(item.SizeBytes);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Un fournisseur défaillant ne fait pas échouer le scan global (§12).
                errors.Add(new ProviderError(provider.Id, ex.Message));
                _logger?.LogWarning(ex, "Le fournisseur de scan {ProviderId} a échoué.", provider.Id);
            }
            finally
            {
                aggregator.OnProviderCompleted(provider.DisplayName);
            }
        });

        stopwatch.Stop();

        return new ScanReport
        {
            Items = items.OrderByDescending(i => i.SizeBytes).ToList(),
            Elapsed = stopwatch.Elapsed,
            Errors = errors.ToList(),
        };
    }

    /// <summary>
    /// Agrège la progression de tous les fournisseurs et limite la fréquence des notifications UI
    /// (au plus une tous les ~120 ms), les événements de fin de fournisseur étant toujours émis.
    /// </summary>
    private sealed class ProgressAggregator(IProgress<ScanProgress>? sink, int totalProviders) : IScanProgressReporter
    {
        private const long MinIntervalMs = 120;

        private long _files;
        private long _bytes;
        private int _items;
        private int _completed;
        private long _lastTick;

        public void OnItemFound(long bytes)
        {
            Interlocked.Increment(ref _items);
            Interlocked.Add(ref _bytes, bytes);
        }

        public void OnProviderCompleted(string provider)
        {
            Interlocked.Increment(ref _completed);
            Push(provider, force: true);
        }

        public void ReportFiles(int fileDelta, string currentPath)
        {
            Interlocked.Add(ref _files, fileDelta);
            Push(currentPath, force: false);
        }

        private void Push(string provider, bool force)
        {
            if (sink is null)
            {
                return;
            }

            if (!force)
            {
                var now = Environment.TickCount64;
                var last = Interlocked.Read(ref _lastTick);
                if (now - last < MinIntervalMs || Interlocked.CompareExchange(ref _lastTick, now, last) != last)
                {
                    return;
                }
            }
            else
            {
                Interlocked.Exchange(ref _lastTick, Environment.TickCount64);
            }

            sink.Report(new ScanProgress
            {
                CurrentProvider = provider,
                CompletedProviders = Volatile.Read(ref _completed),
                TotalProviders = totalProviders,
                ItemsFound = Volatile.Read(ref _items),
                BytesFound = Interlocked.Read(ref _bytes),
                FilesScanned = Interlocked.Read(ref _files),
            });
        }
    }
}
