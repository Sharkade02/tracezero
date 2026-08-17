using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Disk;
using TraceZero.Application.Exclusions;
using TraceZero.Application.Privacy;
using TraceZero.Application.Scanning;
using TraceZero.Domain;
using TraceZero.Domain.Common;

namespace TraceZero.App.ViewModels;

/// <summary>
/// Page d'accueil avec **Health Check en un clic** (§2, benchmark CCleaner). Une analyse rapide réelle
/// agrège l'espace récupérable (par risque et par catégorie), les traces de confidentialité et
/// l'occupation disque. Aucune valeur simulée : tout reste « — » tant qu'aucune analyse n'a eu lieu (§0),
/// et aucun « score » inventé ni alarmisme (§42).
/// </summary>
public sealed partial class DashboardViewModel : PageViewModelBase, IDisposable
{
    private const string Placeholder = "—";

    private readonly INavigationService _navigation;
    private readonly CleanupViewModel _cleanup;
    private readonly IScanEngine _scanEngine;
    private readonly IPrivacyInspector _privacyInspector;
    private readonly IExclusionStore _exclusionStore;
    private readonly IDriveQueryService _drives;
    private CancellationTokenSource? _cts;

    public DashboardViewModel(
        INavigationService navigation,
        CleanupViewModel cleanup,
        IScanEngine scanEngine,
        IPrivacyInspector privacyInspector,
        IExclusionStore exclusionStore,
        IDriveQueryService drives)
    {
        _navigation = navigation;
        _cleanup = cleanup;
        _scanEngine = scanEngine;
        _privacyInspector = privacyInspector;
        _exclusionStore = exclusionStore;
        _drives = drives;
    }

    public override string Title => Localizer.Get("Nav.Home");

    public override string IconGlyph => "\U0001F3E0";

    public override bool IsUnderConstruction => false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HealthCheckCommand))]
    private bool _isChecking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCleanable))]
    private bool _hasResult;

    private long _recoverableBytes;
    public bool HasCleanable => HasResult && _recoverableBytes > 0;

    [ObservableProperty] private string _recoverableText = Placeholder;
    [ObservableProperty] private string _safeText = Placeholder;
    [ObservableProperty] private string _privacyText = Placeholder;
    [ObservableProperty] private string _reviewText = Placeholder;
    [ObservableProperty] private string _winTempText = Placeholder;
    [ObservableProperty] private string _browsersText = Placeholder;
    [ObservableProperty] private string _recycleBinText = Placeholder;
    [ObservableProperty] private string _winTracesText = Placeholder;
    [ObservableProperty] private string _driveSummaryText = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    private bool CanCheck() => !IsChecking;

    [RelayCommand(CanExecute = nameof(CanCheck))]
    private async Task HealthCheckAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        IsChecking = true;
        StatusMessage = Localizer.Get("Home.Checking");

        try
        {
            var report = await _scanEngine.ScanAsync(progress: null, _cts.Token);
            var items = report.Items.Where(i => !_exclusionStore.IsExcluded(i)).ToList();

            _recoverableBytes = items.Sum(i => i.SizeBytes);
            RecoverableText = ByteSize.Format(_recoverableBytes);
            SafeText = ByteSize.Format(items.Where(i => i.Risk == RiskLevel.Safe).Sum(i => i.SizeBytes));
            PrivacyText = ByteSize.Format(items.Where(i => i.Risk == RiskLevel.Privacy).Sum(i => i.SizeBytes));
            ReviewText = ByteSize.Format(items.Where(i => i.Risk == RiskLevel.Review).Sum(i => i.SizeBytes));

            WinTempText = ByteSize.Format(SumCategories(items,
                Category.WindowsTemp, Category.WindowsCache, Category.CrashDumps, Category.ThumbnailCache, Category.SystemLogs));
            BrowsersText = ByteSize.Format(SumCategories(items, Category.BrowserCache));
            RecycleBinText = ByteSize.Format(SumCategories(items, Category.RecycleBin));

            // Traces de confidentialité : source dédiée (registre allowlisté / fichiers).
            var traces = await Task.Run(() => _privacyInspector.Inspect(), _cts.Token);
            var present = traces.Count(t => t.IsPresent);
            WinTracesText = Localizer.Format("Common.Items", present);

            // Occupation du lecteur système (premier lecteur fixe).
            var fixedDrives = _drives.GetFixedDrives();
            var drive = fixedDrives.Count > 0 ? fixedDrives[0] : null;
            DriveSummaryText = drive is null
                ? string.Empty
                : Localizer.Format("Home.DriveSummary", ByteSize.Format(drive.FreeBytes), ByteSize.Format(drive.TotalBytes));

            HasResult = true;
            StatusMessage = _recoverableBytes > 0
                ? Localizer.Format("Home.CheckResult", RecoverableText)
                : Localizer.Get("Home.CheckClean");
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Home.CheckCanceled");
        }
        finally
        {
            IsChecking = false;
        }
    }

    /// <summary>Ouvre le Nettoyage (qui relance une analyse détaillée avec cases à cocher).</summary>
    [RelayCommand]
    private void OpenCleanup()
    {
        _navigation.RequestNavigate(_cleanup);
        if (_cleanup.ScanCommand.CanExecute(null))
        {
            _cleanup.ScanCommand.Execute(null);
        }
    }

    private static long SumCategories(IEnumerable<ScanItem> items, params Category[] categories) =>
        items.Where(i => categories.Contains(i.Category)).Sum(i => i.SizeBytes);

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
