using TraceZero.Domain.History;
using TraceZero.Persistence;

namespace TraceZero.IntegrationTests;

public sealed class SqliteCleanupHistoryStoreTests : IDisposable
{
    private readonly string _dbPath;

    public SqliteCleanupHistoryStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "tz-hist-" + Guid.NewGuid().ToString("N"), "h.db");
    }

    private static CleanupHistoryEntry Entry(long freed, int items, string source) => new()
    {
        TimestampUtc = DateTimeOffset.UtcNow,
        AppVersion = "0.1.0",
        Source = source,
        FreedBytes = freed,
        ItemsCleaned = items,
        Failures = 0,
        DurationMs = 42,
    };

    [Fact]
    public async Task Records_and_aggregates_history()
    {
        var store = new SqliteCleanupHistoryStore(_dbPath);
        await store.AddAsync(Entry(1000, 3, "Nettoyage"));
        await store.AddAsync(Entry(500, 2, "Confidentialité"));

        var stats = await store.GetStatsAsync();
        Assert.Equal(1500, stats.TotalFreedBytes);
        Assert.Equal(2, stats.CleanupCount);
        Assert.NotNull(stats.LastCleanupUtc);

        var recent = await store.GetRecentAsync(10);
        Assert.Equal(2, recent.Count);
        Assert.Equal("Confidentialité", recent[0].Source); // le plus récent d'abord
    }

    [Fact]
    public async Task Clear_empties_history()
    {
        var store = new SqliteCleanupHistoryStore(_dbPath);
        await store.AddAsync(Entry(100, 1, "Nettoyage"));
        await store.ClearAsync();

        var stats = await store.GetStatsAsync();
        Assert.Equal(0, stats.CleanupCount);
        Assert.Equal(0, stats.TotalFreedBytes);
    }

    [Fact]
    public async Task Persists_across_instances()
    {
        var first = new SqliteCleanupHistoryStore(_dbPath);
        await first.AddAsync(Entry(2048, 5, "Nettoyage"));

        var second = new SqliteCleanupHistoryStore(_dbPath);
        var stats = await second.GetStatsAsync();
        Assert.Equal(2048, stats.TotalFreedBytes);
    }

    public void Dispose()
    {
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            var dir = Path.GetDirectoryName(_dbPath);
            if (dir is not null && Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
