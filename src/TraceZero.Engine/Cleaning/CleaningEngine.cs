using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Privacy;
using TraceZero.Application.Safety;
using TraceZero.Domain;
using TraceZero.Domain.Cleaning;
using TraceZero.Engine.IO;

namespace TraceZero.Engine.Cleaning;

/// <summary>
/// Moteur de nettoyage (§6, §9). Chaque cible est revalidée par <see cref="ISafePathValidator"/>
/// juste avant suppression : le plan seul ne suffit pas à autoriser une suppression.
/// </summary>
public sealed class CleaningEngine : ICleaningEngine
{
    private readonly ISafePathValidator _validator;
    private readonly IRecycleBinService? _recycleBin;
    private readonly IRegistryTraceCleaner? _registryCleaner;
    private readonly ILogger<CleaningEngine>? _logger;

    public CleaningEngine(
        ISafePathValidator validator,
        IRecycleBinService? recycleBin = null,
        IRegistryTraceCleaner? registryCleaner = null,
        ILogger<CleaningEngine>? logger = null)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _recycleBin = recycleBin;
        _registryCleaner = registryCleaner;
        _logger = logger;
    }

    public CleaningPlan BuildPlan(IEnumerable<ScanItem> selectedItems)
    {
        var actions = selectedItems.Select(item => new CleaningAction
        {
            ItemId = item.Id,
            DisplayName = item.DisplayName,
            TargetPath = item.PathOrIdentifier,
            Kind = item.ActionKind,
            AllowedRoots = item.AllowedRoots,
            SweepRoots = item.SweepRoots,
            Risk = item.Risk,
            EstimatedBytes = item.SizeBytes,
            Reversibility = item.Reversibility,
            NeedsElevation = item.NeedsElevation,
        }).ToList();

        return new CleaningPlan { Actions = actions };
    }

    public async Task<CleaningResult> CleanAsync(
        CleaningPlan plan,
        IProgress<CleaningProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var stopwatch = Stopwatch.StartNew();
        var failures = new List<CleaningFailure>();
        long totalFreed = 0;
        var succeeded = 0;
        var processed = 0;

        foreach (var action in plan.Actions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var freed = await Task.Run(() => ExecuteAction(action, failures, cancellationToken), cancellationToken);
                totalFreed += freed;
                succeeded++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add(new CleaningFailure
                {
                    ItemId = action.ItemId,
                    Path = action.TargetPath,
                    Message = $"Impossible de nettoyer « {action.DisplayName} ».",
                    TechnicalDetail = ex.Message,
                });
                _logger?.LogWarning(ex, "Échec de l'action de nettoyage {ItemId}.", action.ItemId);
            }
            finally
            {
                processed++;
                progress?.Report(new CleaningProgress
                {
                    CurrentItem = action.DisplayName,
                    Processed = processed,
                    Total = plan.ActionCount,
                    BytesFreed = totalFreed,
                });
            }
        }

        stopwatch.Stop();

        return new CleaningResult
        {
            BytesFreed = totalFreed,
            ActionsSucceeded = succeeded,
            Failures = failures,
            Elapsed = stopwatch.Elapsed,
        };
    }

    private long ExecuteAction(CleaningAction action, List<CleaningFailure> failures, CancellationToken cancellationToken)
    {
        if (action.Kind == FileActionKind.EmptyRecycleBin)
        {
            return _recycleBin?.Empty() ?? 0;
        }

        if (action.Kind == FileActionKind.ClearRegistryKey)
        {
            // Le nettoyeur registre applique sa propre liste d'autorisation ; pas de taille libérée.
            _registryCleaner?.ClearKey(action.TargetPath);
            return 0;
        }

        // Barrière de sécurité : revalider la cible avant toute opération.
        var validation = _validator.Validate(action.TargetPath, action.AllowedRoots);
        if (!validation.IsAllowed)
        {
            failures.Add(new CleaningFailure
            {
                ItemId = action.ItemId,
                Path = action.TargetPath,
                Message = $"« {action.DisplayName} » a été refusé par la validation de sécurité.",
                TechnicalDetail = $"{validation.Reason}: {validation.Detail}",
            });
            return 0;
        }

        return action.Kind switch
        {
            FileActionKind.DeleteFile => DeleteSingleFile(action, failures),
            FileActionKind.DeleteDirectoryContents => SweepAll(action, failures, deleteRoot: false, cancellationToken),
            FileActionKind.DeleteDirectory => SweepAll(action, failures, deleteRoot: true, cancellationToken),
            _ => 0,
        };
    }

    /// <summary>Balaye une ou plusieurs racines (cas des caches de navigateur regroupés).</summary>
    private long SweepAll(CleaningAction action, List<CleaningFailure> failures, bool deleteRoot, CancellationToken cancellationToken)
    {
        var roots = action.SweepRoots.Count > 0 ? action.SweepRoots : [action.TargetPath];
        long freed = 0;
        foreach (var root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Chaque racine est revalidée individuellement.
            var validation = _validator.Validate(root, action.AllowedRoots);
            if (!validation.IsAllowed)
            {
                failures.Add(new CleaningFailure
                {
                    ItemId = action.ItemId,
                    Path = root,
                    Message = $"« {action.DisplayName} » : un dossier a été refusé par la sécurité.",
                    TechnicalDetail = $"{validation.Reason}: {validation.Detail}",
                });
                continue;
            }

            freed += SweepDirectory(root, action, failures, deleteRoot, cancellationToken);
        }

        return freed;
    }

    private static long DeleteSingleFile(CleaningAction action, List<CleaningFailure> failures)
    {
        try
        {
            var info = new FileInfo(action.TargetPath);
            if (!info.Exists)
            {
                return 0;
            }

            var length = info.Length;
            info.Delete();
            return length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(BuildFileFailure(action.ItemId, action.TargetPath, ex));
            return 0;
        }
    }

    private long SweepDirectory(string root, CleaningAction action, List<CleaningFailure> failures, bool deleteRoot, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            return 0;
        }

        long freed = 0;

        // 1. Supprimer les fichiers, chacun revalidé contre la racine autorisée.
        foreach (var file in SafeFileEnumerator.EnumerateEntries(root, recursive: true, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validation = _validator.Validate(file.FullPath, action.AllowedRoots);
            if (!validation.IsAllowed)
            {
                failures.Add(new CleaningFailure
                {
                    ItemId = action.ItemId,
                    Path = file.FullPath,
                    Message = "Un fichier a été ignoré par sécurité.",
                    TechnicalDetail = $"{validation.Reason}: {validation.Detail}",
                });
                continue;
            }

            try
            {
                File.Delete(file.FullPath);
                freed += file.Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Fichier verrouillé / inaccessible : ne jamais forcer (§9). On signale et on continue.
                failures.Add(BuildFileFailure(action.ItemId, file.FullPath, ex));
            }
        }

        // 2. Supprimer les sous-dossiers désormais vides, du plus profond au plus superficiel.
        DeleteEmptyDirectories(root, action, deleteRoot, failures, cancellationToken);

        return freed;
    }

    private void DeleteEmptyDirectories(
        string root,
        CleaningAction action,
        bool deleteRoot,
        List<CleaningFailure> failures,
        CancellationToken cancellationToken)
    {
        var directories = new List<string>();
        CollectDirectories(root, directories, cancellationToken);

        // Plus profond d'abord.
        foreach (var directory in directories.OrderByDescending(d => d.Length))
        {
            TryDeleteDirectory(directory, action, failures);
        }

        if (deleteRoot)
        {
            TryDeleteDirectory(root, action, failures);
        }
    }

    private static void CollectDirectories(string root, List<string> collected, CancellationToken cancellationToken)
    {
        foreach (var child in SafeFileEnumerator.EnumerateSafeChildDirectories(root))
        {
            cancellationToken.ThrowIfCancellationRequested();
            collected.Add(child);
            CollectDirectories(child, collected, cancellationToken);
        }
    }

    private void TryDeleteDirectory(string directory, CleaningAction action, List<CleaningFailure> failures)
    {
        var validation = _validator.Validate(directory, action.AllowedRoots);
        if (!validation.IsAllowed)
        {
            return;
        }

        try
        {
            // Suppression non récursive : ne réussit que si le dossier est réellement vide.
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory, recursive: false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            failures.Add(BuildFileFailure(action.ItemId, directory, ex));
        }
    }

    private static CleaningFailure BuildFileFailure(string itemId, string path, Exception ex) => new()
    {
        ItemId = itemId,
        Path = path,
        Message = "Élément verrouillé ou inaccessible, ignoré.",
        TechnicalDetail = ex.Message,
    };
}
