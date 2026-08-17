using TraceZero.Domain;
using TraceZero.Domain.Protection;
using TraceZero.Persistence;

namespace TraceZero.IntegrationTests;

public sealed class SqliteProtectionVaultTests : IDisposable
{
    private readonly string _dbPath;

    public SqliteProtectionVaultTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), "tz-vault-" + Guid.NewGuid().ToString("N"), "v.db");
    }

    private static RestoreRecord Record(string description) => new()
    {
        TimestampUtc = DateTimeOffset.UtcNow,
        Description = description,
        Source = "Confidentialité",
        Kind = RestoreItemKind.RegistryBackup,
        Reversibility = Reversibility.Reversible,
        Target = @"Software\Example",
        Payload = "{\"Name\":\"\",\"Values\":[],\"SubKeys\":[]}",
    };

    [Fact]
    public async Task Adds_and_lists_restorable_items_most_recent_first()
    {
        var vault = new SqliteProtectionVault(_dbPath);
        await vault.AddAsync(Record("Documents récents"));
        await vault.AddAsync(Record("Historique d'exécution"));

        var items = await vault.GetRestorableAsync(10);
        Assert.Equal(2, items.Count);
        Assert.Equal("Historique d'exécution", items[0].Description);
    }

    [Fact]
    public async Task Marking_restored_removes_it_from_restorable_list()
    {
        var vault = new SqliteProtectionVault(_dbPath);
        var id = await vault.AddAsync(Record("Documents récents"));

        await vault.MarkRestoredAsync(id);

        Assert.Empty(await vault.GetRestorableAsync(10));

        // L'élément existe toujours mais est marqué restauré.
        var fetched = await vault.GetAsync(id);
        Assert.NotNull(fetched);
        Assert.True(fetched!.IsRestored);
    }

    [Fact]
    public async Task Clear_empties_the_vault()
    {
        var vault = new SqliteProtectionVault(_dbPath);
        await vault.AddAsync(Record("A"));
        await vault.ClearAsync();
        Assert.Empty(await vault.GetRestorableAsync(10));
    }

    [Fact]
    public async Task Persists_payload_and_reversibility_across_instances()
    {
        var first = new SqliteProtectionVault(_dbPath);
        var id = await first.AddAsync(Record("Documents récents"));

        var second = new SqliteProtectionVault(_dbPath);
        var fetched = await second.GetAsync(id);
        Assert.NotNull(fetched);
        Assert.Equal(Reversibility.Reversible, fetched!.Reversibility);
        Assert.Equal(@"Software\Example", fetched.Target);
        Assert.Contains("SubKeys", fetched.Payload);
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
