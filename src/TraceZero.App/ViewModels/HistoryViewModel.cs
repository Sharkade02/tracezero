using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.History;
using TraceZero.Domain.Common;
using TraceZero.Domain.History;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'historique affichée.</summary>
public sealed class HistoryRowViewModel
{
    // Les sources sont stockées en clair (catégorie) ; on les mappe vers une chaîne localisée à l'affichage.
    private static string MapSource(string source) => source switch
    {
        "Nettoyage" => Localizer.Get("Nav.Cleanup"),
        "Confidentialité" => Localizer.Get("Nav.Privacy"),
        "Automatisation" => Localizer.Get("Nav.Automation"),
        _ => source,
    };

    public HistoryRowViewModel(CleanupHistoryEntry entry)
    {
        DateText = entry.TimestampUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        Source = MapSource(entry.Source);
        FreedText = ByteSize.Format(entry.FreedBytes);
        Details = entry.Failures > 0
            ? Localizer.Format("History.DetailsFailures", entry.ItemsCleaned, entry.Failures)
            : Localizer.Format("Common.Items", entry.ItemsCleaned);
    }

    public string DateText { get; }

    public string Source { get; }

    public string FreedText { get; }

    public string Details { get; }
}

/// <summary>
/// Page Historique (§16, §26) : statistiques locales et journal des nettoyages. Aucune télémétrie ;
/// aucun chemin personnel n'est conservé (§39).
/// </summary>
public sealed partial class HistoryViewModel : PageViewModelBase
{
    private readonly ICleanupHistoryStore _store;
    private readonly IDialogService _dialog;
    private readonly IToastService _toasts;

    public HistoryViewModel(ICleanupHistoryStore store, IDialogService dialog, IToastService toasts)
    {
        _store = store;
        _dialog = dialog;
        _toasts = toasts;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.History");

    public override string IconGlyph => "\U0001F553";

    public override bool IsUnderConstruction => false;

    public ObservableCollection<HistoryRowViewModel> Entries { get; } = [];

    [ObservableProperty]
    private string _totalFreedText = "—";

    [ObservableProperty]
    private string _cleanupCountText = "—";

    [ObservableProperty]
    private string _lastCleanupText = "—";

    [ObservableProperty]
    private bool _hasEntries;

    public override void OnActivated() => _ = RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var stats = await _store.GetStatsAsync();
        TotalFreedText = ByteSize.Format(stats.TotalFreedBytes);
        CleanupCountText = stats.CleanupCount.ToString("N0", CultureInfo.GetCultureInfo("fr-FR"));
        LastCleanupText = stats.LastCleanupUtc is { } last
            ? last.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("fr-FR"))
            : "Jamais";

        var recent = await _store.GetRecentAsync(50);
        Entries.Clear();
        foreach (var entry in recent)
        {
            Entries.Add(new HistoryRowViewModel(entry));
        }

        HasEntries = Entries.Count > 0;
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        if (!HasEntries)
        {
            return;
        }

        var confirmed = await _dialog.ConfirmAsync(
            Localizer.Get("History.Confirm.Title"),
            Localizer.Get("History.Confirm.Body"),
            confirmText: Localizer.Get("Common.Erase"),
            cancelText: Localizer.Get("Common.Cancel"),
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        await _store.ClearAsync();
        _toasts.Show(Localizer.Get("History.Toast.Cleared"), ToastKind.Info);
        await RefreshAsync();
    }
}
