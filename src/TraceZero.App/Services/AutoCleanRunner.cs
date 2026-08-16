using TraceZero.Application.Automation;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Exclusions;
using TraceZero.Application.History;
using TraceZero.Application.Scanning;
using TraceZero.Domain.Automation;
using TraceZero.Domain.History;

namespace TraceZero.App.Services;

/// <summary>
/// Exécute un nettoyage sans interface (mode « --autoclean »), utilisé par la tâche planifiée (§15).
/// Ne sélectionne que les éléments autorisés par le profil et respecte les exclusions.
/// </summary>
public sealed class AutoCleanRunner(
    IScanEngine scanEngine,
    ICleaningEngine cleaningEngine,
    IExclusionStore exclusionStore,
    ICleanupHistoryStore historyStore)
{
    public async Task RunAsync(CleaningProfile profile, CancellationToken cancellationToken = default)
    {
        var report = await scanEngine.ScanAsync(progress: null, cancellationToken);

        var selected = report.Items
            .Where(i => CleaningProfiles.Includes(profile, i.Risk))
            .Where(i => !exclusionStore.IsExcluded(i))
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        var plan = cleaningEngine.BuildPlan(selected);
        var result = await cleaningEngine.CleanAsync(plan, progress: null, cancellationToken);

        try
        {
            await historyStore.AddAsync(new CleanupHistoryEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                AppVersion = AppInfo.Version,
                Source = "Automatisation",
                FreedBytes = result.BytesFreed,
                ItemsCleaned = result.ActionsSucceeded,
                Failures = result.Failures.Count,
                DurationMs = (long)result.Elapsed.TotalMilliseconds,
            }, cancellationToken);
        }
        catch (Exception)
        {
            // L'historique ne doit jamais faire échouer le nettoyage.
        }
    }
}
