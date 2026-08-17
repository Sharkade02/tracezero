using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using TraceZero.App.Services;
using TraceZero.Application.Diagnostics;
using TraceZero.Application.Disk;
using TraceZero.Application.Erasure;
using TraceZero.Domain.Common;
using TraceZero.Domain.Diagnostics;
using TraceZero.Domain.Disk;
using TraceZero.Domain.Erasure;

namespace TraceZero.App.ViewModels;

/// <summary>Fichier sélectionné pour effacement sécurisé.</summary>
public sealed class EraseTargetViewModel
{
    public EraseTargetViewModel(string path, long size)
    {
        Path = path;
        FileName = System.IO.Path.GetFileName(path);
        SizeText = ByteSize.Format(size);
    }

    public string Path { get; }
    public string FileName { get; }
    public string SizeText { get; }
}

/// <summary>Lecteur proposé pour l'effacement d'espace libre.</summary>
public sealed class WipeDriveViewModel
{
    public WipeDriveViewModel(DriveInfoModel drive)
    {
        Root = drive.Name;
        var label = string.IsNullOrWhiteSpace(drive.Label) ? "" : $" ({drive.Label})";
        Display = $"{drive.Name}{label} — {ByteSize.Format(drive.FreeBytes)} libres";
    }

    public string Root { get; }
    public string Display { get; }
}

/// <summary>
/// Page « Effacement sécurisé » (§19). Deux fonctions : effacement sécurisé de fichiers choisis
/// (irréversible, jamais présenté autrement) et effacement de l'espace libre d'un lecteur (ne touche
/// aucun fichier existant). Détecte le type de média et présente un avertissement honnête SSD/NVMe
/// (wear leveling / TRIM) : le résultat n'y est jamais garanti.
/// </summary>
public sealed partial class SecureEraseViewModel : PageViewModelBase, IDisposable
{
    private readonly ISecureFileEraser _eraser;
    private readonly IFreeSpaceWiper _wiper;
    private readonly IStorageMediaProbe _mediaProbe;
    private readonly IDriveQueryService _drives;
    private readonly IDialogService _dialog;
    private readonly IToastService _toasts;
    private CancellationTokenSource? _wipeCts;

    public SecureEraseViewModel(
        ISecureFileEraser eraser,
        IFreeSpaceWiper wiper,
        IStorageMediaProbe mediaProbe,
        IDriveQueryService drives,
        IDialogService dialog,
        IToastService toasts)
    {
        _eraser = eraser;
        _wiper = wiper;
        _mediaProbe = mediaProbe;
        _drives = drives;
        _dialog = dialog;
        _toasts = toasts;
    }

    public override string Title => TraceZero.App.Services.Localizer.Get("Nav.SecureErase");

    public override string IconGlyph => "\U0001F525"; // 🔥

    public override bool IsUnderConstruction => false;

    public ObservableCollection<EraseTargetViewModel> Targets { get; } = [];

    public ObservableCollection<WipeDriveViewModel> Drives { get; } = [];

    [ObservableProperty]
    private bool _reinforced;

    [ObservableProperty]
    private WipeDriveViewModel? _selectedDrive;

    [ObservableProperty]
    private string _fileMediaWarning = string.Empty;

    [ObservableProperty]
    private string _driveMediaWarning = string.Empty;

    [ObservableProperty]
    private bool _isErasing;

    [ObservableProperty]
    private bool _isWiping;

    [ObservableProperty]
    private double _wipeProgress;

    [ObservableProperty]
    private string _wipeStatus = string.Empty;

    public bool HasTargets => Targets.Count > 0;

    public override void OnActivated()
    {
        if (Drives.Count == 0)
        {
            foreach (var drive in _drives.GetFixedDrives())
            {
                Drives.Add(new WipeDriveViewModel(drive));
            }

            SelectedDrive = Drives.FirstOrDefault();
        }
    }

