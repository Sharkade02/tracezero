using TraceZero.Domain.History;

namespace TraceZero.Application.History;

/// <summary>Journal local des nettoyages (§16). Local uniquement, aucune télémétrie (§39).</summary>
public interface ICleanupHistoryStore
{
    Task AddAsync(CleanupHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CleanupHistoryEntry>> GetRecentAsync(int max, CancellationToken cancellationToken = default);

    Task<CleanupHistoryStats> GetStatsAsync(CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
