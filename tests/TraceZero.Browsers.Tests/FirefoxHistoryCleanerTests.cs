using Microsoft.Data.Sqlite;

namespace TraceZero.Browsers.Tests;

public sealed class FirefoxHistoryCleanerTests : IDisposable
{
    private readonly string _dir;
    private readonly string _places;

    public FirefoxHistoryCleanerTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tz-ffhist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _places = Path.Combine(_dir, "places.sqlite");
    }

    private void CreatePlacesDb()
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _places, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
        using var c = new SqliteConnection(cs);
        c.Open();
        Exec(c, "CREATE TABLE moz_places (id INTEGER PRIMARY KEY, url TEXT, foreign_count INTEGER DEFAULT 0);");
        Exec(c, "CREATE TABLE moz_historyvisits (id INTEGER PRIMARY KEY, place_id INTEGER);");
        Exec(c, "CREATE TABLE moz_inputhistory (place_id INTEGER, input TEXT);");
        Exec(c, "CREATE TABLE moz_bookmarks (id INTEGER PRIMARY KEY, fk INTEGER);");

        // Place 1 : historique pur (aucun favori) → foreign_count = 0.
        Exec(c, "INSERT INTO moz_places (id, url, foreign_count) VALUES (1, 'http://history.example/', 0);");
        Exec(c, "INSERT INTO moz_historyvisits (id, place_id) VALUES (1, 1);");
        Exec(c, "INSERT INTO moz_inputhistory (place_id, input) VALUES (1, 'his');");

        // Place 2 : marqué en favori → foreign_count = 1, référencé par moz_bookmarks.
        Exec(c, "INSERT INTO moz_places (id, url, foreign_count) VALUES (2, 'http://bookmarked.example/', 1);");
        Exec(c, "INSERT INTO moz_historyvisits (id, place_id) VALUES (2, 2);");
        Exec(c, "INSERT INTO moz_bookmarks (id, fk) VALUES (10, 2);");
        SqliteConnection.ClearAllPools();
    }

    private static void Exec(SqliteConnection c, string sql)
    {
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private long Scalar(string sql)
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _places, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString();
        using var c = new SqliteConnection(cs);
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        var r = cmd.ExecuteScalar();
        var value = r is null or DBNull ? 0 : Convert.ToInt64(r, System.Globalization.CultureInfo.InvariantCulture);
        SqliteConnection.ClearAllPools();
        return value;
    }

    [Fact]
    public void Clears_history_but_keeps_bookmarks()
    {
        CreatePlacesDb();

        var freed = new FirefoxHistoryCleaner().ClearFirefoxHistory(_places);

        Assert.True(freed >= 0);
        // Historique effacé.
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM moz_historyvisits;"));
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM moz_inputhistory;"));
        // La page d'historique pure a disparu ; la page en favori est conservée.
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM moz_places WHERE url = 'http://history.example/';"));
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM moz_places WHERE url = 'http://bookmarked.example/';"));
        // Le favori reste intègre : aucun fk orphelin.
        Assert.Equal(0, Scalar("SELECT COUNT(*) FROM moz_bookmarks WHERE fk NOT IN (SELECT id FROM moz_places);"));
    }

    [Fact]
    public void Missing_file_is_a_safe_no_op()
    {
        var freed = new FirefoxHistoryCleaner().ClearFirefoxHistory(Path.Combine(_dir, "does-not-exist.sqlite"));
        Assert.Equal(0, freed);
    }

    [Fact]
    public void Unexpected_schema_is_left_untouched()
    {
        var cs = new SqliteConnectionStringBuilder { DataSource = _places, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString();
        using (var c = new SqliteConnection(cs))
        {
            c.Open();
            Exec(c, "CREATE TABLE something_else (id INTEGER PRIMARY KEY);");
            Exec(c, "INSERT INTO something_else (id) VALUES (1);");
        }
        SqliteConnection.ClearAllPools();

        var freed = new FirefoxHistoryCleaner().ClearFirefoxHistory(_places);

        Assert.Equal(0, freed);
        Assert.Equal(1, Scalar("SELECT COUNT(*) FROM something_else;"));
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
