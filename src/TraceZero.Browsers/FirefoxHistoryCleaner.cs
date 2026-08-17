using Microsoft.Data.Sqlite;
using TraceZero.Application.Browsers;

namespace TraceZero.Browsers;

/// <summary>
/// Efface l'historique de navigation Firefox dans <c>places.sqlite</c> en préservant les favoris.
///
/// Approche sûre :
/// <list type="bullet">
///   <item>tout se fait dans une transaction ;</item>
///   <item>seules les entrées d'historique « pures » sont supprimées : <c>moz_places</c> dont
///     <c>foreign_count = 0</c> (les lignes référencées par un favori ou un mot-clé ont
///     <c>foreign_count &gt; 0</c> et sont conservées) ;</item>
///   <item>avant de valider, on vérifie qu'aucun favori ne pointe vers une ligne supprimée
///     (aucun <c>moz_bookmarks.fk</c> orphelin) — sinon <c>ROLLBACK</c> et aucun octet libéré ;</item>
///   <item>si la base est verrouillée (Firefox ouvert), l'opération échoue proprement et ne touche rien.</item>
/// </list>
/// Aucune valeur inventée : renvoie la réduction réelle de taille du fichier après <c>VACUUM</c>.
/// </summary>
public sealed class FirefoxHistoryCleaner : IBrowserHistoryCleaner
{
    public long ClearFirefoxHistory(string placesDbPath)
    {
        if (!File.Exists(placesDbPath))
        {
            return 0;
        }

        long sizeBefore;
        try
        {
            sizeBefore = new FileInfo(placesDbPath).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return 0;
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = placesDbPath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
        }.ToString();

        try
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                if (!TableExists(connection, "moz_places") || !TableExists(connection, "moz_historyvisits"))
                {
                    // Schéma inattendu : ne rien tenter.
                    return 0;
                }

                using var transaction = connection.BeginTransaction();

                Execute(connection, transaction, "DELETE FROM moz_historyvisits;");
                if (TableExists(connection, "moz_inputhistory"))
                {
                    Execute(connection, transaction, "DELETE FROM moz_inputhistory;");
                }

                // Cœur : supprimer les URL d'historique non référencées par un favori/mot-clé.
                Execute(connection, transaction, "DELETE FROM moz_places WHERE foreign_count = 0;");

                if (TableExists(connection, "moz_annos"))
                {
                    Execute(connection, transaction, "DELETE FROM moz_annos WHERE place_id NOT IN (SELECT id FROM moz_places);");
                }

                // Garde-fou : aucun favori ne doit pointer vers une ligne désormais absente.
                var danglingBookmarks = TableExists(connection, "moz_bookmarks")
                    ? ScalarLong(connection, transaction,
                        "SELECT COUNT(*) FROM moz_bookmarks WHERE fk IS NOT NULL AND fk NOT IN (SELECT id FROM moz_places);")
                    : 0;

                if (danglingBookmarks > 0)
                {
                    transaction.Rollback();
                    return 0;
                }

                transaction.Commit();

                // Compacter hors transaction pour matérialiser l'espace libéré.
                Execute(connection, null, "VACUUM;");
            }

            // Forcer la fermeture des handles poolés avant de mesurer.
            SqliteConnection.ClearAllPools();

            var sizeAfter = new FileInfo(placesDbPath).Length;
            return Math.Max(0, sizeBefore - sizeAfter);
        }
        catch (Exception ex) when (ex is SqliteException or IOException or UnauthorizedAccessException)
        {
            // Base verrouillée (navigateur ouvert) ou inaccessible : rien n'a été modifié de façon durable.
            return 0;
        }
    }

    private static bool TableExists(SqliteConnection connection, string table)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1;";
        command.Parameters.AddWithValue("$name", table);
        return command.ExecuteScalar() is not null;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction? transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static long ScalarLong(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        var result = command.ExecuteScalar();
        return result is null or DBNull ? 0 : Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }
}
