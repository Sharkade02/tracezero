using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TraceZero.Application.Elevation;
using TraceZero.Domain.Elevation;

namespace TraceZero.Windows.Elevation;

/// <summary>
/// Implémentation Windows de <see cref="IElevatedOperationService"/> (Phase 20, §30).
///
/// Lance à la demande <c>TraceZero.Elevated.exe</c> avec le verbe <c>runas</c> (invite UAC), transmet la
/// commande via un fichier de requête JSON, récupère le résultat via un fichier de réponse, puis le
/// helper s'arrête. Aucune opération élevée n'est réalisée dans le processus UI.
/// </summary>
public sealed class ElevatedOperationClient : IElevatedOperationService
{
    private const string HelperFileName = "TraceZero.Elevated.exe";
    private const int ErrorCancelled = 1223; // ERROR_CANCELLED — l'utilisateur a refusé l'élévation.

    private readonly ILogger<ElevatedOperationClient> _logger;

    public ElevatedOperationClient(ILogger<ElevatedOperationClient> logger) => _logger = logger;

    public bool IsCurrentProcessElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Security.SecurityException)
            {
                return false;
            }
        }
    }

    public async Task<ElevatedResult> RunAsync(ElevatedRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var helperPath = Path.Combine(AppContext.BaseDirectory, HelperFileName);
        if (!File.Exists(helperPath))
        {
            _logger.LogError("Helper élevé introuvable : {HelperPath}", helperPath);
            return ElevatedResult.Fail("Le composant d'élévation est introuvable.");
        }

        var workDir = Path.Combine(Path.GetTempPath(), "tz-elevated-" + Guid.NewGuid().ToString("N"));
        var requestFile = Path.Combine(workDir, "request.json");
        var responseFile = Path.Combine(workDir, "response.json");

        try
        {
            Directory.CreateDirectory(workDir);
            await File.WriteAllTextAsync(
                requestFile,
                JsonSerializer.Serialize(request, ElevatedJsonContext.Default.ElevatedRequest),
                cancellationToken).ConfigureAwait(false);

            var startInfo = new ProcessStartInfo(helperPath)
            {
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"--request \"{requestFile}\" --response \"{responseFile}\"",
            };

            Process? process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorCancelled)
            {
                _logger.LogInformation("Élévation refusée par l'utilisateur (UAC annulé).");
                return ElevatedResult.Fail("Élévation refusée : l'opération nécessite les droits administrateur.");
            }

            if (process is null)
            {
                return ElevatedResult.Fail("Impossible de démarrer le composant d'élévation.");
            }

            using (process)
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (!File.Exists(responseFile))
            {
                _logger.LogError("Le helper élevé n'a produit aucune réponse.");
                return ElevatedResult.Fail("Le composant d'élévation n'a renvoyé aucun résultat.");
            }

            var json = await File.ReadAllTextAsync(responseFile, cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize(json, ElevatedJsonContext.Default.ElevatedResult);
            return result ?? ElevatedResult.Fail("Réponse d'élévation illisible.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(ex, "Échec de communication avec le helper élevé.");
            return ElevatedResult.Fail("Erreur de communication avec le composant d'élévation.");
        }
        finally
        {
            TryCleanup(workDir);
        }
    }

    private void TryCleanup(string workDir)
    {
        try
        {
            if (Directory.Exists(workDir))
            {
                Directory.Delete(workDir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Nettoyage du dossier temporaire d'élévation impossible : {WorkDir}", workDir);
        }
    }
}
