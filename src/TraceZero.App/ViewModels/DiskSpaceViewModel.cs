using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TraceZero.App.Services;
using TraceZero.Application.Disk;
using TraceZero.Domain.Common;
using TraceZero.Domain.Disk;

namespace TraceZero.App.ViewModels;

public sealed class DriveRowViewModel
{
    public DriveRowViewModel(DriveInfoModel drive)
    {
        Name = drive.Label is { } label ? $"{drive.Name} ({label})" : drive.Name;
        Format = drive.Format;
        UsedText = ByteSize.Format(drive.UsedBytes);
        FreeText = ByteSize.Format(drive.FreeBytes);
        TotalText = ByteSize.Format(drive.TotalBytes);
        UsedFraction = drive.UsedFraction;
    }

    public string Name { get; }
    public string Format { get; }
    public string UsedText { get; }
    public string FreeText { get; }
    public string TotalText { get; }
    public double UsedFraction { get; }
    public string SummaryText => Localizer.Format("DiskSpace.Drive.Summary", UsedText, TotalText, FreeText);
}

public sealed partial class LargeFileRowViewModel(LargeFileEntry entry) : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public string FileName => entry.FileName;
    public string Path => entry.Path;
    public long SizeBytes => entry.SizeBytes;
    public string SizeText => ByteSize.Format(entry.SizeBytes);
    public string DateText => entry.LastWriteUtc.ToLocalTime().ToString("d", CultureInfo.CurrentCulture);
}

public sealed record ThresholdOption(string Label, long Bytes);

/// <summary>Page Espace disque (§20) : vue des lecteurs et recherche de gros fichiers.</summary>
public sealed partial class DiskSpaceViewModel : PageViewModelBase, IDisposable
{
    private const int MaxResults = 500;

    private readonly IDriveQueryService _driveQuery;
    private readonly ILargeFileScanner _scanner;
    private readonly IRecycleFileService _recycleFile;
    private CancellationTokenSource? _cts;

    public DiskSpaceViewModel(IDriveQueryService driveQuery, ILargeFileScanner scanner, IRecycleFileService recycleFile)
    {
        _driveQuery = driveQuery;
        _scanner = scanner;
        _recycleFile = recycleFile;
        SelectedThreshold = Thresholds[2];
        LoadDrives();
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.DiskSpace");
    public override string IconGlyph => "\U0001F4BD";
    public override bool IsUnderConstruction => false;

    public ObservableCollection<DriveRowViewModel> Drives { get; } = [];

    public ObservableCollection<LargeFileRowViewModel> LargeFiles { get; } = [];

    public IReadOnlyList<ThresholdOption> Thresholds { get; } =
    [
        new(Localizer.Format("DiskSpace.Threshold", "100 Mo"), 100L * 1024 * 1024),
        new(Localizer.Format("DiskSpace.Threshold", "500 Mo"), 500L * 1024 * 1024),
        new(Localizer.Format("DiskSpace.Threshold", "1 Go"), 1024L * 1024 * 1024),
        new(Localizer.Format("DiskSpace.Threshold", "2 Go"), 2L * 1024 * 1024 * 1024),
    ];

    [ObservableProperty]
    private ThresholdOption _selectedThreshold;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanLargeFilesCommand))]
    [NotifyCanExecuteChangedFor(nameof(RecycleSelectedCommand))]
    private bool _isScanning;

    [ObservableProperty]
    private string _statusMessage = Localizer.Get("DiskSpace.Msg.Idle");

    [ObservableProperty]
    private bool _hasResults;

    public override void OnActivated() => LoadDrives();

    private void LoadDrives()
    {
        Drives.Clear();
        foreach (var drive in _driveQuery.GetFixedDrives())
        {
            Drives.Add(new DriveRowViewModel(drive));
        }
    }

    private bool CanScan() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanLargeFilesAsync()
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        var threshold = SelectedThreshold.Bytes;
        var root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        LargeFiles.Clear();
        HasResults = false;
        IsScanning = true;

        try
        {
            var progress = new Progress<long>(n => StatusMessage = Localizer.Format("DiskSpace.Msg.Scanning", n));
            var reporter = new ProgressScanReporter(progress);

            var found = await Task.Run(async () =>
            {
                var list = new List<LargeFileEntry>();
                await foreach (var entry in _scanner.ScanAsync(root, threshold, reporter, token))
                {
                    list.Add(entry);
                }

                return list;
            }, token);

            foreach (var entry in found.OrderByDescending(e => e.SizeBytes).Take(MaxResults))
            {
                LargeFiles.Add(new LargeFileRowViewModel(entry));
            }

            HasResults = LargeFiles.Count > 0;
            var capSuffix = found.Count > MaxResults ? Localizer.Format("DiskSpace.Msg.CapSuffix", MaxResults) : string.Empty;
            StatusMessage = LargeFiles.Count == 0
                ? Localizer.Get("DiskSpace.Msg.None")
                : Localizer.Format("DiskSpace.Msg.Found", found.Count) + capSuffix;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = Localizer.Get("DiskSpace.Msg.Canceled");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand]
    private static void OpenLocation(LargeFileRowViewModel? row)
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
            // Ouverture best-effort.
        }
    }

    private bool CanRecycle() => !IsScanning;

    [RelayCommand(CanExecute = nameof(CanRecycle))]
    private void RecycleSelected()
    {
        var selected = LargeFiles.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var confirm = MessageBox.Show(
            Localizer.Format("DiskSpace.Confirm.Body", selected.Count),
            Localizer.Get("Common.Confirm"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var removed = 0;
        foreach (var file in selected)
        {
            if (_recycleFile.SendToRecycleBin(file.Path))
            {
                LargeFiles.Remove(file);
                removed++;
            }
        }

        HasResults = LargeFiles.Count > 0;
        StatusMessage = Localizer.Format("DiskSpace.Msg.Recycled", removed);
        LoadDrives();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