    partial void OnSelectedDriveChanged(WipeDriveViewModel? value) =>
        DriveMediaWarning = value is null ? string.Empty : MediaWarning(_mediaProbe.GetMediaForPath(value.Root), forFreeSpace: true);

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = Localizer.Get("SecureErase.PickFiles"),
            Multiselect = true,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var rejected = 0;
        foreach (var path in dialog.FileNames)
        {
            var reason = _eraser.ValidateTarget(path);
            if (reason is not null)
            {
                rejected++;
                continue;
            }

            if (Targets.Any(t => string.Equals(t.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            long size = 0;
            try { size = new FileInfo(path).Length; } catch (IOException) { }
            Targets.Add(new EraseTargetViewModel(path, size));
        }

        if (rejected > 0)
        {
            _toasts.Show(Localizer.Format("SecureErase.Toast.Rejected", rejected), ToastKind.Warning);
        }

        UpdateFileMedia();
        OnPropertyChanged(nameof(HasTargets));
        EraseCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void ClearTargets()
    {
        Targets.Clear();
        FileMediaWarning = string.Empty;
        OnPropertyChanged(nameof(HasTargets));
        EraseCommand.NotifyCanExecuteChanged();
    }

    private void UpdateFileMedia()
    {
        var first = Targets.FirstOrDefault();
        FileMediaWarning = first is null
            ? string.Empty
            : MediaWarning(_mediaProbe.GetMediaForPath(first.Path), forFreeSpace: false);
    }

    private bool CanErase() => !IsErasing && HasTargets;

    [RelayCommand(CanExecute = nameof(CanErase))]
    private async Task EraseAsync()
    {
        var method = Reinforced ? SecureEraseMethod.ReinforcedOverwrite : SecureEraseMethod.SingleOverwrite;
        var confirmed = await _dialog.ConfirmAsync(
            Localizer.Get("SecureErase.Confirm.Title"),
            Localizer.Format("SecureErase.Confirm.Body", Targets.Count),
            confirmText: Localizer.Get("SecureErase.EraseBtn"),
            cancelText: Localizer.Get("Common.Cancel"),
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        IsErasing = true;
        try
        {
            var toErase = Targets.ToList();
            var succeeded = 0;
            var failed = 0;

            foreach (var target in toErase)
            {
                var result = await _eraser.EraseFileAsync(target.Path, method);
                if (result.Success)
                {
                    succeeded++;
                    Targets.Remove(target);
                }
                else
                {
                    failed++;
                }
            }

            _toasts.Show(
                failed == 0
                    ? Localizer.Format("SecureErase.Toast.Erased", succeeded)
                    : Localizer.Format("SecureErase.Toast.ErasedPartial", succeeded, failed),
                failed == 0 ? ToastKind.Success : ToastKind.Warning);
        }
        finally
        {
            IsErasing = false;
            UpdateFileMedia();
            OnPropertyChanged(nameof(HasTargets));
            EraseCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanWipe() => !IsWiping && SelectedDrive is not null;

    [RelayCommand(CanExecute = nameof(CanWipe))]
    private async Task WipeFreeSpaceAsync()
    {
        if (SelectedDrive is not { } drive)
        {
            return;
        }

        var confirmed = await _dialog.ConfirmAsync(
            Localizer.Get("SecureErase.WipeBtn"),
            Localizer.Format("SecureErase.Wipe.ConfirmBody", drive.Root),
            confirmText: Localizer.Get("SecureErase.WipeBtn"),
            cancelText: Localizer.Get("Common.Cancel"),
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        var workingDir = ResolveWorkingDirectory(drive.Root);
        if (workingDir is null)
        {
            _toasts.Show(Localizer.Format("SecureErase.Toast.WriteFailed", drive.Root), ToastKind.Error);
            return;
        }

        _wipeCts?.Dispose();
        _wipeCts = new CancellationTokenSource();
        IsWiping = true;
        WipeProgress = 0;
        WipeStatus = Localizer.Get("SecureErase.Wipe.Running");

        var progress = new Progress<FreeSpaceWipeProgress>(p =>
        {
            WipeProgress = p.Fraction;
            WipeStatus = Localizer.Format("SecureErase.Wipe.Progress", ByteSize.Format(p.BytesWritten), ByteSize.Format(p.EstimatedTotalBytes));
        });

        try
        {
            var result = await _wiper.WipeAsync(workingDir, maxBytes: 0, progress, _wipeCts.Token);
            WipeStatus = result.Canceled
                ? Localizer.Get("SecureErase.Wipe.Canceled")
                : result.Success
                    ? Localizer.Format("SecureErase.Wipe.Success", ByteSize.Format(result.BytesWritten))
                    : Localizer.Format("SecureErase.Wipe.Failed", result.Error ?? string.Empty);

            _toasts.Show(WipeStatus, result.Success ? ToastKind.Success : ToastKind.Warning);
        }
        finally
        {
            IsWiping = false;
            WipeProgress = 0;
            WipeFreeSpaceCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void CancelWipe() => _wipeCts?.Cancel();

    public void Dispose()
    {
        _wipeCts?.Dispose();
        _wipeCts = null;
        GC.SuppressFinalize(this);
    }

    private static string? ResolveWorkingDirectory(string driveRoot)
    {
        // Le dossier temporaire couvre le lecteur système ; sinon on crée un dossier de travail sur le lecteur.
        var temp = Path.GetTempPath();
        if (string.Equals(Path.GetPathRoot(temp), driveRoot, StringComparison.OrdinalIgnoreCase))
        {
            return temp;
        }

        try
        {
            var dir = Path.Combine(driveRoot, "TraceZeroWipe");
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string MediaWarning(DiskMediaKind media, bool forFreeSpace) => media switch
    {
        DiskMediaKind.Ssd => Localizer.Get("SecureErase.Media.Ssd"),
        DiskMediaKind.Hdd => forFreeSpace
            ? Localizer.Get("SecureErase.Media.HddFree")
            : Localizer.Get("SecureErase.Media.HddFile"),
        _ => Localizer.Get("SecureErase.Media.Unknown"),
    };
}
