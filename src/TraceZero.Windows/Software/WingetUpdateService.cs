using System.Diagnostics;
using System.Text.RegularExpressions;
using TraceZero.Application.Software;
using TraceZero.Domain.Software;

namespace TraceZero.Windows.Software;

/// <summary>
/// Détecte les logiciels obsolètes via le Windows Package Manager (winget), source officielle et signée
/// (§23). Ne télécharge ni n'installe rien en propre : la mise à jour est lancée par winget dans une
/// fenêtre visible. Si winget est absent, le rapport est marqué indisponible (jamais de scraping).
/// </summary>
public sealed partial class WingetUpdateService : ISoftwareUpdateService
{
    public async Task<SoftwareUpdateReport> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
    {
        string output;
        try
        {
            output = await RunAsync("upgrade --include-unknown --disable-interactivity", cancellationToken);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // winget introuvable : honnêtement indisponible.
            return SoftwareUpdateReport.Unavailable;
        }

        return new SoftwareUpdateReport { Updates = Parse(output), SourceAvailable = true };
    }

    public bool LaunchUpdate(string packageId) =>
        Launch($"upgrade --id \"{packageId}\" --include-unknown");

    public bool LaunchUpdateAll() =>
        Launch("upgrade --all --include-unknown");

    private static bool Launch(string arguments)
    {
        try
        {
            // Fenêtre visible : l'utilisateur voit et peut interrompre la mise à jour (§23).
            Process.Start(new ProcessStartInfo("winget", arguments) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<string> RunAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("winget", arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            },
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }

    /// <summary>
    /// Extrait les mises à jour de la sortie tabulaire de <c>winget upgrade</c>. Robuste à la locale :
    /// on repère la ligne de tirets puis on découpe chaque ligne de données sur les colonnes (2 espaces
    /// ou plus). Public et pur pour être testable sans winget.
    /// </summary>
    public static IReadOnlyList<SoftwareUpdate> Parse(string output)
    {
        var updates = new List<SoftwareUpdate>();
        if (string.IsNullOrWhiteSpace(output))
        {
            return updates;
        }

        var lines = output.Replace("\r", string.Empty).Split('\n');

        var separatorIndex = Array.FindIndex(lines, l => l.Trim().Length >= 3 && l.Trim().All(c => c == '-'));
        if (separatorIndex < 0)
        {
            return updates;
        }

        for (var i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                break; // fin du tableau
            }

            var columns = ColumnSplit().Split(line.Trim());
            if (columns.Length < 4 || columns.Any(string.IsNullOrWhiteSpace))
            {
                continue; // ligne de résumé / bruit
            }

            updates.Add(new SoftwareUpdate
            {
                Name = columns[0],
                Id = columns[1],
                InstalledVersion = columns[2],
                AvailableVersion = columns[3],
                Source = columns.Length >= 5 ? columns[4] : "winget",
            });
        }

        return updates;
    }

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex ColumnSplit();
}
