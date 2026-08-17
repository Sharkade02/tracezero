using Microsoft.Data.Sqlite;
using TraceZero.Domain;
using TraceZero.Engine.Cleaning;
using TraceZero.Engine.Safety;

namespace TraceZero.Browsers.Tests;

/// <summary>
/// Vérifie la chaîne complète : plan de nettoyage → barrière <see cref="SafePathValidator"/> →
/// dispatch de l'action <see cref="FileActionKind.ClearBrowserHistory"/> vers le nettoyeur Firefox réel.
/// </summary>
public sealed class CleaningEngineHistoryTests : IDisposable
{
    private readonly string _dir;
    private readonly string _places;

    public CleaningEngineHistoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tz-cehist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _places = Path.Combine(_dir, "places.sqlite");
        CreatePlacesDb();
    }

    private void CreatePlacesDb()
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _places, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
        using var c = new SqliteConnection(cs);
        c.Open();
        foreach (var sql in new[]
        {
            "CREATE TABLE moz_places (id INTEGER PRIMARY KEY, url TEXT, foreign_count INTEGER DEFAULT 0);",
            "CREATE TABLE moz_historyvisits (id INTEGER PRIMARY KEY, place_id INTEGER);",
            "CREATE TABLE moz_bookmarks (id INTEGER PRIMARY KEY, fk INTEGER);",
            "INSERT INTO moz_places (id, url, foreign_count) VALUES (1, 'http://history.example/', 0);",
            "INSERT INTO moz_historyvisits (id, place_id) VALUES (1, 1);",
            "INSERT INTO moz_places (id, url, foreign_count) VALUES (2, 'http://bookmarked.example/', 1);",
            "INSERT INTO moz_bookmarks (id, fk) VALUES (10, 2);",
        })
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        SqliteConnection.ClearAllPools();
    }

    private long Count(string sql)
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _places, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        using var c = new SqliteConnection(cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        var value = Convert.ToInt64(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
        SqliteConnection.ClearAllPools();
        return value;
    }

    [Fact]
    public async Task Engine_dispatches_clear_history_and_preserves_bookmarks()
    {
        var engine = new CleaningEngine(
            new SafePathValidator(new WindowsKnownFolders()),
            recycleBin: null,
            registryCleaner: null,
            historyCleaner: new FirefoxHistoryCleaner());

        var item = new ScanItem
        {
            Id = "browsers.privacy::history::firefox",
            RuleId = "browsers.privacy",
            Category = Category.BrowserHistory,
            DisplayName = "Firefox — historique",
            PathOrIdentifier = _places,
            Risk = RiskLevel.Privacy,
            ActionKind = FileActionKind.ClearBrowserHistory,
            AllowedRoots = [_dir],
        };

        var plan = engine.BuildPlan([item]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.Equal(1, result.ActionsSucceeded);
        Assert.False(result.HasFailures);
        Assert.Equal(0, Count("SELECT COUNT(*) FROM moz_historyvisits;"));
        Assert.Equal(0, Count("SELECT COUNT(*) FROM moz_places WHERE url = 'http://history.example/';"));
        Assert.Equal(1, Count("SELECT COUNT(*) FROM moz_places WHERE url = 'http://bookmarked.example/';"));
    }

    [Fact]
    public async Task Engine_refuses_clear_history_outside_allowed_root()
    {
        var engine = new CleaningEngine(
            new SafePathValidator(new WindowsKnownFolders()),
            recycleBin: null,
            registryCleaner: null,
            historyCleaner: new FirefoxHistoryCleaner());

        // Racine autorisée qui ne contient PAS le fichier : la barrière de sécurité doit refuser.
        var item = new ScanItem
        {
            Id = "browsers.privacy::history::firefox",
            RuleId = "browsers.privacy",
            Category = Category.BrowserHistory,
            DisplayName = "Firefox — historique",
            PathOrIdentifier = _places,
            Risk = RiskLevel.Privacy,
            ActionKind = FileActionKind.ClearBrowserHistory,
            AllowedRoots = [Path.Combine(_dir, "somewhere-else")],
        };

        var plan = engine.BuildPlan([item]);
        var result = await engine.CleanAsync(plan, progress: null, CancellationToken.None);

        Assert.True(result.HasFailures);
        // La base est intacte : l'historique n'a pas été touché.
        Assert.Equal(1, Count("SELECT COUNT(*) FROM moz_historyvisits;"));
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
