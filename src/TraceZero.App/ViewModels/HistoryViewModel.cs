using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.Application.History;
using TraceZero.Domain.Common;
using TraceZero.Domain.History;

namespace TraceZero.App.ViewModels;

/// <summary>Ligne d'historique affichée.</summary>
public sealed class HistoryRowViewModel
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public HistoryRowViewModel(CleanupHistoryEntry entry)
    {
        DateText = entry.TimestampUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Fr);
        Source = entry.Source;
        FreedText = ByteSize.Format(entry.FreedBytes);
        Details = entry.Failures > 0
            ? $"{entry.ItemsCleaned} élément(s) · {entry.Failures} ignoré(s)"
            : $"{entry.ItemsCleaned} élément(s)";
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

    public HistoryViewModel(ICleanupHistoryStore store)
    {
        _store = store;
    }

    public override string Title => "Historique";

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
        await _store.ClearAsync();
        await RefreshAsync();
    }
}
