using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Disk;
using TraceZero.Application.Duplicates;
using TraceZero.Domain.Common;
using TraceZero.Domain.Duplicates;

namespace TraceZero.App.ViewModels;

public sealed partial class DuplicateFileRowViewModel(DuplicateFile file) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public DuplicateFile File => file;
    public string FileName => file.FileName;
    public string Path => file.Path;
    public string DateText => file.LastWriteUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    public DateTime LastWriteUtc => file.LastWriteUtc;
}

public sealed class DuplicateGroupViewModel
{
    public DuplicateGroupViewModel(DuplicateGroup group)
    {
        SizeText = ByteSize.Format(group.SizeBytes);
        ReclaimableText = ByteSize.Format(group.ReclaimableBytes);
        Files = new ObservableCollection<DuplicateFileRowViewModel>(
            group.Files.OrderByDescending(f => f.LastWriteUtc).Select(f => new DuplicateFileRowViewModel(f)));
        HeaderText = Localizer.Format("Dup.Header", Files.Count, SizeText, ReclaimableText);
    }

    public string SizeText { get; }
    public string ReclaimableText { get; }
    public string HeaderText { get; }
    public ObservableCollection<DuplicateFileRowViewModel> Files { get; }

    /// <summary>Sélectionne toutes les copies sauf la plus récente (stratégie sûre par défaut).</summary>
    public void SelectAllButNewest()
    {
        DuplicateFileRowViewModel? newest = null;
        foreach (var file in Files)
        {
            if (newest is null || file.LastWriteUtc > newest.LastWriteUtc)
            {
                newest = file;
            }
        }

        foreach (var file in Files)
        {
            file.IsSelected = !ReferenceEquals(file, newest);
        }
    }

    /// <summary>Garantit qu'au moins une copie reste (jamais supprimer tout un groupe).</summary>
    public void EnsureKeepsOne()
    {
        if (Files.All(f => f.IsSelected) && Files.Count > 0)
        {
            var newest = Files.OrderByDescending(f => f.LastWriteUtc).First();
            newest.IsSelected = false;
        }
    }
}

/// <summary>
/// Page Doublons (§21). Détection fiable (taille → hash partiel → hash complet), stratégie
/// « garder le plus récent », suppression réversible (Corbeille) avec validation utilisateur.
/// </summary>
public sealed partial class DuplicatesViewModel : PageViewModelBase, IDisposable
{
    private readonly IDuplicateFinder _finder;
    private readonly IRecycleFileService _recycleFile;
    private CancellationTokenSource? _cts;

    public DuplicatesViewModel(IDuplicateFinder finder, IRecycleFileService recycleFile)
    {
        _finder = finder;
        _recycleFile = recycleFile;
        SelectedThreshold = Thresholds[1];
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.Duplicates");
    public override string IconGlyph => "\U0001F5C2";
    public override bool IsUnderConstruction => false;

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = [];

    public IReadOnlyList<ThresholdOption> Thresholds { get; } =
    [
        new(Localizer.Format("Dup.Threshold", "100 Ko"), 100L * 1024),
        new(Localizer.Format("Dup.Threshold", "1 Mo"), 1024L * 1024),
        new(Localizer.Format("Dup.Threshold", "10 Mo"), 10L * 1024 * 1024),
    ];

    [ObservableProperty]
    private ThresholdOption _selectedThreshold;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("Dup.Msg.Idle");

    [ObservableProperty]
    private bool _hasGroups;

    private bool CanScan() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var minBytes = SelectedThreshold.Bytes;
        var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        Groups.Clear();
        HasGroups = false;
        IsScanning = true;

        try
        {
            var progress = new Progress<long>(n => StatusMessage = Localizer.Format("Dup.Msg.Scanning", n));
            var reporter = new ProgressScanReporter(progress);

            var found = await _finder.FindAsync(root, minBytes, reporter, token);

            long reclaimable = 0;
            foreach (var group in found)
            {
                Groups.Add(new DuplicateGroupViewModel(group));
                reclaimable += group.ReclaimableBytes;
            }

            HasGroups = Groups.Count > 0;
            StatusMessage = Groups.Count == 0
                ? Localizer.Get("Dup.Msg.None")
                : Localizer.Format("Dup.Msg.Found", Groups.Count, ByteSize.Format(reclaimable));
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("Dup.Msg.Canceled");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private void KeepNewest()
    {
        foreach (var group in Groups)
        {
            group.SelectAllButNewest();
        }

        StatusMessage = Localizer.Get("Dup.Msg.Preselected");
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var file in Groups.SelectMany(g => g.Files))
        {
            file.IsSelected = false;
        }
    }

    [RelayCommand]
    private static void OpenLocation(DuplicateFileRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{row.Path}\"") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    [RelayCommand]
    private void RecycleSelected()
    {
        // Sécurité : on ne supprime jamais toutes les copies d'un groupe.
        foreach (var group in Groups)
        {
            group.EnsureKeepsOne();
        }

        var toRemove = Groups.SelectMany(g => g.Files).Where(f => f.IsSelected).ToList();
        if (toRemove.Count == 0)
        {
            StatusMessage = Localizer.Get("Dup.Msg.NoneSelected");
            return;
        }

        var confirm = MessageBox.Show(
            Localizer.Format("Dup.Confirm.Body", toRemove.Count),
            Localizer.Get("Common.Confirm"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var removed = 0;
        foreach (var group in Groups.ToList())
        {
            foreach (var file in group.Files.Where(f => f.IsSelected).ToList())
            {
                if (_recycleFile.SendToRecycleBin(file.Path))
                {
                    group.Files.Remove(file);
                    removed++;
                }
            }

            if (group.Files.Count < 2)
            {
                Groups.Remove(group);
            }
        }

        HasGroups = Groups.Count > 0;
        StatusMessage = Localizer.Format("Dup.Msg.Recycled", removed);
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
