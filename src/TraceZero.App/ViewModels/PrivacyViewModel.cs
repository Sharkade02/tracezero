using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Cleaning;
using TraceZero.Application.History;
using TraceZero.Application.Privacy;
using TraceZero.Application.Protection;
using TraceZero.Domain;
using TraceZero.Domain.Common;
using TraceZero.Domain.History;
using TraceZero.Domain.Protection;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'une trace d'activité Windows, expliquée (§15).</summary>
public partial class PrivacyTraceRowViewModel : ObservableObject
{
    public PrivacyTraceRowViewModel(PrivacyTraceResult result)
    {
        Result = result;
    }

    public PrivacyTraceResult Result { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => Result.Definition.NameKey is { } k ? Localizer.Get(k) : Result.Definition.DisplayName;

    public string Explanation => Result.Definition.ExplanationKey is { } k ? Localizer.Get(k) : Result.Definition.Explanation;

    public string Why => Result.Definition.WhyKey is { } k ? Localizer.Get(k) : Result.Definition.Why;

    public bool IsPresent => Result.IsPresent;

    public bool CanSelect => Result.IsPresent;

    public string StatusText
    {
        get
        {
            if (!Result.IsPresent)
            {
                return Localizer.Get("Privacy.NoTrace");
            }

            return Result.SizeBytes > 0
                ? Localizer.Format("Privacy.ElementsSize", Result.EntryCount, ByteSize.Format(Result.SizeBytes))
                : Localizer.Format("Privacy.TraceCount", Result.EntryCount);
        }
    }
}

/// <summary>
/// Page « Ce que Windows sait encore de votre activité » (§15). Inspecte les traces réelles,
/// explique chacune, et permet un nettoyage sûr (registre allowlisté / fichiers validés).
/// </summary>
public sealed partial class PrivacyViewModel : PageViewModelBase, IDisposable
{
    private readonly IPrivacyInspector _inspector;
    private readonly ICleaningEngine _cleaningEngine;
    private readonly ICleanupHistoryStore _historyStore;
    private readonly IRegistryBackupService _registryBackup;
    private readonly IProtectionVault _vault;
    private CancellationTokenSource? _cts;

