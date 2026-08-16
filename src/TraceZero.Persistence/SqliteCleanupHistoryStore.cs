using Microsoft.Data.Sqlite;
using TraceZero.Application.History;
using TraceZero.Domain.History;

namespace TraceZero.Persistence;

/// <summary>
/// Journal local des nettoyages sur SQLite (§16). Ne stocke qu'un résumé — jamais de chemin
/// personnel ni de contenu de fichier (§39).
/// </summary>
public sealed class SqliteCleanupHistoryStore : ICleanupHistoryStore
{
    private readonly string _connectionString;

    public SqliteCleanupHistoryStore(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
        }.ToString();

        EnsureSchema();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS cleanup_history (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp    TEXT    NOT NULL,
                app_version  TEXT    NOT NULL,
                source       TEXT    NOT NULL,
                freed_bytes  INTEGER NOT NULL,
                items        INTEGER NOT NULL,
                failures     INTEGER NOT NULL,
                duration_ms  INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task AddAsync(CleanupHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO cleanup_history (timestamp, app_version, source, freed_bytes, items, failures, duration_ms)
            VALUES ($timestamp, $version, $source, $freed, $items, $failures, $duration);
            """;
        command.Parameters.AddWithValue("$timestamp", entry.TimestampUtc.ToString("O"));
        command.Parameters.AddWithValue("$version", entry.AppVersion);
        command.Parameters.AddWithValue("$source", entry.Source);
        command.Parameters.AddWithValue("$freed", entry.FreedBytes);
        command.Parameters.AddWithValue("$items", entry.ItemsCleaned);
        command.Parameters.AddWithValue("$failures", entry.Failures);
        command.Parameters.AddWithValue("$duration", entry.DurationMs);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CleanupHistoryEntry>> GetRecentAsync(int max, CancellationToken cancellationToken = default)
    {
        var entries = new List<CleanupHistoryEntry>();

        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, timestamp, app_version, source, freed_bytes, items, failures, duration_ms
            FROM cleanup_history ORDER BY id DESC LIMIT $max;
            """;
        command.Parameters.AddWithValue("$max", max);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new CleanupHistoryEntry
            {
                Id = reader.GetInt64(0),
                TimestampUtc = DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
                AppVersion = reader.GetString(2),
                Source = reader.GetString(3),
                FreedBytes = reader.GetInt64(4),
                ItemsCleaned = reader.GetInt32(5),
                Failures = reader.GetInt32(6),
                DurationMs = reader.GetInt64(7),
            });
        }

        return entries;
    }

    public async Task<CleanupHistoryStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COALESCE(SUM(freed_bytes), 0), COUNT(*), MAX(timestamp) FROM cleanup_history;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return CleanupHistoryStats.Empty;
        }

        var total = reader.GetInt64(0);
        var count = reader.GetInt32(1);
        DateTimeOffset? last = reader.IsDBNull(2)
            ? null
            : DateTimeOffset.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind);

        return new CleanupHistoryStats(total, count, last);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM cleanup_history;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
