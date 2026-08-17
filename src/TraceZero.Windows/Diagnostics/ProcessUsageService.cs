using System.Diagnostics;
using TraceZero.Application.Diagnostics;
using TraceZero.Domain.Diagnostics;

namespace TraceZero.Windows.Diagnostics;

/// <summary>
/// Programmes qui consomment le plus de mémoire, en lecture seule. Énumère les process via
/// <see cref="Process.GetProcesses()"/>, agrège par nom (un logiciel peut avoir plusieurs process,
/// ex. un navigateur) et trie par working set décroissant. Aucune élévation : les process dont l'accès
/// est refusé (protégés/système) sont simplement ignorés — donnée honnête, jamais forcée.
/// </summary>
public sealed class ProcessUsageService : IProcessUsageService
{
    public IReadOnlyList<ProcessUsage> GetTopByMemory(int count = 8)
    {
        var take = Math.Clamp(count, 1, 50);
        var totals = new Dictionary<string, (long Bytes, int Count)>(StringComparer.OrdinalIgnoreCase);

        Process[] processes;
        try
        {
            processes = Process.GetProcesses();
        }
        catch (InvalidOperationException)
        {
            return [];
        }

        foreach (var process in processes)
        {
            try
            {
                var name = process.ProcessName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var ws = process.WorkingSet64;
                var entry = totals.TryGetValue(name, out var existing) ? existing : (Bytes: 0L, Count: 0);
                totals[name] = (entry.Bytes + ws, entry.Count + 1);
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
                // Process disparu ou inaccessible (protégé/système) : ignoré honnêtement.
            }
            finally
            {
                process.Dispose();
            }
        }

        return totals
            .Select(kv => new ProcessUsage
            {
                Name = kv.Key,
                ProcessCount = kv.Value.Count,
                WorkingSetBytes = kv.Value.Bytes,
            })
            .OrderByDescending(p => p.WorkingSetBytes)
            .Take(take)
            .ToList();
    }
}
