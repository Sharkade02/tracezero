using Microsoft.Data.Sqlite;
using TraceZero.Application.Protection;
using TraceZero.Domain;
using TraceZero.Domain.Protection;

namespace TraceZero.Persistence;

/// <summary>
/// Coffre local des éléments restaurables sur SQLite (§17). Conserve les sauvegardes créées avant les
/// nettoyages réversibles. Données locales uniquement, jamais transmises (§39).
/// </summary>
public sealed class SqliteProtectionVault : IProtectionVault
{
    private readonly string _connectionString;

    public SqliteProtectionVault(string databasePath)
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
            CREATE TABLE IF NOT EXISTS restore_points (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                timestamp     TEXT    NOT NULL,
                description   TEXT    NOT NULL,
                source        TEXT    NOT NULL,
                kind          INTEGER NOT NULL,
                reversibility INTEGER NOT NULL,
                target        TEXT    NOT NULL,
                payload       TEXT    NOT NULL,
                restored      INTEGER NOT NULL DEFAULT 0
            );
            """;
        command.ExecuteNonQuery();
    }

    public async Task<long> AddAsync(RestoreRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO restore_points (timestamp, description, source, kind, reversibility, target, payload, restored)
            VALUES ($timestamp, $description, $source, $kind, $reversibility, $target, $payload, $restored);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$timestamp", record.TimestampUtc.ToString("O"));
        command.Parameters.AddWithValue("$description", record.Description);
        command.Parameters.AddWithValue("$source", record.Source);
        command.Parameters.AddWithValue("$kind", (int)record.Kind);
        command.Parameters.AddWithValue("$reversibility", (int)record.Reversibility);
        command.Parameters.AddWithValue("$target", record.Target);
        command.Parameters.AddWithValue("$payload", record.Payload);
        command.Parameters.AddWithValue("$restored", record.IsRestored ? 1 : 0);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
        return id;
    }

    public async Task<IReadOnlyList<RestoreRecord>> GetRestorableAsync(int max, CancellationToken cancellationToken = default)
    {
        var records = new List<RestoreRecord>();

        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, timestamp, description, source, kind, reversibility, target, payload, restored
            FROM restore_points WHERE restored = 0 ORDER BY id DESC LIMIT $max;
            """;
        command.Parameters.AddWithValue("$max", max);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(Read(reader));
        }

        return records;
    }

    public async Task<RestoreRecord?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, timestamp, description, source, kind, reversibility, target, payload, restored
            FROM restore_points WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task MarkRestoredAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE restore_points SET restored = 1 WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM restore_points;";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static RestoreRecord Read(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        TimestampUtc = DateTimeOffset.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind),
        Description = reader.GetString(2),
        Source = reader.GetString(3),
        Kind = (RestoreItemKind)reader.GetInt32(4),
        Reversibility = (Reversibility)reader.GetInt32(5),
        Target = reader.GetString(6),
        Payload = reader.GetString(7),
        IsRestored = reader.GetInt32(8) != 0,
    };
}
