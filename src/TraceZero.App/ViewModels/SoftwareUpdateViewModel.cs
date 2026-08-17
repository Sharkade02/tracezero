using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Software;
using TraceZero.Domain.Software;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'une mise à jour logicielle disponible.</summary>
public sealed class SoftwareUpdateRowViewModel
{
    public SoftwareUpdateRowViewModel(SoftwareUpdate update)
    {
        Model = update;
        Name = update.Name;
        Id = update.Id;
        Source = update.Source;
        VersionText = $"{update.InstalledVersion} → {update.AvailableVersion}";
    }

    public SoftwareUpdate Model { get; }
    public string Name { get; }
    public string Id { get; }
    public string Source { get; }
    public string VersionText { get; }
}

/// <summary>
/// Page « Mises à jour » (Phase 13, §23). Détecte les logiciels obsolètes via le Windows Package Manager
/// (winget), source officielle et signée. TraceZero n'installe rien en propre : la mise à jour est lancée
/// par winget, visible par l'utilisateur. Aucun scraping de sources douteuses.
/// </summary>
public sealed partial class SoftwareUpdateViewModel : PageViewModelBase
{
    private readonly ISoftwareUpdateService _service;
    private readonly IToastService _toasts;

    public SoftwareUpdateViewModel(ISoftwareUpdateService service, IToastService toasts)
    {
        _service = service;
        _toasts = toasts;
    }

    public override string Title => Localizer.Get("Nav.Software");

    public override string IconGlyph => "\U0001F53C"; // 🔼

    public override bool IsUnderConstruction => false;

    public ObservableCollection<SoftwareUpdateRowViewModel> Updates { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasUpdates;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var report = await Task.Run(() => _service.GetAvailableUpdatesAsync());
            Updates.Clear();
            foreach (var update in report.Updates)
            {
                Updates.Add(new SoftwareUpdateRowViewModel(update));
            }

            HasUpdates = Updates.Count > 0;
            StatusMessage = !report.SourceAvailable
                ? Localizer.Get("Software.Unavailable")
                : Updates.Count == 0
                    ? Localizer.Get("Software.None")
                    : Localizer.Format("Software.Found", Updates.Count);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Update(SoftwareUpdateRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        _toasts.Show(
            _service.LaunchUpdate(row.Id)
                ? Localizer.Format("Software.LaunchedToast", row.Name)
                : Localizer.Get("Software.LaunchFailed"),
            ToastKind.Info);
    }

    [RelayCommand]
    private void UpdateAll()
    {
        _toasts.Show(
            _service.LaunchUpdateAll()
                ? Localizer.Get("Software.LaunchAllToast")
                : Localizer.Get("Software.LaunchFailed"),
            ToastKind.Info);
    }
}
