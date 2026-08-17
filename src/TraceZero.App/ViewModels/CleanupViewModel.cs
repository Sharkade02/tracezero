using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Cleaning;
using TraceZero.Application.Exclusions;
using TraceZero.Application.History;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Cleaning;
using TraceZero.Domain.Common;
using TraceZero.Domain.History;
using TraceZero.Domain.Scanning;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Page Nettoyage : scan réel, prévisualisation de ce qui va se passer (§3.3) et nettoyage réel
/// via le moteur. Aucune valeur n'est simulée.
/// </summary>
public partial class CleanupViewModel : PageViewModelBase, IDisposable
{
    private readonly IScanEngine _scanEngine;
    private readonly ICleaningEngine _cleaningEngine;
    private readonly ICleanupHistoryStore _historyStore;
    private readonly IExclusionStore _exclusionStore;
    private CancellationTokenSource? _cts;

    public CleanupViewModel(
        IScanEngine scanEngine,
        ICleaningEngine cleaningEngine,
        ICleanupHistoryStore historyStore,
        IExclusionStore exclusionStore)
    {
        _scanEngine = scanEngine;
        _cleaningEngine = cleaningEngine;
        _historyStore = historyStore;
        _exclusionStore = exclusionStore;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Cleanup");

    public override string IconGlyph => "\U0001F9F9";

    public override bool IsUnderConstruction => false;

    public ObservableCollection<ScanItemViewModel> Items { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(CleanCommand))]
    private bool _isScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(CleanCommand))]
    private bool _isCleaning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEmptyState))]
    private bool _hasScanned;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("Cleanup.Msg.Idle");

    [ObservableProperty]
    private double _progressFraction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    private string? _resultMessage;

    [ObservableProperty]
    private bool _hasResultFailures;

    public bool IsBusy => IsScanning || IsCleaning;

    public bool HasResult => !string.IsNullOrEmpty(ResultMessage);

    public bool HasItems => Items.Count > 0;

    public bool ShowEmptyState => HasScanned && Items.Count == 0;

    public int SelectedCount => Items.Count(i => i.IsSelected);

    public long SelectedBytes => Items.Where(i => i.IsSelected).Sum(i => i.SizeBytes);

    public string SelectedBytesText => ByteSize.Format(SelectedBytes);

    public string SafeBytesText => ByteSize.Format(SelectedSum(RiskLevel.Safe));

    public string PrivacyBytesText => ByteSize.Format(SelectedSum(RiskLevel.Privacy));

    public string ReviewBytesText => ByteSize.Format(SelectedSum(RiskLevel.Review));

    private long SelectedSum(RiskLevel risk) =>
        Items.Where(i => i.IsSelected && i.Risk == risk).Sum(i => i.SizeBytes);

    private bool CanScan() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        ResultMessage = null;
        HasResultFailures = false;
        DetachItems();
        Items.Clear();
        RaiseSummary();
        HasScanned = false;
        IsScanning = true;
        ProgressFraction = 0;

        try
        {
            var progress = new Progress<ScanProgress>(p =>
            {
                ProgressFraction = p.Fraction;
                StatusMessage = p.FilesScanned > 0
                    ? Localizer.Format("Cleanup.Msg.ScanningFiles", p.FilesScanned, ByteSize.Format(p.BytesFound))
                    : Localizer.Format("Cleanup.Msg.ScanningProvider", p.CurrentProvider);
            });

            var report = await _scanEngine.ScanAsync(progress, _cts.Token);

            var excluded = 0;
            foreach (var item in report.Items)
            {
                if (_exclusionStore.IsExcluded(item))
                {
                    excluded++;
                    continue;
                }

                var vm = new ScanItemViewModel(item);
                vm.PropertyChanged += OnItemChanged;
                Items.Add(vm);
            }

            HasScanned = true;
            var excludedSuffix = excluded > 0 ? Localizer.Format("Cleanup.Msg.ExcludedSuffix", excluded) : string.Empty;
            StatusMessage = Items.Count == 0
                ? Localizer.Get("Cleanup.Msg.NothingFound") + excludedSuffix
                : Localizer.Format("Cleanup.Msg.Found", Items.Count, ByteSize.Format(Items.Sum(i => i.SizeBytes)), report.Elapsed.TotalSeconds) + excludedSuffix;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Cleanup.Msg.ScanCanceled");
        }
        finally
        {
            IsScanning = false;
            ProgressFraction = 0;
            RaiseSummary();
        }
    }

    private bool CanClean() => !IsBusy && SelectedCount > 0;

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var selected = Items.Where(i => i.IsSelected).Select(i => i.Model).ToList();
        var plan = _cleaningEngine.BuildPlan(selected);

        IsCleaning = true;
        ProgressFraction = 0;

        try
        {
            var progress = new Progress<CleaningProgress>(p =>
            {
                ProgressFraction = p.Fraction;
                StatusMessage = Localizer.Format("Cleanup.Msg.Cleaning", p.CurrentItem, p.Processed, p.Total);
            });

            var result = await _cleaningEngine.CleanAsync(plan, progress, _cts.Token);
            await RecordHistoryAsync(result);

            HasResultFailures = result.HasFailures;
            ResultMessage = result.HasFailures
                ? Localizer.Format("Cleanup.Msg.ResultFailures", ByteSize.Format(result.BytesFreed), result.Failures.Count)
                : Localizer.Format("Cleanup.Msg.ResultOk", ByteSize.Format(result.BytesFreed));
            StatusMessage = Localizer.Get("Cleanup.Msg.Done");

            DetachItems();
            Items.Clear();
            HasScanned = false;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Cleanup.Msg.CleanCanceled");
        }
        finally
        {
            IsCleaning = false;
            ProgressFraction = 0;
            RaiseSummary();
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    private async Task RecordHistoryAsync(CleaningResult result)
    {
        try
        {
            await _historyStore.AddAsync(new CleanupHistoryEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                AppVersion = AppInfo.Version,
                Source = "Nettoyage",
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
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScanItemViewModel.IsSelected))
        {
            RaiseSummary();
        }
    }

    private void DetachItems()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= OnItemChanged;
        }
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(SelectedBytes));
        OnPropertyChanged(nameof(SelectedBytesText));
        OnPropertyChanged(nameof(SafeBytesText));
        OnPropertyChanged(nameof(PrivacyBytesText));
        OnPropertyChanged(nameof(ReviewBytesText));
        CleanCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        DetachItems();
        GC.SuppressFinalize(this);
    }
}
