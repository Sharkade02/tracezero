using System.Text.Json;
using TraceZero.Domain.Elevation;
using TraceZero.Engine.Elevation;

namespace TraceZero.Elevated;

/// <summary>
/// Helper élevé de TraceZero (Phase 20, §30) — surface minimale.
///
/// Invocation attendue :
/// <code>TraceZero.Elevated.exe --request &lt;fichier.json&gt; --response &lt;fichier.json&gt;</code>
///
/// Il n'accepte qu'une commande structurée (<see cref="ElevatedRequest"/>), applique sa propre
/// validation de sécurité (via <see cref="ElevatedOperationExecutor"/> / <c>ElevatedSafePathValidator</c>),
/// refuse tout chemin arbitraire, journalise, écrit une réponse structurée, puis s'arrête.
/// Il ne fait jamais confiance au client UI.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        var requestPath = GetArg(args, "--request");
        var responsePath = GetArg(args, "--response");

        // Sans fichier de réponse, on ne peut rien renvoyer : arrêt immédiat, aucun effet de bord.
        if (string.IsNullOrWhiteSpace(responsePath))
        {
            Log("Argument --response manquant : arrêt sans action.");
            return 2;
        }

        ElevatedResult result;
        try
        {
            if (string.IsNullOrWhiteSpace(requestPath) || !File.Exists(requestPath))
            {
                result = ElevatedResult.Fail("Fichier de requête manquant.");
            }
            else
            {
                var json = File.ReadAllText(requestPath);
                var request = JsonSerializer.Deserialize(json, ElevatedJsonContext.Default.ElevatedRequest);

                result = request is null
                    ? ElevatedResult.Fail("Requête d'élévation illisible.")
                    : new ElevatedOperationExecutor().Execute(request);

                Log($"Opération {request?.Operation.ToString() ?? "?"} → " +
                    $"succès={result.Success}, libéré={result.BytesFreed} o, " +
                    $"ok={result.ActionsSucceeded}, échecs={result.ActionsFailed}" +
                    (result.ErrorMessage is { } e ? $", erreur={e}" : string.Empty));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            result = ElevatedResult.Fail("Erreur lors du traitement de la requête d'élévation.");
            Log($"Exception : {ex.GetType().Name} — {ex.Message}");
        }

        TryWriteResponse(responsePath, result);
        return result.Success ? 0 : 1;
    }

    private static string? GetArg(string[] args, string name)
    {
        var index = Array.FindIndex(args, a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void TryWriteResponse(string path, ElevatedResult result)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, JsonSerializer.Serialize(result, ElevatedJsonContext.Default.ElevatedResult));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log($"Impossible d'écrire la réponse : {ex.Message}");
        }
    }

    /// <summary>Journal minimal sous <c>%ProgramData%\TraceZero\logs</c> (accessible en écriture élevée).</summary>
    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "TraceZero", "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ssZ}\t{message}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(dir, "elevated.log"), line);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Le journal est best-effort ; ne jamais faire échouer l'opération à cause du log.
        }
    }
}
