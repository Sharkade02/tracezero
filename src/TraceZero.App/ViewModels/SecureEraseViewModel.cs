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

    public override string Title => "Effacement sécurisé";

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
            Title = "Choisir les fichiers à effacer définitivement",
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
            _toasts.Show($"{rejected} élément(s) refusé(s) : fichier système, dossier ou cible protégée.", ToastKind.Warning);
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
            "Effacement sécurisé irréversible",
            $"{Targets.Count} fichier(s) vont être écrasés puis supprimés. Cette opération est irréversible — les fichiers ne pourront pas être récupérés. Continuer ?",
            confirmText: "Effacer définitivement",
            cancelText: "Annuler",
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
                    ? $"{succeeded} fichier(s) effacé(s) de façon sécurisée."
                    : $"{succeeded} effacé(s), {failed} en échec (verrouillé/inaccessible).",
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
            "Effacer l'espace libre",
            $"TraceZero va remplir l'espace libre de {drive.Root} pour rendre irrécupérables les fichiers déjà supprimés. Vos fichiers existants ne sont pas touchés. L'opération peut être longue et est annulable. Continuer ?",
            confirmText: "Effacer l'espace libre",
            cancelText: "Annuler",
            destructive: true);

        if (!confirmed)
        {
            return;
        }

        var workingDir = ResolveWorkingDirectory(drive.Root);
        if (workingDir is null)
        {
            _toasts.Show($"Impossible d'écrire sur {drive.Root} (droits insuffisants ?).", ToastKind.Error);
            return;
        }

        _wipeCts?.Dispose();
        _wipeCts = new CancellationTokenSource();
        IsWiping = true;
        WipeProgress = 0;
        WipeStatus = "Effacement de l'espace libre en cours…";

        var progress = new Progress<FreeSpaceWipeProgress>(p =>
        {
            WipeProgress = p.Fraction;
            WipeStatus = $"{ByteSize.Format(p.BytesWritten)} écrits sur ~{ByteSize.Format(p.EstimatedTotalBytes)}";
        });

        try
        {
            var result = await _wiper.WipeAsync(workingDir, maxBytes: 0, progress, _wipeCts.Token);
            WipeStatus = result.Canceled
                ? "Effacement de l'espace libre annulé."
                : result.Success
                    ? $"Espace libre effacé ({ByteSize.Format(result.BytesWritten)} écrits puis libérés)."
                    : $"Échec : {result.Error}";

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
        DiskMediaKind.Ssd =>
            "Ce lecteur est un SSD/NVMe. L'écrasement n'est pas garanti (wear leveling, TRIM) : Windows peut placer les données ailleurs. Ne comptez pas dessus comme sur un HDD.",
        DiskMediaKind.Hdd => forFreeSpace
            ? "Disque dur (HDD) : l'effacement de l'espace libre est efficace."
            : "Disque dur (HDD) : l'écrasement avant suppression est efficace.",
        _ =>
            "Type de lecteur indéterminé. Si c'est un SSD/NVMe, l'écrasement n'est pas garanti (wear leveling / TRIM).",
    };
}