    public PrivacyViewModel(
        IPrivacyInspector inspector,
        ICleaningEngine cleaningEngine,
        ICleanupHistoryStore historyStore,
        IRegistryBackupService registryBackup,
        IProtectionVault vault)
    {
        _inspector = inspector;
        _cleaningEngine = cleaningEngine;
        _historyStore = historyStore;
        _registryBackup = registryBackup;
        _vault = vault;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Privacy");

    public override string IconGlyph => "\U0001F512";

    public override bool IsUnderConstruction => false;

    public ObservableCollection<PrivacyTraceRowViewModel> Traces { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(InspectCommand))]
    [NotifyCanExecuteChangedFor(nameof(CleanCommand))]
    private bool _isInspecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(InspectCommand))]
    [NotifyCanExecuteChangedFor(nameof(CleanCommand))]
    private bool _isCleaning;

    [ObservableProperty]
    private bool _hasInspected;

    [ObservableProperty]
    private string _statusMessage = "Analysez les traces que Windows conserve sur votre activité.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    private string? _resultMessage;

    public bool IsBusy => IsInspecting || IsCleaning;

    public bool HasTraces => Traces.Count > 0;

    public bool HasResult => !string.IsNullOrEmpty(ResultMessage);

    public int SelectedCount => Traces.Count(t => t.IsSelected);

    private bool CanInspect() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanInspect))]
    private async Task InspectAsync()
    {
        IsInspecting = true;
        ResultMessage = null;
        DetachRows();
        Traces.Clear();

        try
        {
            var results = await Task.Run(() => _inspector.Inspect());
            foreach (var result in results)
            {
                var row = new PrivacyTraceRowViewModel(result);
                row.PropertyChanged += OnRowChanged;
                Traces.Add(row);
            }

            HasInspected = true;
            var present = results.Count(r => r.IsPresent);
            StatusMessage = present == 0
                ? "Aucune trace d'activité trouvée."
                : $"{present} type(s) de trace détecté(s). Sélectionnez ce que vous voulez effacer.";
        }
        finally
        {
            IsInspecting = false;
            RaiseSummary();
        }
    }

    private bool CanClean() => !IsBusy && SelectedCount > 0;

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var selectedRows = Traces.Where(t => t.IsSelected).ToList();
        var selected = selectedRows.Select(t => t.Result.CleanTarget).ToList();
        var plan = _cleaningEngine.BuildPlan(selected);

        IsCleaning = true;
        try
        {
            // Protection du nettoyage (§17) : sauvegarder les traces registre AVANT de les effacer,
            // pour pouvoir les restaurer. Rien n'est transmis — la sauvegarde reste locale.
            var backups = await Task.Run(() => CaptureRegistryBackups(selectedRows));

            var result = await _cleaningEngine.CleanAsync(plan, progress: null, _cts.Token);

            try
            {
                await _historyStore.AddAsync(new CleanupHistoryEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    AppVersion = AppInfo.Version,
                    Source = "Confidentialité",
                    FreedBytes = result.BytesFreed,
                    ItemsCleaned = result.ActionsSucceeded,
                    Failures = result.Failures.Count,
                    DurationMs = (long)result.Elapsed.TotalMilliseconds,
                });
            }
            catch (Exception)
            {
                // L'historique ne doit jamais faire échouer un nettoyage réussi.
            }

            await PersistBackupsAsync(backups);

            ResultMessage = result.HasFailures
                ? $"{result.ActionsSucceeded} type(s) de trace nettoyé(s). {result.Failures.Count} élément(s) verrouillé(s) ignoré(s)."
                : $"{result.ActionsSucceeded} type(s) de trace nettoyé(s).";
            StatusMessage = "Nettoyage terminé. Relancez l'analyse pour voir l'état actuel.";
            DetachRows();
            Traces.Clear();
            HasInspected = false;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Nettoyage annulé.";
        }
        finally
        {
            IsCleaning = false;
            RaiseSummary();
        }
    }

    /// <summary>Capture (hors UI) une sauvegarde de chaque trace registre sélectionnée, avant nettoyage.</summary>
    private List<(PrivacyTraceRowViewModel Row, RegistryKeySnapshot Snapshot)> CaptureRegistryBackups(
        IReadOnlyList<PrivacyTraceRowViewModel> rows)
    {
        var backups = new List<(PrivacyTraceRowViewModel, RegistryKeySnapshot)>();

        foreach (var row in rows)
        {
            var definition = row.Result.Definition;
            if (definition.Kind != PrivacyTraceKind.Registry || string.IsNullOrEmpty(definition.RegistrySubKey))
            {
                continue;
            }

            var snapshot = _registryBackup.Capture(definition.RegistrySubKey);
            if (snapshot is { IsEmpty: false })
            {
                backups.Add((row, snapshot));
            }
        }

        return backups;
    }

    /// <summary>Persiste les sauvegardes capturées dans le coffre de restauration (jamais bloquant pour le nettoyage).</summary>
    private async Task PersistBackupsAsync(
        IReadOnlyList<(PrivacyTraceRowViewModel Row, RegistryKeySnapshot Snapshot)> backups)
    {
        foreach (var (row, snapshot) in backups)
        {
            var definition = row.Result.Definition;
            try
            {
                await _vault.AddAsync(new RestoreRecord
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Description = definition.DisplayName,
                    Source = "Confidentialité",
                    Kind = RestoreItemKind.RegistryBackup,
                    Reversibility = Reversibility.Reversible,
                    Target = definition.RegistrySubKey!,
                    Payload = RegistrySnapshotCodec.Serialize(snapshot),
                });
            }
            catch (Exception)
            {
                // La sauvegarde de restauration ne doit jamais faire échouer un nettoyage réussi.
            }
        }
    }

    private void OnRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrivacyTraceRowViewModel.IsSelected))
        {
            RaiseSummary();
        }
    }

    private void DetachRows()
    {
        foreach (var row in Traces)
        {
            row.PropertyChanged -= OnRowChanged;
        }
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(HasTraces));
        OnPropertyChanged(nameof(SelectedCount));
        CleanCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        DetachRows();
        GC.SuppressFinalize(this);
    }
}
